using System.Net;

namespace NiveraAPI.Networking.Interfaces;

/// <summary>
/// Represents a network client.
/// </summary>
public interface INetworkClient : IDisposable
{
    /// <summary>
    /// Whether the client is connected.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// The local peer of the client.
    /// </summary>
    Peer? LocalPeer { get; }
    
    /// <summary>
    /// The local endpoint of the client.
    /// </summary>
    IPEndPoint? LocalEndPoint { get; }
    
    /// <summary>
    /// The remote endpoint of the client.
    /// </summary>
    IPEndPoint? RemoteEndPoint { get; }

    /// <summary>
    /// Disconnects the client.
    /// </summary>
    void Disconnect();
}