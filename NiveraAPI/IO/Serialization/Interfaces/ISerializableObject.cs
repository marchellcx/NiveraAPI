namespace NiveraAPI.IO.Serialization.Interfaces;

/// <summary>
/// Defines a contract for serializable objects, enabling them to convert their state
/// into a sequence of bytes and reconstruct their state from a sequence of bytes.
/// </summary>
public interface ISerializableObject
{
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    IObjectSerializer Serializer { get; }
    
    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    void Serialize(ByteWriter writer);

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    void Deserialize(ByteReader reader);
}