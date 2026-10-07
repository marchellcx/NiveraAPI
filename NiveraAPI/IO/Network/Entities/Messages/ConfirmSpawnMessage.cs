using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

namespace NiveraAPI.IO.Network.Entities.Messages;

/// <summary>
/// Represents a message sent by the client to confirm an entity spawn.
/// </summary>
public struct ConfirmSpawnMessage : ISerializableObject
{
    /// <summary>
    /// The ID of the entity to confirm spawn for.
    /// </summary>
    public ushort Id = 0;
    
    /// <summary>
    /// Creates a new instance of the ConfirmSpawnMessage class.
    /// </summary>
    /// <param name="id">The ID of the entity to confirm spawn for.</param>
    public ConfirmSpawnMessage(ushort id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<ConfirmSpawnMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteUInt16(Id);
    }
    
    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        Id = reader.ReadUInt16();
    }
}