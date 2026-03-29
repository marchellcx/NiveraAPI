using System.Net;
using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.Networking.Interfaces;

/// <summary>
/// Represents a network server.
/// </summary>
public interface INetworkServer : IServiceCollection
{
    /// <summary>
    /// Event triggered when the server starts.
    /// </summary>
    event Action? Started;

    /// <summary>
    /// Event triggered when the server stops.
    /// </summary>
    event Action? Stopped; 
    
    /// <summary>
    /// Event triggered when a new peer connects to the server.
    /// </summary>
    event Action<Peer>? Connected;
    
    /// <summary>
    /// Event triggered when a peer disconnects from the server.
    /// </summary>
    event Action<Peer>? Disconnected;
    
    /// <summary>
    /// The local endpoint of the server.
    /// </summary>
    IPEndPoint? LocalEndPoint { get; }
    
    /// <summary>
    /// Gets or sets the types of services that the server will provide.
    /// </summary>
    Type[] ProvidedServices { get; set; }
    
    /// <summary>
    /// Gets a list of all connected peers.
    /// </summary>
    IReadOnlyList<Peer> Peers { get; }

    /// <summary>
    /// Checks if the specified peer is currently connected to the network.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> instance to check for a network connection.</param>
    /// <returns>True if the peer is connected; otherwise, false.</returns>
    bool IsConnected(Peer peer);

    /// <summary>
    /// Retrieves the network endpoint associated with the specified peer.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> instance for which to retrieve the endpoint.</param>
    /// <returns>The <see cref="IPEndPoint"/> associated with the peer if available; otherwise, null.</returns>
    IPEndPoint? GetEndPoint(Peer peer);

    /// <summary>
    /// Disconnects the specified peer from the network.
    /// </summary>
    /// <param name="peer">The <see cref="Peer"/> instance to be disconnected.</param>
    void Disconnect(Peer peer);
}