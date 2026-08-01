using System.Net;
using System.Net.Sockets;

using NiveraAPI.IO.Network.API.Internal;
using NiveraAPI.IO.Network.API.Internal.Tcp;
using NiveraAPI.IO.Network.API.Internal.Udp;
using NiveraAPI.Logs;
using NiveraAPI.Services;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network;

/// <summary>
/// Represents a network client capable of handling connections, sending, and receiving data
/// over a network. This class manages the lifecycle of a network connection, provides logging,
/// and internal state tracking while facilitating communication with a remote endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The <c>NetClient</c> class extends the <see cref="ServiceCollection"/> type,
/// inheriting service-related functionality. It focuses on networking features and exposes
/// various methods for connecting to a remote server, managing communication pipelines,
/// and interacting with internal mechanisms such as the send and receive pipelines.
/// </para>
/// <para>
/// Thread safety is ensured for key operations through the use of volatile fields
/// and threading mechanisms where applicable.
/// </para>
/// </remarks>
public class NetClient : ServiceCollection
{
    #region UDP fields
    private volatile bool udpConnecting;
    private volatile bool udpConnected;
    private volatile bool udpRequireData;
    internal volatile bool udpGotData = false;
    private volatile int udpRecvThreads = 8;
    
    private volatile Socket udpSocket;
    private volatile IPEndPoint udpCurrent;
    
    private volatile UdpClientRecvPipe udpRecvPipe;
    private volatile UdpClientSendPipe udpSendPipe;
    
    private volatile CancellationTokenSource udpSendCts;
    private volatile CancellationTokenSource udpConnectCts;
    #endregion
    
    #region TCP fields
    private volatile bool tcpConnecting;
    private volatile bool tcpConnected;

    private volatile IPEndPoint tcpCurrent;
    
    private volatile TcpClient tcpClient;
    private volatile TcpClientSendPipe tcpSendPipe;
    private volatile TcpClientRecvPipe tcpRecvPipe;
    #endregion
    
    internal volatile bool debugLogs;
    internal volatile bool isUdpMode;

    private volatile LogSink log = LogManager.GetSource("IO", "NetClient");

    internal volatile ActionQueue queue = new();

    /// <summary>
    /// Gets called when the client successfully establishes a connection to a remote server.
    /// </summary>
    public event Action? Connected;

    /// <summary>
    /// Gets called when the client is disconnected from the remote server.
    /// </summary>
    public event Action? Disconnected;

    /// <summary>
    /// Gets or sets the number of threads used for handling UDP packet reception.
    /// This property determines the concurrency level when processing incoming UDP data,
    /// with higher values potentially improving throughput under heavy load.
    /// </summary>
    public int UdpReceiveThreads
    {
        get => udpRecvThreads;
        set => udpRecvThreads = value;
    }

