using System.Text;

using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;
using NiveraAPI.Networking.Database.Enums;

namespace NiveraAPI.Networking.Database.Messages;

/// <summary>
/// Represents a message used for retrieving a value from the database.
/// </summary>
public struct DbResponseMessage : ISerializableObject
{
    /// <summary>
    /// The ID of the request.
    /// </summary>
    public ushort Id;

    /// <summary>
    /// The data returned by the database.
    /// </summary>
    /// <remarks>Will be UTF-32 encoded exception if <see cref="Result"/> is equal to <see cref="DbResult.Exception"/></remarks>
    public byte[]? Data;

    /// <summary>
    /// The result of the database request.
    /// </summary>
    public DbResult Result;

    /// <summary>
    /// The time the request was sent.
    /// </summary>
    public long UtcReceived;

    /// <summary>
    /// The time the request was processed.
    /// </summary>
    public long UtcProcessed;
    
    /// <summary>
    /// Creates a new instance of the DbResponseMessage struct.
    /// </summary>
    /// <param name="id">The ID of the request.</param>
    /// <param name="utcReceived">The time the request was sent.</param>
    /// <param name="utcProcessed">The time the request was processed.</param>
    /// <param name="exception">The exception that occurred during the request.</param>   
    public DbResponseMessage(ushort id, long utcReceived, long utcProcessed, Exception exception)
    {
        Id = id;
        UtcReceived = utcReceived;
        UtcProcessed = utcProcessed;
        Result = DbResult.Exception;
        Data = Encoding.UTF32.GetBytes(exception.ToString());
    }

    /// <summary>
    /// Creates a new instance of the DbResponseMessage struct.
    /// </summary>
    /// <param name="id">The ID of the request.</param>
    /// <param name="utcReceived">The time the request was sent.</param>
    /// <param name="utcProcessed">The time the request was processed.</param>
    /// <param name="result">The result of the database request.</param>
    /// <param name="data">The data returned by the database.</param>
    public DbResponseMessage(ushort id, long utcReceived, long utcProcessed, DbResult result, byte[]? data = null)
    {
        Id = id;
        UtcReceived = utcReceived;
        UtcProcessed = utcProcessed;
        Result = result;
        Data = data;
    }
    
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<DbResponseMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteUInt16(Id);
        writer.WriteByte((byte)Result);
        writer.WriteInt64(UtcReceived);
        writer.WriteInt64(UtcProcessed);

        if (Result is DbResult.Ok or DbResult.Exception)
        {
            if (Data?.Length < 1)
            {
                writer.WriteByte(0);
            }
            else
            {
                writer.WriteByte(1);
                writer.WriteBytes(Data ?? Array.Empty<byte>());
            }
        }
    }

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        Id = reader.ReadUInt16();
        Result = (DbResult)reader.ReadByte();
        UtcReceived = reader.ReadInt64();
        UtcProcessed = reader.ReadInt64();
        
        if ((Result is DbResult.Ok or DbResult.Exception) && reader.ReadByte() == 1)
            Data = reader.ReadBytes();
        else
            Data = Array.Empty<byte>();
    }
}