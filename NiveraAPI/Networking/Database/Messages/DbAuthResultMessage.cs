using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

using NiveraAPI.Networking.Database.Enums;

namespace NiveraAPI.Networking.Database.Messages;

/// <summary>
/// Represents a message used for authenticating a user against the database.
/// </summary>
public struct DbAuthResultMessage : ISerializableObject
{
    /// <summary>
    /// Whether the authentication was successful.
    /// </summary>
    public bool IsOk;

    /// <summary>
    /// The permissions of the user.
    /// </summary>
    public DbPerms Permissions;

    public DbAuthResultMessage(bool isOk, DbPerms permissions)
    {
        IsOk = isOk;
        Permissions = permissions;
    }
    
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<DbAuthResultMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteBool(IsOk);
        writer.WriteByte((byte)Permissions);
    }

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        IsOk = reader.ReadBool();
        Permissions = (DbPerms)reader.ReadByte();
    }
}