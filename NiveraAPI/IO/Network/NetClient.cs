using System.Net;
using System.Net.Sockets;

using NiveraAPI.IO.Network.API.Internal;

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
    private volatile int recvThreads = 8;

    private volatile Socket socket;
    private volatile IPEndPoint current;
    
    private volatile CancellationTokenSource sendCts;
    private volatile CancellationTokenSource connectCts;

    private volatile ClientRecvPipe recvPipe;
    private volatile ClientSendPipe sendPipe;

    private volatile bool connecting;
    private volatile bool connected;

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
    /// Gets or sets the number of threads used by the receive pipeline in the network client.
    /// </summary>
    /// <remarks>
    /// This property determines how many threads will be allocated to handle data received from the network.
    /// Adjusting this value can impact the concurrency and performance of message processing.
    /// </remarks>
    public int ReceiveThreads
    {
        get => recvThreads;
        set => recvThreads = value;
    }

    /// <summary>
    /// Gets the logging mechanism associated with the network client.
    /// </summary>
    public LogSink Log => log;
    
    /// <summary>
    /// Gets the send pipeline associated with the network client.
    /// </summary>
    public ClientSendPipe SendPipe => sendPipe;

    /// <summary>
    /// Gets the receive pipeline associated with the network client.
    /// </summary>
    public ClientRecvPipe RecvPipe => recvPipe;

    /// <summary>
    /// Whether the client is currently attempting to connect to a remote server.
    /// </summary>
    public bool IsConnecting => connecting;
    
    /// <summary>
    /// Whether the client is currently connected to a remote server.
    /// </summary>
    public bool IsConnected => connected;
    
    /// <summary>
    /// Gets the total number of bytes sent by the network client's send pipeline.
    /// </summary>
    public long SentBytes => sendPipe.SentBytes;

    /// <summary>
    /// Gets the total number of bytes received by the client through the network pipeline.
    /// </summary>
    public long ReceivedBytes => recvPipe.ReceivedBytes;
    
    /// <summary>
    /// Gets the network connection associated with the client.
    /// </summary>
    /// <remarks>
    /// This property represents the active network connection managed by the client.
    /// It provides access to the underlying <c>NetConnection</c> instance,
    /// allowing interaction with the established connection. The property is read-only
    /// and will be set internally when a connection is successfully established.
    /// </remarks>
    public NetConnection? Connection { get; private set; }

    /// <summary>
    /// List of services that should be added to a newly created connection.
    /// </summary>
    public List<Type> Services { get; } = new();

    /// <summary>
    /// Updates the internal action queue by processing and executing queued actions.
    /// Invokes the <see cref="ActionQueue.UpdateQueue"/> method to handle the queued actions.
    /// </summary>
    public void Update()
    {
        try
        {
            queue.UpdateQueue();

            if (Connection != null)
            {
                while (recvPipe.TryGrab(out var data))
                {
                    log.Debug($"Processing received data: {data.Reader.Count} bytes");
                    
                    try
                    {
                        Connection.Receive(data.Reader); // in theory this should never throw because it's wrapped in a try-catch
                                                         // block itself but just in case
                    }
                    finally
                    {
                        recvPipe.Return(data);
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

    /// <summary>
    /// Initiates a connection to a remote server using the specified target endpoint.
    /// </summary>
    /// <param name="target">The <see cref="IPEndPoint"/> of the remote server to connect to.</param>
    /// <exception cref="Exception">Thrown when a connection attempt is already in progress.</exception>
    public void Connect(IPEndPoint target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (connecting)
            throw new Exception("The client is already attempting to connect ..");

        connecting = true;
        connectCts = new CancellationTokenSource();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            log.Debug("Connecting thread started");
            
            while (!connected)
            {
                try
                {
                    socket?.Dispose();
                    
                    log.Debug($"Connecting to {target} ..");

                    socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.Blocking = false;

                    socket.SendBufferSize = NetSettings.MTU;
                    socket.ReceiveBufferSize = NetSettings.MTU;
                    
                    socket.Connect(target);

                    connected = true;
                    connecting = false;

                    current = target;

                    queue.AddToQueue(OnConnected);
                    
                    log.Debug($"Connected!");
                }
                catch (Exception ex)
                {
                    log.Error($"Connect failed: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// Disconnects the client socket if a connection is currently established.
    /// Ensures that the socket is safely disconnected and logs any errors
    /// encountered during the disconnection process.
    /// </summary>
    public void Disconnect()
    {
        try
        {
            log.Debug("Disconnecting ..");

            if (sendCts is { IsCancellationRequested: false })
                sendCts.Cancel();
            
            if (connectCts is { IsCancellationRequested: false })
                connectCts.Cancel();
            
            if (socket is { Connected: true })
                socket.Disconnect(false);

            if (Connection != null)
            {
                Disconnected?.Invoke();
                
                Connection.Stop();
                Connection = null;
            }
        }
        catch (Exception ex)
        {
            log.Error($"Could not disconnect!\n{ex}");
        }
    }

    /// <inheritdoc />
    public override void Stop()
    {
        base.Stop();
        
        log.Debug("Stopping client ..");
        
        Disconnect();
        
        sendPipe.Stop();
        sendPipe = null!;
        
        recvPipe.Stop();
        recvPipe = null!;

        connected = false;
        connecting = false;
        
        queue.ClearQueue();

        try
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
            }
        }
        catch
        {
            // ignore
        }

        socket = null!;
    }

    private void OnConnected()
    {
        log.Debug("Setting up local connection ..");
        
        recvPipe = new(this, socket);
        recvPipe.Start();
        
        log.Debug("RecvPipe started");

        sendPipe = new(this, socket);
        
        log.Debug("SendPipe started");
        
        Connection = new(this, socket, current, 0);
        Connection.Start();
        
        log.Debug("Connection started");
        
        Services.ForEach(t => Connection.AddService(t, []));

        log.Debug("Services added");
        
        sendCts = new();
        
        ThreadPool.QueueUserWorkItem(_ => InternalUpdate());
        
        log.Debug("Update thread started");
        
        Connected?.Invoke();
    }

    internal void OnSendPipeError(SocketError error, Exception ex)
    {
        log.Error($"SendPipe received an error: &1{error}&r, stopping client!");
        
        if (ex != null)
            log.Error(ex);
        
        Stop();
    }

    internal void OnReceivePipeError(SocketError error, Exception ex)
    {
        log.Error($"RecvPipe received an error: &1{error}&r, stopping client!");
        
        if (ex != null)
            log.Error(ex);
        
        Stop();
    }

    private void InternalUpdate()
    {
        while (!sendCts.IsCancellationRequested)
        {
            Thread.Sleep(1);
            
            try
            {
                if (Connection is { HasData: true })
                {
                    var writer = sendPipe.GetWriter();

                    if (Connection.TryWrite(writer))
                    {
                        sendPipe.Send(writer);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}