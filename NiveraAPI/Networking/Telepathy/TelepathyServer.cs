using System.Collections.Concurrent;
using System.Net;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Logs;
using NiveraAPI.Networking.Interfaces;
using NiveraAPI.Extensions;
using NiveraAPI.Services;
using Telepathy;

namespace NiveraAPI.Networking.Telepathy;

/// <summary>
/// Represents a server implementation using the Telepathy library.
/// </summary>
public class TelepathyServer : ServiceCollection, INetworkServer
{
    private static readonly LogSink? log = LogManager.GetSource("Networking", "TelepathyServer");
    
    private readonly List<TelepathyPeer> peers = new(16);
    
    private Server? server;
    private ConcurrentDictionary<int, ConnectionState> states;
    
    /// <summary>
    /// Gets called when the server is successfully started.
    /// </summary>
    public event Action? Started;

    /// <summary>
    /// Gets called when the server is successfully listening for incoming connections.
    /// </summary>
    public event Action<int>? Listening;
    
    /// <summary>
    /// Gets called when the server is stopped.
    /// </summary>
    public event Action? Stopped;
    
    /// <summary>
    /// Gets called when a new peer connects to the server.
    /// </summary>
    public event Action<Peer>? Connected;

    /// <summary>
    /// Gets called when a peer disconnects from the server.
    /// </summary>
    public event Action<Peer>? Disconnected;
    
    /// <summary>
    /// The local endpoint of the server.
    /// </summary>
    public IPEndPoint? LocalEndPoint => server?.listener?.LocalEndpoint as IPEndPoint;

    /// <summary>
    /// Indicates whether the server is currently listening for incoming connections.
    /// </summary>
    public bool IsListening => server != null 
                               && server.Active
                               && server.listener != null
                               && server.listener.Server != null
                               && server.listener.Server.IsBound;

    /// <summary>
    /// Gets or sets a value indicating whether the Nagle's algorithm is disabled for network communications.
    /// When set to true, small packets are sent immediately without waiting for more data to reduce latency.
    /// </summary>
    public bool NoDelay { get; set; } = true;

    /// <summary>
    /// Specifies the maximum number of messages that can be queued for sending.
    /// </summary>
    public int SendQueueLimit { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum number of messages allowed in the receive queue for a connection.
    /// </summary>
    public int ReceiveQueueLimit { get; set; } = 10000;

    /// <summary>
    /// Specifies the maximum number of messages that can be processed in a single tick.
    /// </summary>
    public int ProcessLimit { get; set; } = 100;

    /// <summary>
    /// Specifies the types of services that should be registered with all new peers.
    /// </summary>
    public Type[]? ProvidedServices { get; set; } = Array.Empty<Type>();
    
    /// <summary>
    /// Gets the number of connected peers.
    /// </summary>
    public int Connections => peers.Count;
    
    /// <summary>
    /// Gets an enumerable collection of connected peers.
    /// </summary>
    public IReadOnlyList<Peer> Peers => peers;

    /// <summary>
    /// Initializes and sets up the server instance if it is not already started.
    /// This includes allocating resources for managing server connections,
    /// configuring settings such as message size, and starting the server's background tick thread.
    /// </summary>
    public override void Start()
    {
        base.Start();
        
        if (server == null)
        {
            log.Debug("Starting ...");
            
            server = new(TelepathySettings.MAX_MSG_SIZE);
            
            states = typeof(Server)
                .FindField("clients")?
                .GetValue(server) as ConcurrentDictionary<int, ConnectionState> ?? throw new Exception("Failed to get the clients field.");
            
            server.NoDelay = NoDelay;
            server.SendQueueLimit = SendQueueLimit;
            server.ReceiveQueueLimit = ReceiveQueueLimit;
            
            server.OnData = OnData;
            server.OnConnected = OnConnected;
            server.OnDisconnected = OnDisconnected;
            
            LibraryUpdate.Register(Update);
            
            Started?.Invoke();
            
            log.Debug("Started.");
        }
    }

    /// <summary>
    /// Begins listening for incoming client connections on the specified port.
    /// This operation will fail if the server has not been started or if it is already in the listening state.
    /// Configures the endpoint for network communication and starts accepting connections.
    /// </summary>
    /// <param name="port">The port on which the server will listen for incoming connections. Defaults to 0, which lets the system select an available port.</param>
    /// <exception cref="InvalidOperationException">Thrown if the server is not started or is already listening.</exception>
    public void Listen(int port = 0)
    {
        if (server == null)
            throw new InvalidOperationException("Server is not started.");
        
        if (IsListening)
            throw new InvalidOperationException("Server is already listening.");
        
        if (!server.Start(port))
            throw new InvalidOperationException("Failed to start server.");
        
        Listening?.Invoke(port);
    }

    /// <summary>
    /// Disconnects the specified peer from the network.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> instance to be disconnected.</param>
    public void Disconnect(Peer peer)
    {
        if (server == null || peers.Count == 0)
            return;

        if (peer is not TelepathyPeer telepathyPeer || !peers.Remove(telepathyPeer))
            return;

        try
        {
            peer.Stop();
        }
        catch (Exception ex)
        {
            log.Error($"Failed to stop peer {peer.Id}:\n{ex}");
        }

        try
        {
            server.Disconnect(peer.Id);
        }
        catch (Exception ex)
        {
            log.Error($"Could not disconnect peer &1{peer.Id}&r:\n{ex}");
        }
    }

    /// <summary>
    /// Disconnects all active client connections managed by the server.
    /// This involves stopping each connected peer and ensuring the server releases associated resources.
    /// Any errors encountered during the disconnection process are logged.
    /// </summary>
    public void Disconnect()
    {
        if (server == null || peers.Count == 0)
            return;

        foreach (var peer in peers.ToArray())
        {
            try
            {
                if (!peer.IsRunning)
                    continue;
                
                Disconnected?.Invoke(peer);
                
                peer.Stop();
                
                server.Disconnect(peer.Id);
            }
            catch (Exception ex)
            {
                (peer.Log ?? log).Error($"Failed to stop peer:\n{ex}");
            }
        }
        
        peers.Clear();
    }

    /// <summary>
    /// Stops the server and disconnects all connected peers.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        
        Disconnect();
        
        LibraryUpdate.Unregister(Update);

        if (server != null)
        {
            try
            {
                server.Stop();
                
                Stopped?.Invoke();
            }
            catch (Exception ex)
            {
                log.Error($"Failed to stop the server:\n{ex}");
            }

            server = null;
        }
    }
    
