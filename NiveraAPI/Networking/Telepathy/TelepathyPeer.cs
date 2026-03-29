using System.Net;
using NiveraAPI.Logs;
using NiveraAPI.Networking.Interfaces;
using Telepathy;

namespace NiveraAPI.Networking.Telepathy;

/// <summary>
/// Represents a peer in a networking context utilizing the Telepathy framework.
/// This class extends the base <see cref="Peer"/> class and provides implementations specific to Telepathy-based communication.
/// </summary>
public class TelepathyPeer : Peer
{
    private IPEndPoint endPoint;
    private ConnectionState state;
    
    /// <summary>
    /// Gets the remote endpoint of the peer.
    /// </summary>
    public override IPEndPoint? RemoteEndPoint => endPoint;

    /// <summary>
    /// Gets a value indicating whether the peer is currently connected.
    /// </summary>
    public override bool IsConnected => state != null && state.client != null && state.client.Connected;

    /// <summary>
    /// Gets the interval at which the peer sends a ping packet to the server.
    /// </summary>
    public override int PingInterval => TelepathySettings.PING_INTERVAL;

    /// <summary>
    /// Creates a new instance of <see cref="Peer"/> representing a client-side peer.
    /// </summary>
    /// <param name="id">The unique identifier of the peer.</param>
    /// <param name="client">The <see cref="INetworkClient"/> instance to associate with the peer.</param>
    /// <param name="services">Optional array of service types provided by the peer.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/> is null.</exception>
    public TelepathyPeer(int id, INetworkClient client, Type[]? services = null) : base(id, 0, client, services)
        => throw new NotImplementedException();

    /// <summary>
    /// Creates a new instance of <see cref="Peer"/> representing a server-side peer.
    /// </summary>
    /// <param name="id">The unique identifier of the peer.</param>
    /// <param name="endPoint">The network endpoint associated with the peer.</param>
    /// <param name="state">The internal connection state.</param>
    /// <param name="server">The <see cref="INetworkServer"/> instance to associate with the peer.</param>
    /// <param name="services">Optional array of service types provided by the peer.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="server"/> is null.</exception>
    public TelepathyPeer(int id, IPEndPoint endPoint, ConnectionState state, INetworkServer server, Type[]? services = null) : base(id, server, services)
    {
        if (server is not TelepathyServer)
            throw new ArgumentException("Server must be an instance of TelepathyServer.");
        
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
    }
}