    /// <summary>
    /// Determines whether the client requires receiving data packets through the UDP protocol
    /// before considering the connection successfully initialized.
    /// </summary>
    public bool UdpRequireData
    {
        get => udpRequireData;
        set => udpRequireData = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether debug logs are enabled for the network client.
    /// </summary>
    public bool DebugLogs
    {
        get => debugLogs;
        set => debugLogs = value;
    }

    /// <summary>
    /// Gets the logging mechanism associated with the network client.
    /// </summary>
    public LogSink Log => log;
    
    /// <summary>
    /// Gets or sets the maximum number of retransmissions allowed for a message that does not have a handler assigned until it is discarded.
    /// </summary>
    public int MaxRetransmissions { get; set; }
    
    /// <summary>
    /// Whether the client is currently using UDP for communication.
    /// </summary>
    public bool IsUsingUdp => isUdpMode;

    /// <summary>
    /// Whether the client is currently attempting to connect to a remote server.
    /// </summary>
    public bool IsConnecting => isUdpMode ? udpConnecting : tcpConnecting;
    
    /// <summary>
    /// Whether the client is currently connected to a remote server.
    /// </summary>
    public bool IsConnected => isUdpMode ? udpConnected : tcpConnected;

    /// <summary>
    /// Gets the total number of bytes sent by the network client's send pipeline.
    /// </summary>
    public long SentBytes => isUdpMode ? udpSendPipe.SentBytes : tcpSendPipe.sentBytes;

    /// <summary>
    /// Gets the total number of bytes received by the client through the network pipeline.
    /// </summary>
    public long ReceivedBytes => isUdpMode ? udpRecvPipe.ReceivedBytes : tcpRecvPipe.receivedBytes;
    
    /// <summary>
    /// Gets the network connection associated with the client.
    /// </summary>
    public NetConnection? Connection { get; private set; }

    /// <summary>
    /// List of services that should be added to a newly created connection.
    /// </summary>
    public List<Type> Services { get; } = new();

    /// <summary>
    /// Updates the state of the client by processing necessary network operations
    /// for either UDP or TCP based on the current mode.
    /// </summary>
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

    /// <summary>
    /// Establishes a connection to the specified endpoint using either TCP or UDP based on the provided parameter.
    /// </summary>
    /// <param name="target">The endpoint to which the connection will be established.</param>
    /// <param name="useUdp">Indicates whether to use UDP (true) or TCP (false) for the connection.</param>
    public void Connect(IPEndPoint target, bool useUdp)
    {
        isUdpMode = useUdp;

        if (isUdpMode)
        {
            UdpConnect(target);
        }
        else
        {
            TcpConnect(target);
        }
    }

    /// <summary>
    /// Disconnects the client socket if a connection is currently established.
    /// Ensures that the socket is safely disconnected and logs any errors
    /// encountered during the disconnection process.
    /// </summary>
    public void Disconnect()
    {
        if (isUdpMode)
        {
            UdpDisconnect();
        }
        else
        {
            TcpDisconnect();
        }
    }

    /// <inheritdoc />
    public override void Stop()
    {
        base.Stop();

        if (isUdpMode)
        {
            UdpStop();
        }
        else
        {
            TcpStop();
        }
    }

    #region TCP Networking
    private void TcpStop()
    {
        log.DebugIf("Stopping client ..", debugLogs);
        
        Disconnect();
        
        queue.ClearQueue();
    }

    private void TcpConnect(IPEndPoint target)
    {
        if (tcpClient != null)
            Disconnect();
        
        tcpCurrent = target;

        tcpClient = new();
            
        tcpClient.SendBufferSize = NetSettings.MTU;
        tcpClient.ReceiveBufferSize = NetSettings.MTU;

        tcpConnected = false;
        tcpConnecting = true;
            
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    log.DebugIf($"Connecting to &1{target}&r ..", debugLogs);
                        
                    await Task.Delay(1000);
                    await tcpClient.ConnectAsync(target.Address, target.Port);
                        
                    tcpConnected = true;
                    tcpConnecting = false;
                        
                    log.DebugIf("Connected!", debugLogs);

                    queue.AddToQueue(TcpOnConnected);
                    break;
                }
                catch (Exception ex)
                {
                    log.Error($"Error while connecting:\n{ex}");
                }
            }
        });
    }

    private void TcpDisconnect()
    {
        try
        {
            log.DebugIf("Disconnecting ..", debugLogs);

            tcpConnected = false;
            tcpConnecting = false;
            
            if (Connection != null)
            {
                Disconnected?.Invoke();

                RemoveService(typeof(NetConnection));
                
                Connection = null;
            }

            try
            {
                if (tcpClient is { Connected: true })
                    tcpClient.Close();
                
                tcpClient.Dispose();
                tcpClient = null!;
            }
            catch
            {
                // ignored
            }
            
            tcpRecvPipe?.Stop();
            tcpRecvPipe = null!;
            
            tcpSendPipe?.Stop();
            tcpSendPipe = null!;
            
            StopAllServices();
        }
        catch (Exception ex)
        {
            log.Error($"Could not disconnect!\n{ex}");
        }
    }

    private void TcpOnConnected()
    {
        log.DebugIf("Setting up local connection ..", debugLogs);
        
        tcpRecvPipe = new(tcpClient, this);
        tcpRecvPipe.Start();
        
        log.DebugIf("TcpRecvPipe started", debugLogs);

        tcpSendPipe = new(tcpClient, this);
        tcpSendPipe.Start();
        
        log.DebugIf("TcpSendPipe started", debugLogs);

        Connection = new(this, tcpCurrent, tcpClient, 0);

        AddService(Connection);
        
        log.DebugIf("Connection started", debugLogs);
        
        Services.ForEach(t => Connection.AddService(t, []));

        log.DebugIf("Services added", debugLogs);
        
        ThreadPool.QueueUserWorkItem(_ => TcpInternalUpdate());
        
        log.DebugIf("Update thread started", debugLogs);
        
        Connected?.Invoke();
    }

    internal void TcpOnSendPipeError(Exception ex)
    {
        log.Error($"TcpSendPipe received an error: &1{ex.Message}&r, disconnecting client!");
        
        if (ex != null)
            log.Error(ex);
        
        Disconnect();
    }

    internal void TcpOnReceivePipeError(Exception ex)
    {
        log.Error($"TcpRecvPipe received an error: &1{ex.Message}&r, disconnecting client!");
        
        if (ex != null)
            log.Error(ex);
        
        Disconnect();
    }

    private void TcpUpdate()
    {
        try
        {
            queue.UpdateQueue();

            if (Connection != null)
            {
                while (tcpRecvPipe.TryGrab(out var data))
                {
                    log.DebugIf($"Processing received data: {data.Count} bytes", debugLogs);
                    
                    try
                    {
                        Connection.Receive(data); // in theory this should never throw because it's wrapped in a try-catch
                                                  // block itself but just in case
                    }
                    finally
                    {
                        tcpRecvPipe.Return(data);
                    }
                }
                
                Connection.Update();
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed to process action queue:\n{ex}");
        }
    }
    
    private void TcpInternalUpdate()
    {
        while (tcpClient != null)
        {
            Thread.Sleep(1);
            
            try
            {
                if (Connection is { HasData: true })
                {
                    log.DebugIf($"There is data available to send", debugLogs);
                    
                    var writer = tcpSendPipe.GetWriter();

                    if (Connection.TryWrite(writer))
                        tcpSendPipe.Send(writer);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error while updating connection send:\n{ex}");
            }
        }
        
        log.DebugIf($"Update thread exited", debugLogs);
    }
    #endregion
    
    #region UDP Networking
    private void UdpConnect(IPEndPoint target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (udpConnecting)
            throw new Exception("The client is already attempting to connect ..");

        udpConnecting = true;
        udpConnectCts = new CancellationTokenSource();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            log.DebugIf("Connecting thread started", debugLogs);
            
            while (!udpConnected)
            {
                try
                {
                    udpGotData = false;
                    
                    udpSocket?.Dispose();
                    
                    log.DebugIf($"Connecting to {target} ..", debugLogs);

                    udpSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    udpSocket.Blocking = false;

                    udpSocket.SendBufferSize = NetSettings.MTU;
                    udpSocket.ReceiveBufferSize = NetSettings.MTU;
                    
                    udpSocket.Connect(target);

                    if (udpRequireData)
                    {
                        udpRecvPipe = new(this, udpSocket);
                        udpRecvPipe.Start();
                        
                        udpSocket.Send(new byte[] { 0x1 }, SocketFlags.None);

                        while (!udpGotData)
                            continue;
                    }

                    udpConnected = true;
                    udpConnecting = false;
                    udpCurrent = target;

                    queue.AddToQueue(UdpOnConnected);
                    
                    log.DebugIf("Connected!", debugLogs);
                }
                catch (Exception ex)
                {
                    log.Error($"Connect failed: {ex.Message}");
                }
            }
        });
    }
    
    private void UdpDisconnect()
    {
        try
        {
            udpGotData = false;
            
            log.DebugIf("Disconnecting ..", debugLogs);

            try
            {
                if (udpSendCts is { IsCancellationRequested: false })
                    udpSendCts.Cancel();
            }
            catch
            {
                // ignored
            }

            try
            {
                if (udpConnectCts is { IsCancellationRequested: false })
                    udpConnectCts.Cancel();
            }
            catch
            {
                // ignored
            }

            try
            {
                if (udpSocket is { Connected: true })
                    udpSocket.Disconnect(false);
            }
            catch
            {
                // ignored
            }

            if (Connection != null)
            {
                Disconnected?.Invoke();

                RemoveService(typeof(NetConnection));
                
                Connection = null;
            }
        }
        catch (Exception ex)
        {
            log.Error($"Could not disconnect!\n{ex}");
        }
    }

    private void UdpStop()
    {
        log.DebugIf("Stopping client ..", debugLogs);
        
        Disconnect();
        
        udpSendPipe.Stop();
        udpSendPipe = null!;
        
        udpRecvPipe.Stop();
        udpRecvPipe = null!;

        udpConnected = false;
        udpConnecting = false;
        
        queue.ClearQueue();

        try
        {
            if (udpSocket != null)
            {
                udpSocket.Close();
                udpSocket.Dispose();
            }
        }
        catch
        {
            // ignore
        }

        udpSocket = null!;
    }

    private void UdpUpdate()
    {
        try
        {
            queue.UpdateQueue();

            if (Connection != null)
            {
                while (udpRecvPipe.TryGrab(out var data))
                {
                    log.DebugIf($"Processing received data: {data.Reader.Count} bytes", debugLogs);
                    
                    try
                    {
                        Connection.Receive(data.Reader); // in theory this should never throw because it's wrapped in a try-catch
                                                         // block itself but just in case
                    }
                    finally
                    {
                        udpRecvPipe.Return(data);
                    }
                }
                
                Connection.Update();
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed to process action queue:\n{ex}");
        }
    }

    private void UdpOnConnected()
    {
        log.DebugIf("Setting up local connection ..", debugLogs);

        if (!udpRequireData)
        {
            udpRecvPipe = new(this, udpSocket);
            udpRecvPipe.Start();
            
            log.DebugIf("RecvPipe started", debugLogs);
        }

        udpSendPipe = new(this, udpSocket);
        
        log.DebugIf("SendPipe started", debugLogs);
        
        Connection = new(this, udpSocket, udpCurrent, 0);

        AddService(Connection);
        
        log.DebugIf("Connection started", debugLogs);
        
        Services.ForEach(t => Connection.AddService(t, []));

        log.DebugIf("Services added", debugLogs);
        
        udpSendCts = new();
        
        ThreadPool.QueueUserWorkItem(_ => UdpInternalUpdate());
        
        log.DebugIf("Update thread started", debugLogs);
        
        Connected?.Invoke();
    }

    internal void UdpOnSendPipeError(SocketError error, Exception ex)
    {
        log.Error($"SendPipe received an error: &1{error}&r, stopping client!");
        
        if (ex != null)
            log.Error(ex);
        
        Stop();
    }

    internal void UdpOnReceivePipeError(SocketError error, Exception ex)
    {
        log.Error($"RecvPipe received an error: &1{error}&r, stopping client!");
        
        if (ex != null)
            log.Error(ex);
        
        Stop();
    }

    private void UdpInternalUpdate()
    {
        while (!udpSendCts.IsCancellationRequested)
        {
            Thread.Sleep(1);
            
            try
            {
                if (Connection is { HasData: true })
                {
                    var writer = udpSendPipe.GetWriter();

                    if (Connection.TryWrite(writer))
                    {
                        udpSendPipe.Send(writer);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
    #endregion
}