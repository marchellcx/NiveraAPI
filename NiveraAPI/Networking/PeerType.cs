namespace NiveraAPI.Networking;

/// <summary>
/// Describes the type of peer.
/// </summary>
public enum PeerType
{
    /// <summary>
    /// A server-side peer.
    /// </summary>
    Peer,
    
    /// <summary>
    /// A local client-side peer.
    /// </summary>
    Local,
    
    /// <summary>
    /// A peer that has been disposed.
    /// </summary>
    Disposed
}