using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;
using NiveraAPI.Console;
using NiveraAPI.IO.Network.API.Internal;
using NiveraAPI.IO.Network.API.Internal.Udp;
using NiveraAPI.Logs;
using NiveraAPI.Services;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network;

/// <summary>
/// Represents a networking server that supports both TCP and UDP communication modes,
/// allowing the management of connections, data transmission, and service provisioning.
/// </summary>
public class NetServer : ServiceCollection
{
    private static volatile LogSink log = LogManager.GetSource("IO", "NetServer");

    #region UDP fields
    private long udpSentBytes;
    private volatile int udpRecvThreads = 8;
    
    private volatile Socket udpSocket;
    private volatile UdpServerRecvPipe udpRecvPipe;
    private volatile CancellationTokenSource udpCts;
    
    private volatile ConcurrentQueue<UdpSendData> udpSendPool = new();
    #endregion
    
    #region TCP fields
    private volatile TcpListener tcpListener;
    
    private volatile CancellationTokenSource tcpSendCts;
    private volatile CancellationTokenSource tcpConnectCts;
    #endregion

    internal volatile bool isUdpMode;
    internal volatile bool debugLogs;
    
    private volatile int connId = 0;
    private volatile Predicate<TcpClient> tcpPredicate;

    private volatile ActionQueue queue = new();
    private volatile ConcurrentDictionary<int, NetConnection> conns = new();

    /// <summary>
    /// Gets called when a new connection is established.
    /// </summary>
    public event Action<NetConnection>? Connected;

    /// <summary>
    /// Gets called when a connection is disconnected.
    /// </summary>
    public event Action<NetConnection>? Disconnected; 
    
    /// <summary>
    /// The number of threads used for receiving data.
    /// </summary>
    public int UdpReceiveThreads
    {
        get => udpRecvThreads;
        set => udpRecvThreads = value;
    }
    
    /// <summary>
    /// Gets the total number of bytes sent by the server.
    /// </summary>
    public long SentBytes => udpSentBytes;

    /// <summary>
    /// Gets the total number of bytes received by the server.
    /// </summary>
    public long ReceivedBytes => udpRecvPipe?.ReceivedBytes ?? 0;
    
    /// <summary>
    /// Gets or sets the maximum number of retransmissions allowed for a message that does not have a handler assigned until it is discarded.
    /// </summary>
    public int MaxRetransmissions { get; set; }

    /// <summary>
    /// Whether debug logs are enabled.
    /// </summary>
    public bool DebugLogs
    {
        get => debugLogs;
        set => debugLogs = value;
    }

