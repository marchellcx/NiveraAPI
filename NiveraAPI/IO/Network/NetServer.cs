using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;

using NiveraAPI.IO.Network.API.Internal;

using NiveraAPI.Logs;
using NiveraAPI.Services;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network;

public class NetServer : ServiceCollection
{
    private static volatile LogSink log = LogManager.GetSource("IO", "NetServer");

    internal volatile bool debugLogs;
    
    private volatile int connId = 0;
    private volatile int recvThreads = 8;
    
    private volatile Socket socket;
    private volatile ServerRecvPipe recvPipe;
    private volatile CancellationTokenSource cts;

    private volatile NetConnection[] conns = [];
    private volatile ConcurrentQueue<SendData> sendPool = new();

    private volatile ActionQueue queue = new();

    private long sentBytes;

    /// <summary>
    /// Gets called when a new connection is established.
    /// </summary>
    public event Action<NetConnection>? Connected;

    /// <summary>
    /// Gets called when a connection is disconnected.
    /// </summary>
    public event Action<NetConnection>? Disconnected; 
    
    /// <summary>
    /// Gets the total number of bytes sent by the server.
    /// </summary>
    public long SentBytes => sentBytes;

    /// <summary>
    /// Gets the total number of bytes received by the server.
    /// </summary>
    public long ReceivedBytes => recvPipe?.ReceivedBytes ?? 0;

    /// <summary>
    /// The number of threads used for receiving data.
    /// </summary>
    public int ReceiveThreads
    {
        get => recvThreads;
        set => recvThreads = value;
    }

    /// <summary>
    /// Whether debug logs are enabled.
    /// </summary>
    public bool DebugLogs
    {
        get => debugLogs;
        set => debugLogs = value;
    }

    /// <summary>
    /// Gets the logging mechanism associated with the network server.
    /// </summary>
    public LogSink Log => log;

    /// <summary>
    /// The list of services provided by the server.
    /// </summary>
    public List<Type> ProvidedServices { get; } = new();
    
    /// <summary>
    /// The list of connections currently connected to the server.
    /// </summary>
    public IReadOnlyList<NetConnection> Connections => conns;

