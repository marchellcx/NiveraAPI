using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;
using NiveraAPI.Networking.Database.Enums;

namespace NiveraAPI.Networking.Database.Messages;

/// <summary>
/// Represents a message used for retrieving a value from the database.
/// </summary>
public struct DbActionMessage : ISerializableObject
{
    /// <summary>
    /// The ID of the request.
    /// </summary>
    public ushort Id;

    /// <summary>
    /// The table to perform the action on.
    /// </summary>
    public string? Table;

    /// <summary>
    /// The item to perform the action on.
    /// </summary>
    public string? Item;

    /// <summary>
    /// Additional data.
    /// </summary>
    public byte[]? Data;
    
    /// <summary>
    /// The action to perform.
    /// </summary>
    public DbAction Action;

    /// <summary>
    /// The time the request was sent.
    /// </summary>
    public long UtcTicks;

    /// <summary>
    /// Creates a new instance of the DbActionMessage struct.
    /// </summary>
    /// <param name="id">The ID of the request.</param>
    /// <param name="action">The action to perform.</param>
    /// <param name="table">The table to perform the action on.</param>
    /// <param name="item">The item to perform the action on.</param>
    /// <param name="data">Additional data.</param>
    public DbActionMessage(ushort id, DbAction action, string? table, string? item, byte[]? data = null)
    {
        Id = id;
        Action = action;
        Table = table;
        Item = item;
        Data = data;
    }
    
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => DefaultSerializer<DbActionMessage>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public void Serialize(ByteWriter writer)
    {
        writer.WriteByte((byte)Action);
        writer.WriteUInt16(Id);
        writer.WriteInt64(UtcTicks);

        switch (Action)
        {
            case DbAction.AddNewItem
                or DbAction.UpdateExistingItem
                or DbAction.UpdateExistingOrAddItem:
                writer.WriteString(Table);
                writer.WriteString(Item);
                writer.WriteBytes(Data);
                break;
            
            case DbAction.AddTable
                or DbAction.ClearTable
                or DbAction.RemoveTable:
                writer.WriteString(Table);
                break;
            
            case DbAction.RemoveItem:
                writer.WriteString(Table);
                writer.WriteString(Item);
                break;
        }
    }

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public void Deserialize(ByteReader reader)
    {
        Action = (DbAction)reader.ReadByte();
        Id = reader.ReadUInt16();
        UtcTicks = reader.ReadInt64();
        
        switch (Action)
        {
            case DbAction.AddNewItem
                or DbAction.UpdateExistingItem
                or DbAction.UpdateExistingOrAddItem:
                Table = reader.ReadString();
                Item = reader.ReadString();
                Data = reader.ReadBytes();
                break;
                
            case DbAction.AddTable
                or DbAction.ClearTable
                or DbAction.RemoveTable:
                Table = reader.ReadString();
                break;
            
            case DbAction.RemoveItem:
                Table = reader.ReadString();
                Item = reader.ReadString();
                break;
        }
    }
}