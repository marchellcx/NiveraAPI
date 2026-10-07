using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

namespace NiveraAPI.IO.Network.Entities.Messages;

/// <summary>
/// Represents a message sent by the server to spawn an entity.
/// </summary>
public struct EntitySpawnMessage : ISerializableObject
{
    /// <summary>
    /// The name of the type of entity to spawn.
    /// </summary>
    public string Type;
    
    /// <summary>
    /// The ID of the entity to spawn.
    /// </summary>
    public ushort Id;

    /// <summary>
    /// Creates a new instance of the EntitySpawnMessage class.
    /// </summary>
    public EntitySpawnMessage(string type, ushort id)
    {
        Type = type;
        Id = id;
    }

    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<EntitySpawnMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteString(Type);
        writer.WriteUInt16(Id);
    }
    
    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        Type = reader.ReadString();
        Id = reader.ReadUInt16();
    }
}