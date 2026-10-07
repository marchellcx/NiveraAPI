namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// Represents the data type contained in an entity wrapped message.
/// </summary>
public enum EntityHeader : byte
{
    /// <summary>
    /// Data contained in the message is a message.
    /// </summary>
    Message = 0,
    
    /// <summary>
    /// Data contained in the message is a remote method invocation.
    /// </summary>
    Method = 1,
    
    /// <summary>
    /// Data contained in the message is a remote method invocation response.
    /// </summary>
    Response = 2,
    
    /// <summary>
    /// Data contained in the message is a sync var update.
    /// </summary>
    SyncVar = 3,

    /// <summary>
    /// Data contained in the message is related to synchronizing the state of a parent entity
    /// within the networked synchronization hierarchy.
    /// </summary>
    SyncParentMessage = 4,

    /// <summary>
    /// Data contained in the message is a payload for a sync object.
    /// </summary>
    SyncObjectPayload = 5,
}