using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

namespace NiveraAPI.IO.Network.Entities.Messages;

/// <summary>
/// Represents a message used for synchronizing a variable on an entity.
/// This class is responsible for serializing and deserializing data that is
/// used to update the state of a specific variable on an entity in the networked environment.
/// </summary>
public struct EntitySyncVarMessage : ISerializableObject
{
    /// <summary>
    /// The ID of the entity.
    /// </summary>
    public ushort Entity;

    /// <summary>
    /// The index of the sync variable.
    /// </summary>
    public ushort Index;

    /// <summary>
    /// The data to be sent or that was received.
    /// </summary>
    public byte[]? Data;

    /// <summary>
    /// Creates a new instance of the <see cref="EntitySyncVarMessage"/> struct.
    /// </summary>
    /// <param name="entity">The ID of the entity.</param>
    /// <param name="index">The index of the sync variable.</param>
    /// <param name="data">The data to be sent or that was received.</param>
    public EntitySyncVarMessage(ushort entity, ushort index, byte[]? data = null)
    {
        Entity = entity;
        Index = index;
        Data = data;
    }
    
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<EntitySyncVarMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteUInt16(Entity);
        writer.WriteUInt16(Index);

        if (Data?.Length > 0)
        {
            writer.WriteByte(0);
            writer.WriteBytes(Data);
        }
        else
        {
            writer.WriteByte(1);
        }
    }

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        Entity = reader.ReadUInt16();
        Index = reader.ReadUInt16();

        if (reader.ReadByte() == 0)
            Data = reader.ReadBytes();
    }
}