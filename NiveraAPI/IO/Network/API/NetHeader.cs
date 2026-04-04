namespace NiveraAPI.IO.Network.API;

/// <summary>
/// Represents the header of a network packet.
/// </summary>
public enum NetHeader : byte
{
    /// <summary>
    /// Ping packet.
    /// </summary>
    Ping,
    
    /// <summary>
    /// Time packet.
    /// </summary>
    Time,
    
    /// <summary>
    /// Message packet.
    /// </summary>
    Message,
    
    /// <summary>
    /// Connect packet.
    /// </summary>
    Connect,
    
    /// <summary>
    /// Disconnect packet.
    /// </summary>
    Disconnect
}