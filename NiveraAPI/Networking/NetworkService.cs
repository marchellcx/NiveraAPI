using System.Net;

using NiveraAPI.IO.Serialization.Interfaces;

using NiveraAPI.Logs;
using NiveraAPI.Services;
using NiveraAPI.Networking.Interfaces;

namespace NiveraAPI.Networking;

/// <summary>
/// Represents a network service that can function as either a client or server, providing
/// various networking capabilities through a binding to a <see cref="Peer"/>.
/// </summary>
public class NetworkService : Service, INetworkService
{
    /// <summary>
    /// The peer associated with this network service.
    /// </summary>
    public Peer Peer { get; private set; }
    
    /// <summary>
    /// Gets the ID of the parent peer.
    /// </summary>
    public int PeerId => Peer?.Id ?? -1;
    
    /// <summary>
    /// Gets the latency (in milliseconds) of the parent peer.
    /// </summary>
    public float PeerLatency => Peer?.Latency ?? -1f;
    
    /// <summary>
    /// Whether or not this service is running under a server instance.
    /// </summary>
    public bool IsServer => Peer?.IsServer ?? false;

    /// <summary>
    /// Whether or not this service is running under a client instance.
    /// </summary>
    public bool IsClient => Peer?.IsClient ?? false;
    
    /// <summary>
    /// Whether or not the parent peer is currently connected.
    /// </summary>
    public bool IsConnected => Peer?.IsConnected ?? false;

    /// <summary>
    /// The number of ticks that have passed since the time sync packet was sent to the client.
    /// </summary>
    public long TimeTicks => Peer?.TimeTicks ?? 0;
    
    /// <summary>
    /// The number of seconds that have passed since the time sync packet was sent to the client.
    /// </summary>
    public float TimeSeconds => Peer?.TimeSeconds ?? 0;
    
    /// <summary>
    /// The number of milliseconds that have passed since the time sync packet was sent to the client.
    /// </summary>
    public float TimeMilliseconds => Peer?.TimeMilliseconds ?? 0;
    
    /// <summary>
    /// The client or server associated with this network service.
    /// </summary>
    public INetworkClient? Client => Peer?.Client;
    
    /// <summary>
    /// The server associated with this network service.
    /// </summary>
    public INetworkServer? Server => Peer?.Server;
    
    /// <summary>
    /// The local endpoint of the parent peer.
    /// </summary>
    public IPEndPoint? LocalEndPoint => Peer?.LocalEndPoint;
    
    /// <summary>
    /// The remote endpoint of the parent peer.
    /// </summary>
    public IPEndPoint? RemoteEndPoint => Peer?.RemoteEndPoint;

    /// <summary>
    /// Gets the log sink associated with this network service.
    /// </summary>
    public LogSink Log { get; private set; }
    
    /// <summary>
    /// Starts the service.
    /// </summary>
    public override void Start()
    {
        if (Collection is not Peer peer)
            throw new InvalidOperationException("NetworkService subtypes can only be added to a Peer!");
        
        base.Start();
        
        Peer= peer;
        
        Log = LogManager.GetSource("Networking", $"NetworkService_{GetType().Name}@{peer.Id}");
        Log.Info("Service started.");
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        
        Log.Info("Service stopped.");
    }

    /// <summary>
    /// Updates the state of the network service, allowing for adjustments based on local and network timing deltas.
    /// </summary>
    /// <param name="localDeltaTime">The time elapsed locally since the last update, in seconds.</param>
    /// <param name="networkDeltaTime">The time elapsed based on network synchronization since the last update, in seconds.</param>
    public virtual void Update(float localDeltaTime, float networkDeltaTime)
    {
        
    }

    /// <summary>
    /// Sends the specified serializable object to the connected peer.
    /// </summary>
    /// <param name="serializableObject">The object to be serialized and sent over the network.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no available peer to send the object.
    /// </exception>
    public void Send(ISerializableObject serializableObject)
        => Peer?.Send(serializableObject);

    /// <summary>
    /// Disconnects the parent peer from the network.
    /// </summary>
    public void Disconnect()
        => Peer?.Disconnect();

    /// <summary>
    /// Processes a given serializable object payload, determining if the payload can be handled by the network service.
    /// </summary>
    /// <param name="serializableObject">
    /// The payload object that implements the <see cref="ISerializableObject"/> interface.
    /// This object is intended to be processed by the network service.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the payload was successfully handled.
    /// Returns <c>true</c> if the payload was processed, otherwise <c>false</c>.
    /// </returns>
    public virtual bool HandlePayload(ISerializableObject serializableObject)
        => false;
}