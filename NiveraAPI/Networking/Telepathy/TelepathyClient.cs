using System.Net;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Logs;
using NiveraAPI.Networking.Interfaces;
using Telepathy;

namespace NiveraAPI.Networking.Telepathy;

/// <summary>
/// Represents a client implementation for the Telepathy networking library.
/// Handles network connectivity and communication with remote endpoints.
/// Implements the <see cref="INetworkClient"/> interface.
/// </summary>
public class TelepathyClient : INetworkClient
{
    // telepathy does not expose a local endpoint, so we have to "fake" one ourselves
    private static IPEndPoint localEP = new(IPAddress.Loopback, 0);
    private static LogSink log = LogManager.GetSource("Networking", "TelepathyClient");
        
    private Peer? peer;
    private Client? client;
    private IPEndPoint? endPoint;
    
    /// <summary>
    /// Gets called when the client successfully connects to a remote endpoint.
    /// </summary>
    public event Action? Connected;

    /// <summary>
    /// Gets called when the client disconnects from the remote endpoint.
    /// </summary>
    public event Action? Disconnected;

    /// <summary>
    /// Whether the client is connected.
    /// </summary>
    public bool IsConnected => client != null && client.Connected;

    /// <summary>
    /// The local peer of the client.
    /// </summary>
    public Peer? LocalPeer => peer;

    /// <summary>
    /// The local endpoint of the client.
    /// </summary>
    public IPEndPoint? LocalEndPoint => IsConnected ? localEP : null;

    /// <summary>
    /// The remote endpoint of the client.
    /// </summary>
    public IPEndPoint? RemoteEndPoint => IsConnected ? endPoint : null;

