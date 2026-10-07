using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

namespace NiveraAPI.IO.Network.Entities.Messages;

/// <summary>
/// Represents a message that wraps data and targets a specific entity by its ID.
/// </summary>
/// <remarks>
/// This struct is used for network communication purposes within the NiveraAPI system.
/// It contains the ID of the targeted entity and the associated data payload to be sent.
/// </remarks>
public struct EntityWrappedMessage : ISerializableObject
{
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<EntityWrappedMessage>.Singleton;

    /// <summary>
    /// The ID of the targeted entity.
    /// </summary>
    public ushort Id;

    /// <summary>
    /// The data to be sent to the entity.
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// Creates a new instance of the EntityWrappedMessage struct.
    /// </summary>
    /// <param name="id">The ID of the targeted entity.</param>
    /// <param name="data">The data to be sent to the entity.</param>
    /// <exception cref="ArgumentNullException">Thrown when the data parameter is null.</exception>
    public EntityWrappedMessage(ushort id, byte[] data)
    {
        Id = id;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteUInt16(Id);
        writer.WriteBytes(Data);
    }

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        Id = reader.ReadUInt16();
        Data = reader.ReadBytes();
    }
}