using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

namespace NiveraAPI.Networking.Database.Messages;

/// <summary>
/// Represents a message used for authenticating a user against the database.
/// </summary>
public struct DbAuthMessage : ISerializableObject
{
    /// <summary>
    /// The user to authenticate.
    /// </summary>
    public string User;

    /// <summary>
    /// The password to authenticate with.
    /// </summary>
    public string Password;

    /// <summary>
    /// Creates a new instance of the DbAuthMessage struct.
    /// </summary>
    /// <param name="user">The user to authenticate.</param>
    /// <param name="password">The password to authenticate with.</param>
    public DbAuthMessage(string user, string password)
    {
        User = user;
        Password = password;
    }
    
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<DbAuthMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteString(User);
        writer.WriteString(Password);
    }

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        User = reader.ReadString();
        Password = reader.ReadString();
    }
}