using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

namespace NiveraAPI.Networking.Entities.Messages;

/// <summary>
/// Represents a message sent by the server to a client to perform an RPC or a CMD.
/// </summary>
public struct EntityInvokeMessage : ISerializableObject
{
    /// <summary>
    /// The conversation ID.
    /// </summary>
    public byte Id;

    /// <summary>
    /// Whether the message is an RPC or a CMD.
    /// </summary>
    public bool Rpc;

    /// <summary>
    /// The ID of the entity to perform the RPC or CMD on.
    /// </summary>
    public ushort Entity;

    /// <summary>
    /// Index of the RPC or CMD.
    /// </summary>
    public short Index;

    /// <summary>
    /// The data to be sent or that was received.
    /// </summary>
    public byte[]? Data;
    
    /// <summary>
    /// Creates a new instance of the <see cref="EntityInvokeMessage"/> struct.
    /// </summary>
    /// <param name="id">The unique identifier for the RPC or CMD message.</param>
    /// <param name="rpc">Whether the message is an RPC or a CMD.</param>
    /// <param name="entity">The ID of the entity to perform the RPC or CMD on.</param>
    /// <param name="index">The index of the RPC or CMD.</param>
    /// <param name="data">The data to be sent or that was received.</param>   
    public EntityInvokeMessage(byte id, bool rpc, ushort entity, short index, byte[]? data = null)
    {
        Id = id;
        Rpc = rpc;
        Entity = entity;
        Index = index;
        Data = data;
    }

    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<EntityInvokeMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteByte(Id);
        writer.WriteBool(Rpc);
        writer.WriteUInt16(Entity);
        writer.WriteInt16(Index);
        
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
        Id = reader.ReadByte();
        Rpc = reader.ReadBool();
        Entity = reader.ReadUInt16();
        Index = reader.ReadInt16();

        if (reader.ReadByte() == 0)
            Data = reader.ReadBytes();
    }
}