    /// <summary>
    /// Begins listening for incoming network connections on the specified port.
    /// If no port is specified, a default value of 0 is used, which allows the operating system to select an available port.
    /// </summary>
    /// <param name="port">The port on which the server will listen for incoming connections. Use 0 to let the operating system assign a random available port.</param>
    public void Listen(int port = 0)
    {
        log.DebugIf($"Starting server on port {port}...", debugLogs);
        
        if (socket != null)
            Stop();
        
        if (!IsRunning)
            Start();

        connId = 0;
        sentBytes = 0;
        
        log.DebugIf("Creating socket...", debugLogs);
        
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        
        socket.SendBufferSize = NetSettings.MTU;
        socket.ReceiveBufferSize = NetSettings.MTU;
        
        log.DebugIf("Binding socket...", debugLogs);
        
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));

        log.DebugIf($"Bound to port {port}", debugLogs);
        
        recvPipe = new(this, socket);
        recvPipe.Start();
        
        log.DebugIf("RecvPipe started", debugLogs);

        cts = new();
        
        ThreadPool.QueueUserWorkItem(_ => Send());
        
        log.DebugIf("Send thread started", debugLogs);
    }

    /// <inheritdoc />
    public override void Stop()
    {
        log.DebugIf("Stopping server...", debugLogs);
        
        base.Stop();
        
        cts.Cancel();
        
        log.DebugIf("Stopping RecvPipe", debugLogs);

        if (recvPipe != null)
        {
            recvPipe.Stop();
            recvPipe = null!;
        }
        
        log.DebugIf("Stopping connections...", debugLogs);

        for (var x = 0; x < conns.Length; x++)
        {
            try
            {
                conns[x].Stop();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        try
        {
            if (socket != null)
            {
                log.DebugIf("Closing socket...", debugLogs);
                
                socket.Close();
                socket.Dispose();
            }
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }
        
        log.DebugIf("Clearing send pool", debugLogs);

        while (sendPool.TryDequeue(out var data))
        {
            data.Args.Dispose();
            data.Writer.ReturnToPool();
        }

        socket = null!;

        conns = [];
        
        log.DebugIf("Server stopped", debugLogs);
    }

    /// <summary>
    /// Disconnects the specified network connection from the server.
    /// Upon disconnection, the connection will no longer be managed by the server or processed by the associated action queue.
    /// </summary>
    /// <param name="conn">The network connection to be disconnected.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided connection is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided connection is not managed by this server.</exception>
    public void Disconnect(NetConnection conn)
    {
        if (conn == null)
            throw new ArgumentNullException(nameof(conn));

        if (!conns.Contains(conn))
            throw new ArgumentException("Connection is not connected to this server");
        
        queue.AddToQueue(() => RemoveConnection(conn));
    }

    /// <summary>
    /// Processes incoming network data and updates the state of all active network connections.
    /// This method retrieves and processes data from the receive pipe, attempting to find or register
    /// the appropriate connection for the received data. If a connection is found or created, the data
    /// is passed to the connection's receive handler. After processing the receive pipe, the state of all
    /// active connections is updated.
    /// </summary>
    /// <remarks>
    /// Exceptions encountered during data processing or connection updates are logged but do not interrupt
    /// the execution of the method or processing of other connections.
    /// </remarks>
    public void Update()
    {
        queue.UpdateQueue();
        
        if (recvPipe is { Size: > 0 })
        {
            while (recvPipe.Grab(out var data))
            {
                try
                {
                    var ip = (IPEndPoint)data.Args.RemoteEndPoint;
                    var conn = FindConnection(ip);

                    if (ip.Address == IPAddress.Any && ip.Port == 0)
                    {
                        log.Warn($"Received data from invalid IP: {ip} ({data.Args.RemoteEndPoint})");
                        
                        recvPipe.Return(data);
                        continue;
                    }
                    
                    log.DebugIf($"Received {data.Reader.Count} bytes from {ip}", debugLogs);

                    if (conn == null)
                        conn = RegisterConnection(ip);

                    conn.Receive(data.Reader);
                }
                catch (Exception ex)
                {
                    log.Error($"Could not process received data:\n{ex}");
                }
                
                recvPipe.Return(data);
            }
        }

        var array = conns;

        for (var x = 0; x < array.Length; x++)
        {
            var conn = array[x];

            try
            {
                conn.Update();
            }
            catch (Exception ex)
            {
                log.Error($"Could not update connection:\n{ex}");
            }

            if (conn.Ping.IsTimedOut)
            {
                log.DebugIf($"Connection &1{conn.EndPoint}&r timed out, removing", debugLogs);
                
                RemoveConnection(conn);
            }
        }
    }

    private void Send()
    {
        void Completed(object _, SocketAsyncEventArgs args)
        {
            if (args.UserToken is SendData data)
                sendPool.Enqueue(data);

            if (args.SocketError != SocketError.Success
                && args.RemoteEndPoint is IPEndPoint endPoint)
            {
                log.Error($"Send failed ({endPoint}): {args.SocketError}");
                
                if (FindConnection(endPoint) is { } conn)
                {
                    log.DebugIf("Removing connection due to send failure", debugLogs);
                    
                    RemoveConnection(conn);
                }
                else
                {
                    log.DebugIf("Connection not found, skipping", debugLogs);
                }
            }
            
            Interlocked.Add(ref sentBytes, args.BytesTransferred);
            
            log.DebugIf($"Sent {args.BytesTransferred} bytes ({sentBytes} total)", debugLogs);
        }
        
        SendData GetData()
        {
            if (!sendPool.TryDequeue(out var data))
            {
                data = new();
                data.Args.Completed += Completed;
            }

            data.Writer.Position = 0;
            return data;
        }

        while (!cts.IsCancellationRequested)
        {
            Thread.Sleep(1);
            
            try
            {
                var array = conns;
                
                for (var x = 0; x < array.Length; x++)
                {
                    var conn = array[x];

                    if (!conn.IsValid || !conn.IsRunning)
                        continue;

                    if (!conn.HasData)
                        continue;
                    
                    var data = GetData();
                    
                    log.DebugIf($"Connection &1{conn.EndPoint}&r has data, serializing ..", debugLogs);

                    try
                    {
                        if (!conn.TryWrite(data.Writer))
                        {
                            log.DebugIf($"Connection &1{conn.EndPoint}&r is not ready to send data, queuing ..", debugLogs);
                            
                            sendPool.Enqueue(data);
                            continue;
                        }

                        data.Args.RemoteEndPoint = conn.serverSendEndPoint;
                        data.Args.SetBuffer(data.Args.Buffer, 0, data.Writer.Position);

                        log.DebugIf($"Sending &1{data.Writer.Position}&r bytes to &1{conn.EndPoint}&r ({conn.serverSendEndPoint}) ..", debugLogs);

                        var pending = socket.SendToAsync(data.Args);
                        
                        if (!pending)
                            Completed(null!, data.Args);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error sending data to &1{conn.EndPoint}&r:\n{ex}");
                    }

                    sendPool.Enqueue(data);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }

    private void RemoveConnection(NetConnection conn)
    {
        queue.AddToQueue(() =>
        {
            log.DebugIf($"Removing connection {conn.Id}", debugLogs);
            
            try
            {
                conn.Stop();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            Disconnected?.Invoke(conn);
        });

        conns = conns
            .Except([conn])
            .ToArray();
    }

    private NetConnection? FindConnection(IPEndPoint endPoint)
    {
        for (var x = 0; x < conns.Length; x++)
        {
            var conn = conns[x];

            if (conn.EndPoint.Equals(endPoint))
                return conn;
        }

        return null;
    }

    private NetConnection RegisterConnection(IPEndPoint endPoint)
    {
        log.DebugIf($"Registering new connection: {endPoint}", debugLogs);
        
        var conn = new NetConnection(this, endPoint, Interlocked.Increment(ref connId));
        
        conn.Start();

        conns = conns
            .Append(conn)
            .ToArray();
        
        ProvidedServices.ForEach(t => conn.AddService(t, []));
        
        Connected?.Invoke(conn);
        return conn;
    }
}