namespace NiveraAPI.Networking.Database.Enums;

/// <summary>
/// Represents the permissions granted to a database user.
/// </summary>
[Flags]
public enum DbPerms : byte
{
    /// <summary>
    /// No permissions.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Grants permission to clear a specific table within the database.
    /// </summary>
    ClearTable = 1,
    
    /// <summary>
    /// Grants permission to clear all tables within the database.
    /// </summary>
    ClearAllTables = 2,
    
    /// <summary>
    /// Grants permission to create a new table within the database.
    /// </summary>
    CreateTable = 4,
    
    /// <summary>
    /// Grants permission to delete a specific table within the database.
    /// </summary>
    DeleteTable = 8,
    
    /// <summary>
    /// Grants permission to add a new item to a specific table within the database.
    /// </summary>
    AddNewItem = 16,
    
    /// <summary>
    /// Grants permission to update an existing item within a specific table within the database.
    /// </summary>
    UpdateExistingItem = 32,
    
    /// <summary>
    /// Grants permission to remove an item from a specific table within the database.
    /// </summary>
    RemoveItem = 64,
    
    /// <summary>
    /// Grants permission to access a specific item within a specific table within the database.
    /// </summary>
    AccessItem = 128
}