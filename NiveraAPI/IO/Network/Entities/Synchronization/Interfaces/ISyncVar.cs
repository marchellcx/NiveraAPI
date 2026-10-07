using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.Entities.Synchronization.Interfaces;

/// <summary>
/// Represents an interface for synchronized variables used in a networked entity synchronization system.
/// </summary>
public interface ISyncVar
{
    /// <summary>
    /// Removes the current synchronized variable from the synchronization system.
    /// </summary>
    void Remove();

    /// <summary>
    /// Confirms the current synchronized variable, typically to indicate that changes have been acknowledged
    /// or processed within the synchronization system.
    /// </summary>
    void Confirmed();

    /// <summary>
    /// Processes data read from a ByteReader instance.
    /// </summary>
    /// <param name="reader">The ByteReader instance providing serialized data to be deserialized and processed.</param>
    void Receive(ByteReader reader);
}