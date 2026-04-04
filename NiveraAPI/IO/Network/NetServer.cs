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
        log.Debug($"Starting server on port {port}...");
        
        if (socket != null)
            Stop();
        
        if (!IsRunning)
            Start();

        connId = 0;
        sentBytes = 0;
        
        log.Debug("Creating socket...");
        
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        
        socket.SendBufferSize = NetSettings.MTU;
        socket.ReceiveBufferSize = NetSettings.MTU;
        
        log.Debug("Binding socket...");
        
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));

        log.Debug($"Bound to port {port}");
        
        recvPipe = new(this, socket);
        recvPipe.Start();
        
        log.Debug("RecvPipe started");

        cts = new();
        
        ThreadPool.QueueUserWorkItem(_ => Send());
        
        log.Debug("Send thread started");
    }

    /// <inheritdoc />
    public override void Stop()
    {
        log.Debug("Stopping server...");
        
        base.Stop();
        
        cts.Cancel();
        
        log.Debug("Stopping RecvPipe");

        if (recvPipe != null)
        {
            recvPipe.Stop();
            recvPipe = null!;
        }
        
        log.Debug("Stopping connections...");

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
                log.Debug("Closing socket...");
                
                socket.Close();
                socket.Dispose();
            }
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }
        
        log.Debug("Clearing send pool");

        while (sendPool.TryDequeue(out var data))
        {
            data.Args.Dispose();
            data.Writer.ReturnToPool();
        }

        socket = null!;

        conns = [];
        
        log.Debug("Server stopped");
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
                    
                    log.Debug($"Received {data.Reader.Count} bytes from {ip}");

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
                log.Debug($"Connection &1{conn.EndPoint}&r timed out, removing");
                
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
                    log.Debug("Removing connection due to send failure");
                    
                    RemoveConnection(conn);
                }
                else
                {
                    log.Debug("Connection not found, skipping");
                }
            }
            
            Interlocked.Add(ref sentBytes, args.BytesTransferred);
            
            log.Debug($"Sent {args.BytesTransferred} bytes ({sentBytes} total)");
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
                    
                    log.Debug($"Connection &1{conn.EndPoint}&r has data, serializing ..");

                    try
                    {
                        if (!conn.TryWrite(data.Writer))
                        {
                            log.Debug($"Connection &1{conn.EndPoint}&r is not ready to send data, queuing ..");
                            
                            sendPool.Enqueue(data);
                            continue;
                        }

                        data.Args.RemoteEndPoint = conn.serverSendEndPoint;
                        data.Args.SetBuffer(data.Args.Buffer, 0, data.Writer.Position);

                        log.Debug($"Sending &1{data.Writer.Position}&r bytes to &1{conn.EndPoint}&r ({conn.serverSendEndPoint}) ..");

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
            log.Debug($"Removing connection {conn.Id}");
            
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
        log.Debug($"Registering new connection: {endPoint}");
        
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