    /// <summary>
    /// Gets or sets a predicate that determines whether a TCP client connection is accepted.
    /// </summary>
    /// <remarks>
    /// This property allows filtering incoming TCP client connections based on custom logic.
    /// The assigned predicate must return true to accept the connection or false to reject it.
    /// If set to null, an <see cref="ArgumentNullException"/> will be thrown.
    /// </remarks>
    public Predicate<TcpClient> TcpPredicate
    {
        get => tcpPredicate;
        set => tcpPredicate = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Whether the server is currently using UDP for communication.
    /// </summary>
    public bool IsUsingUdp => isUdpMode;

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
    public IReadOnlyDictionary<int, NetConnection> Connections => conns;

    /// <summary>
    /// Starts listening for incoming connections on the specified port, using TCP or UDP based on the provided configuration.
    /// </summary>
    /// <param name="port">The port number to listen on. If set to 0, an available port will be selected automatically.</param>
    /// <param name="useUdp">Specifies whether to use UDP (true) or TCP (false) for communication.</param>
    public void Listen(int port = 0, bool useUdp = true)
    {
        isUdpMode = useUdp;

        if (useUdp)
        {
            UdpListen(port);
        }
        else
        {
            TcpListen(port);
        }
    }

    /// <inheritdoc />
    public override void Stop()
    {
        if (isUdpMode)
        {
            UdpStop();
        }
        else
        {
            TcpStop();
        }
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
        
        queue.AddToQueue(() => RemoveConnection(conn));
    }

    /// <summary>
    /// Disconnects all currently connected clients by removing their connections from the server's connection list.
    /// This operation ensures that no active connections remain.
    /// </summary>
    public void DisconnectAll()
    {
        foreach (var kvp in conns)
        {
            log.DebugIf($"Removing connection {kvp.Key}", debugLogs);
            
            try
            {
                if (kvp.Value.IsRunning)
                {
                    kvp.Value.Stop();

                    Disconnected?.Invoke(kvp.Value);
                }

                if (kvp.Value.tcpClient != null)
                {
                    try
                    {
                        kvp.Value.tcpClient.Close();
                        kvp.Value.tcpClient = null!;
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    kvp.Value.tcpSendPipe?.Stop();
                    kvp.Value.tcpSendPipe = null!;
                    
                    kvp.Value.tcpRecvPipe?.Stop();
                    kvp.Value.tcpRecvPipe = null!;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        conns.Clear();
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
        if (isUdpMode)
        {
            UdpUpdate();
        }
        else
        {
            TcpUpdate();
        }
    }
    
    private void RemoveConnection(NetConnection conn)
    {
        if (!conns.TryRemove(conn.Id, out _))
            return;
        
        queue.AddToQueue(() =>
        {
            log.DebugIf($"Removing connection {conn.Id}", debugLogs);
            
            try
            {
                if (conn.IsRunning)
                {
                    conn.Stop();
                    
                    Disconnected?.Invoke(conn);
                }
                
                if (conn.tcpClient != null)
                {
                    try
                    {
                        conn.tcpClient.Close();
                        conn.tcpClient = null!;
                    }
                    catch
                    {
                        // ignored
                    }
                    
                    conn.tcpSendPipe?.Stop();
                    conn.tcpSendPipe = null!;
                    
                    conn.tcpRecvPipe?.Stop();
                    conn.tcpRecvPipe = null!;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        });
    }
    
    #region TCP Networking
    private void TcpStop()
    {
        log.DebugIf("Stopping server...", debugLogs);
        
        base.Stop();
        
        DisconnectAll();
        
        tcpSendCts.Cancel();
        tcpConnectCts.Cancel();

        try
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
                tcpListener = null!;
            }
        }
        catch
        {
            // ignored
        }
    }
    
    private void TcpSend(object obj)
    {
        var cts = (CancellationTokenSource)obj;
        
        while (!cts.IsCancellationRequested)
        {
            Thread.Sleep(1);
            
            try
            {
                foreach (var kvp in conns)
                {
                    if (!kvp.Value.IsValid || !kvp.Value.IsRunning)
                        continue;

                    if (!kvp.Value.HasData)
                        continue;
                    
                    if (kvp.Value.tcpSendPipe == null)
                        continue;
                    
                    log.DebugIf($"Connection &1{kvp.Value.EndPoint}&r has data, serializing ..", debugLogs);
                    
                    var writer = kvp.Value.tcpSendPipe.GetWriter();

                    try
                    {
                        if (!kvp.Value.TryWrite(writer))
                        {
                            log.DebugIf($"Connection &1{kvp.Value.EndPoint}&r is not ready to send data, queuing ..", debugLogs);
                            
                            kvp.Value.tcpSendPipe.ReturnWriter(writer);
                        }
                        else
                        {
                            kvp.Value.tcpSendPipe.Send(writer);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error sending data to &1{kvp.Value.EndPoint}&r:\n{ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
    
    private void TcpUpdate()
    {
        queue.UpdateQueue();
        
        foreach (var kvp in conns)
        {
            try
            {
                if (kvp.Value.tcpRecvPipe == null)
                {
                    log.Warn($"Encountered connection with a null RecvPipe!");
                    continue;
                }

                while (kvp.Value.tcpRecvPipe.TryGrab(out var reader))
                {
                    try
                    {
                        kvp.Value.Receive(reader);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Could not process incoming data for connection &1{kvp.Value.EndPoint}&r:\n{ex}");
                        
                        kvp.Value.tcpRecvPipe.Return(reader);
                        continue;
                    }

                    kvp.Value.tcpRecvPipe.Return(reader);
                }
                
                kvp.Value.Update();
            }
            catch (Exception ex)
            {
                log.Error($"Could not update connection:\n{ex}");
            }
        }
    }

    private void TcpListen(int port)
    {
        log.DebugIf($"Starting server on port {port}...", debugLogs);

        if (tcpListener != null)
        {
            DisconnectAll();

            try
            {
                tcpListener.Stop();
            }
            catch
            {
                // ignored
            }
        }

        tcpSendCts?.Cancel();
        tcpConnectCts?.Cancel();

        tcpSendCts = new();
        tcpConnectCts = new();
        
        tcpListener = new(IPAddress.Any, port);
        tcpListener.ExclusiveAddressUse = false;
        
        tcpListener.Start();
        
        ThreadPool.QueueUserWorkItem(TcpSend, tcpSendCts);
        ThreadPool.QueueUserWorkItem(TcpAccept, tcpConnectCts);
    }

    private void TcpRegister(TcpClient client)
    {
        log.DebugIf($"Registering new connection: {client.Client.RemoteEndPoint}", debugLogs);
        
        var conn = new NetConnection(this, client.Client.RemoteEndPoint as IPEndPoint, Interlocked.Increment(ref connId));

        conn.tcpClient = client;
        conn.clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

        conn.tcpSendPipe = new(client, conn);
        conn.tcpSendPipe.Start();
        
        conn.tcpRecvPipe = new(client, conn);
        conn.tcpRecvPipe.Start();
        
        conn.Start();
        
        conns.TryAdd(conn.Id, conn);
        
        ProvidedServices.ForEach(t => conn.AddService(t, []));
        
        Connected?.Invoke(conn);
    }

    private void TcpAccept(object obj)
    {
        var cts = (CancellationTokenSource)obj;

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var client = tcpListener.AcceptTcpClient();

                if (client != null)
                {
                    try
                    {
                        if (tcpPredicate != null && !tcpPredicate(client))
                        {
                            client.Close();
                            client.Dispose();
                            
                            return;
                        }
                        
                        client.NoDelay = true;

                        client.SendBufferSize = NetSettings.MTU;
                        client.ReceiveBufferSize = NetSettings.MTU;

                        client.ExclusiveAddressUse = false;
                    }
                    catch (Exception ex)
                    {
                         ConsoleOutput.Write($"Error while accepting client:\n{ex}", ConsoleColor.DarkRed);
                    }

                    queue.AddToQueue(() => TcpRegister(client));
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
    #endregion

    #region UDP Networking
    private void UdpListen(int port)
    {
        log.DebugIf($"Starting server on port {port}...", debugLogs);
        
        if (udpSocket != null)
            Stop();
        
        if (!IsRunning)
            Start();

        connId = 0;
        udpSentBytes = 0;
        
        log.DebugIf("Creating socket...", debugLogs);
        
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpSocket.Blocking = false;
        
        udpSocket.SendBufferSize = NetSettings.MTU;
        udpSocket.ReceiveBufferSize = NetSettings.MTU;
        
        log.DebugIf("Binding socket...", debugLogs);
        
        udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        udpSocket.Bind(new IPEndPoint(IPAddress.Any, port));

        log.DebugIf($"Bound to port {port}", debugLogs);
        
        udpRecvPipe = new(this, udpSocket);
        udpRecvPipe.Start();
        
        log.DebugIf("RecvPipe started", debugLogs);

        udpCts = new();
        
        ThreadPool.QueueUserWorkItem(_ => UdpSend());
        
        log.DebugIf("Send thread started", debugLogs);
    }

    private void UdpStop()
    {
        log.DebugIf("Stopping server...", debugLogs);
        
        base.Stop();
        
        udpCts.Cancel();
        
        log.DebugIf("Stopping RecvPipe", debugLogs);

        if (udpRecvPipe != null)
        {
            udpRecvPipe.Stop();
            udpRecvPipe = null!;
        }
        
        log.DebugIf("Stopping connections...", debugLogs);

        foreach (var kvp in conns)
        {
            try
            {
                kvp.Value.Stop();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        try
        {
            if (udpSocket != null)
            {
                log.DebugIf("Closing socket...", debugLogs);
                
                udpSocket.Close();
                udpSocket.Dispose();
            }
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }
        
        log.DebugIf("Clearing send pool", debugLogs);

        while (udpSendPool.TryDequeue(out var data))
        {
            data.Args.Dispose();
            data.Writer.ReturnToPool();
        }

        udpSocket = null!;

        conns.Clear();
        
        log.DebugIf("Server stopped", debugLogs);
    }
    
    private void UdpUpdate()
    {
        queue.UpdateQueue();
        
        if (udpRecvPipe is { Size: > 0 })
        {
            while (udpRecvPipe.Grab(out var data))
            {
                try
                {
                    var ip = (IPEndPoint)data.Args.RemoteEndPoint;
                    var conn = UdpFindConnection(ip);

                    if (ip.Address == IPAddress.Any && ip.Port == 0)
                    {
                        log.Warn($"Received data from invalid IP: {ip} ({data.Args.RemoteEndPoint})");
                        
                        udpRecvPipe.Return(data);
                        continue;
                    }
                    
                    log.DebugIf($"Received {data.Reader.Count} bytes from {ip}", debugLogs);

                    if (conn == null)
                    {
                        conn = UdpRegisterConnection(ip);
                    }

                    conn.Receive(data.Reader);
                }
                catch (Exception ex)
                {
                    log.Error($"Could not process received data:\n{ex}");
                }
                
                udpRecvPipe.Return(data);
            }
        }
        
        foreach (var kvp in conns)
        {
            try
            {
                kvp.Value.Update();
            }
            catch (Exception ex)
            {
                log.Error($"Could not update connection:\n{ex}");
            }

            if (kvp.Value.Ping.IsTimedOut)
            {
                log.DebugIf($"Connection &1{kvp.Value.EndPoint}&r timed out, removing", debugLogs);
                
                RemoveConnection(kvp.Value);
            }
        }
    }

    private void UdpSend()
    {
        void Completed(object _, SocketAsyncEventArgs args)
        {
            if (args.UserToken is UdpSendData data)
                udpSendPool.Enqueue(data);

            if (args.SocketError != SocketError.Success
                && args.RemoteEndPoint is IPEndPoint endPoint)
            {
                log.Error($"Send failed ({endPoint}): {args.SocketError}");
                
                if (UdpFindConnection(endPoint) is { } conn)
                {
                    log.DebugIf("Removing connection due to send failure", debugLogs);
                    
                    RemoveConnection(conn);
                }
                else
                {
                    log.DebugIf("Connection not found, skipping", debugLogs);
                }
            }
            
            Interlocked.Add(ref udpSentBytes, args.BytesTransferred);
            
            log.DebugIf($"Sent {args.BytesTransferred} bytes ({udpSentBytes} total)", debugLogs);
        }
        
        UdpSendData GetData()
        {
            if (!udpSendPool.TryDequeue(out var data))
            {
                data = new();
                data.Args.Completed += Completed;
            }

            data.Writer.Position = 0;
            return data;
        }

        while (!udpCts.IsCancellationRequested)
        {
            Thread.Sleep(1);
            
            try
            {
                var array = conns;
                
                foreach (var kvp in conns)
                {
                    if (!kvp.Value.IsValid || !kvp.Value.IsRunning)
                        continue;

                    if (!kvp.Value.HasData)
                        continue;
                    
                    var data = GetData();
                    
                    log.DebugIf($"Connection &1{kvp.Value.EndPoint}&r has data, serializing ..", debugLogs);

                    try
                    {
                        if (!kvp.Value.TryWrite(data.Writer))
                        {
                            log.DebugIf($"Connection &1{kvp.Value.EndPoint}&r is not ready to send data, queuing ..", debugLogs);
                            
                            udpSendPool.Enqueue(data);
                            continue;
                        }

                        data.Args.RemoteEndPoint = kvp.Value.serverSendEndPoint;
                        data.Args.SetBuffer(data.Args.Buffer, 0, data.Writer.Position);

                        log.DebugIf($"Sending &1{data.Writer.Position}&r bytes to &1{kvp.Value.EndPoint}&r ({kvp.Value.serverSendEndPoint}) ..", debugLogs);

                        var pending = udpSocket.SendToAsync(data.Args);
                        
                        if (!pending)
                            Completed(null!, data.Args);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error sending data to &1{kvp.Value.EndPoint}&r:\n{ex}");
                    }

                    udpSendPool.Enqueue(data);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
    
    private NetConnection? UdpFindConnection(IPEndPoint endPoint)
    {
        foreach (var kvp in conns)
        {
            if (kvp.Value.EndPoint.Equals(endPoint))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private NetConnection UdpRegisterConnection(IPEndPoint endPoint)
    {
        log.DebugIf($"Registering new connection: {endPoint}", debugLogs);
        
        var conn = new NetConnection(this, endPoint, Interlocked.Increment(ref connId));
        
        conn.Start();
        
        conns.TryAdd(conn.Id, conn);
        
        ProvidedServices.ForEach(t => conn.AddService(t, []));
        
        Connected?.Invoke(conn);
        return conn;
    }
    #endregion
}