using NiveraAPI.IO.Serialization.Interfaces;

namespace NiveraAPI.IO.Network.API;

/// <summary>
/// Represents a message that has been retransmitted.
/// </summary>
public struct RetransmittedMessage
{
    /// <summary>
    /// The amount of retransmissions that have occured.
    /// </summary>
    public readonly int Count;
    
    /// <summary>
    /// The message that was retransmitted.
    /// </summary>
    public readonly ISerializableObject Message;
    
    /// <summary>
    /// Creates a new instance of the RetransmittedMessage struct.
    /// </summary>
    public RetransmittedMessage(int count, ISerializableObject message)
    {
        Count = count;
        Message = message;
    }
}