    /// <summary>
    /// Checks if the specified peer is currently connected to the network.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> instance to check for a network connection.</param>
    /// <returns>True if the peer is connected; otherwise, false.</returns>
    public bool IsConnected(Peer peer)
    {
        if (peer is not TelepathyPeer telepathyPeer)
            return false;

        return telepathyPeer.IsConnected && telepathyPeer.IsRunning;
    }

    /// <summary>
    /// Retrieves the network endpoint associated with the specified peer.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> instance for which to retrieve the endpoint.</param>
    /// <returns>The <see cref="IPEndPoint"/> associated with the peer if available; otherwise, null.</returns>
    public IPEndPoint? GetEndPoint(Peer peer)
    {
        if (peer is not TelepathyPeer telepathyPeer)
            return null;
        
        return telepathyPeer.RemoteEndPoint;
    }
    
    private void Update()
    {
        if (server != null)
        {
            try
            {
                server.Tick(ProcessLimit);

                for (var x = 0; x < peers.Count; x++)
                {
                    var peer = peers[x];
                    
                    peer.Update();
                    peer.WriteOutput();

                    if (peer.Writer.Position > 0)
                        server.Send(peer.Id, peer.Writer.ToSegment());
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to process update:\n{ex}");
            }
        }
    }

    private void OnData(int connectionId, ArraySegment<byte> data)
    {
        if (data.Array == null || data.Count < 1)
        {
            log.Warn("Received invalid data!");
            return;
        }
        
        var peer = peers.Find(p => p.Id == connectionId);
        
        if (peer == null)
        {
            log.Error($"Received data for an unknown peer: &1{connectionId}&r");
            return;
        }

        try
        {
            peer.Reader.Reset(data);
            peer.ReadInput();
        }
        catch (Exception ex)
        {
            (peer.Log ?? log).Error($"Failed to read data from peer &1{connectionId}&r: {ex.Message}");
        }
    }

    private void OnConnected(int connectionId, string _)
    {
        if (!states.TryGetValue(connectionId, out var state))
        {
            log.Error($"Failed to get state for connection &1{connectionId}&r");
            return;
        }

        if (state.client == null)
        {
            log.Error($"Client for connection &1{connectionId}&r is null");
            return;
        }

        if (state.client.Client == null)
        {
            log.Error($"Socket for connection &1{connectionId}&r is null");
            return;
        }

        if (state.client.Client.RemoteEndPoint is not IPEndPoint endPoint)
        {
            log.Error($"Remote endpoint for connection &1{connectionId}&r is not an IP endpoint");
            return;
        }

        try
        {
            var peer = new TelepathyPeer(connectionId, endPoint, state, this, ProvidedServices);

            peers.Add(peer);

            peer.Start();

            Connected?.Invoke(peer);
            
            (peer.Log ?? log).Debug($"Connected to &1{peer.RemoteEndPoint}&r");
        }
        catch (Exception ex)
        {
            log.Error($"Failed to handle connection &1{connectionId}&r (&6{endPoint}&r):\n{ex}");
        }
    }
    
    private void OnDisconnected(int connectionId)
    {
        var peer = peers.Find(p => p.Id == connectionId);
        
        if (peer == null)
        {
            log.Error($"Received disconnect for unknown connection &1{connectionId}&r");
            return;
        }
        
        peers.Remove(peer);
        
        try
        {
            Disconnected?.Invoke(peer);
            
            peer.Stop();
            
            (peer.Log ?? log).Debug($"Disconnected from &1{peer.RemoteEndPoint}&r");
        }
        catch (Exception ex)
        {
            log.Error($"Failed to disconnect peer &1{connectionId}&r:\n{ex}");
        }
    }
}