    /// <summary>
    /// Gets or sets a value indicating whether the Nagle's algorithm is disabled for the connection.
    /// When set to <c>true</c>, small packets are sent immediately without waiting to aggregate data.
    /// This can enhance responsiveness for low-latency applications at the cost of increased network overhead.
    /// </summary>
    public bool NoDelay { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of messages that can be queued for sending.
    /// If the queue reaches this limit, additional messages may be discarded or delayed
    /// until space becomes available.
    /// </summary>
    public int SendQueueLimit { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum number of messages that can be stored in the receive queue.
    /// This limit helps control memory usage and prevents overflow by capping the number of inbound messages
    /// awaiting processing.
    /// </summary>
    public int ReceiveQueueLimit { get; set; } = 10000;

    /// <summary>
    /// Limits how many messages can be processed in a single tick.
    /// </summary>
    public int ProcessLimit { get; set; } = 100;

    /// <summary>
    /// Specifies the types of services that should be registered with the local peer.
    /// </summary>
    public Type[]? Services { get; set; } = Array.Empty<Type>();

    /// <summary>
    /// Initializes and starts the Telepathy client if it has not been created.
    /// Configures the client with event handlers for data reception, connection, and disconnection.
    /// </summary>
    public void Start()
    {
        if (client == null)
        {
            log.Info("Starting ...");
            
            client = new(TelepathySettings.MAX_MSG_SIZE);

            client.NoDelay = NoDelay;
            client.SendQueueLimit = SendQueueLimit;
            client.ReceiveQueueLimit = ReceiveQueueLimit;
            
            client.OnData = OnData;
            client.OnConnected = OnConnected;
            client.OnDisconnected = OnDisconnected;
            
            LibraryUpdate.Register(Update);
            
            log.Info("Started.");
        }
    }

    /// <summary>
    /// Establishes a connection to the specified remote endpoint using the Telepathy client.
    /// Throws an exception if the target is null, the client is not started, or if already connected.
    /// </summary>
    /// <param name="target">The remote endpoint to connect to, represented by an <see cref="IPEndPoint"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when the target endpoint is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the client is not started or already connected.</exception>
    public void Connect(IPEndPoint target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (client == null)
            throw new InvalidOperationException("Client not started.");
        
        if (client.Connected)
            throw new InvalidOperationException("Already connected.");

        endPoint = target;

        try
        {
            // like bruh
            client.Connect(target.Address.ToString(), target.Port);
        }
        catch (Exception ex)
        {
            log.Error($"Failed to connect to &1{target}&r:\n{ex}");

            endPoint = null;
        }
    }

    /// <summary>
    /// Attempts to reconnect the Telepathy client to the last used or a new target endpoint.
    /// If the client is already connected, it will first disconnect before reconnecting.
    /// </summary>
    /// <param name="newTarget">An optional <see cref="IPEndPoint"/> to specify a new target for the reconnection. If omitted, the last known endpoint is used.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the client is not started, or if no previous endpoint exists and <paramref name="newTarget"/> is null.
    /// </exception>
    public void Reconnect(IPEndPoint? newTarget = null)
    {
        if (client == null)
            throw new InvalidOperationException("Client not started.");

        if (endPoint == null && newTarget == null)
            throw new InvalidOperationException("Cannot reconnect without a target endpoint.");
        
        if (!client.Connected)
        {
            Connect(newTarget ?? endPoint);
            return;
        }
        
        Disconnect();
        
        Connect(newTarget ?? endPoint);
    }

    /// <summary>
    /// Disconnects the Telepathy client if it is currently connected.
    /// Ensures that the local peer and client are properly stopped and cleaned up.
    /// Logs any errors that occur during the disconnection process.
    /// </summary>
    public void Disconnect()
    {
        if (client is { Connected: true })
        {
            Disconnected?.Invoke();
            
            try
            {
                if (peer != null)
                {
                    peer.Stop();
                    peer = null;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to stop local peer:\n{ex}");
            }

            try
            {
                client.Disconnect();
            }
            catch (Exception ex)
            {
                log.Error($"Failed to disconnect client:\n{ex}");
            }

            peer = null;
            endPoint = null;
        }
    }

    /// <summary>
    /// Disposes of the resources used by the Telepathy client.
    /// Closes any active connections, clears event handlers, and resets internal state.
    /// Ensures that all references to the client, tick thread, and peer are nullified.
    /// </summary>
    public void Dispose()
    {
        Disconnect();

        LibraryUpdate.Unregister(Update);
        
        if (client != null)
        {
            client.OnData = null;
            client.OnConnected = null;
            client.OnDisconnected = null;
        }

        client = null;
    }
    
    private void Update()
    {
        try
        {
            if (client != null)
            {
                client.Tick(ProcessLimit);

                if (peer != null)
                {
                    peer.Update();
                    peer.WriteOutput();

                    if (peer.Writer.Position > 0)
                        client.Send(peer.Writer.ToSegment());
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed while updating local peer:\n{ex}");
        }
    }

    private void OnData(ArraySegment<byte> data)
    {
        try
        {
            if (data.Array == null || data.Count < 1)
            {
                log.Warn("Received invalid data!");
                return;
            }
            
            if (peer == null)
            {
                log.Warn("Received data while not connected!");
                return;
            }

            peer.Reader.Reset(data);
            peer.ReadInput();
        }
        catch (Exception ex)
        {
            log.Error($"Failed to process incoming data:\n{ex}");
        }
    }

    private void OnConnected()
    {
        try
        {
            peer = new(0, TelepathySettings.PING_INTERVAL, this, Services);
            peer.Start();
            
            log.Debug($"Connected to &1{endPoint}&r.");

            Connected?.Invoke();
        }
        catch (Exception ex)
        {
            log.Error($"Failed to start local peer:\n{ex}");
            
            Disconnect();
        }
    }

    private void OnDisconnected()
    {
        try
        {
            Disconnected?.Invoke();

            if (peer != null)
            {
                peer.Stop();
                peer = null;
            }
            
            log.Debug($"Disconnected from &3{endPoint}&r.");

            peer = null;
            endPoint = null;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to process disconnect:\n{ex}");
        }
    }
}