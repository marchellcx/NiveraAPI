using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Networking.Interfaces;

namespace NiveraAPI.Networking;

/// <summary>
/// Provides extension methods for the <see cref="INetworkServer"/> class.
/// </summary>
public static class NetworkExtensions
{
    /// <summary>
    /// Sends the specified serializable object to all connected peers of the network server.
    /// </summary>
    /// <param name="server">The network server that manages the connected peers.</param>
    /// <param name="obj">The serializable object to be sent to all peers.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="server"/> or <paramref name="obj"/> is null.</exception>
    public static void SendToAll(this INetworkServer server, ISerializableObject obj)
    {
        if (server == null)
            throw new ArgumentNullException(nameof(server));
        
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        for (var i = 0; i < server.Peers.Count; i++)
            server.Peers[i].Send(obj);
    }

    /// <summary>
    /// Sends the specified serializable object to all connected peers of the network server
    /// for which the specified predicate evaluates to true.
    /// </summary>
    /// <param name="server">The network server that manages the connected peers.</param>
    /// <param name="obj">The serializable object to be sent to the filtered peers.</param>
    /// <param name="predicate">The condition used to filter the peers to which the object should be sent.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="server"/>, <paramref name="obj"/>, or <paramref name="predicate"/> is null.
    /// </exception>
    public static void SendToWhere(this INetworkServer server, ISerializableObject obj, Predicate<Peer> predicate)
    {
        if (server == null)
            throw new ArgumentNullException(nameof(server));

        if (obj == null)
            throw new ArgumentNullException(nameof(obj));
        
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        for (var i = 0; i < server.Peers.Count; i++)
        {
            var peer = server.Peers[i];
            
            if (predicate(peer))
            {
                server.Peers[i].Send(obj);
            }
        }
    }
}