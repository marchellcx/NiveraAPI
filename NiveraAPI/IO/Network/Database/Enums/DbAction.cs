namespace NiveraAPI.IO.Network.Database.Enums;

/// <summary>
/// Represents an action to perform on a database.
/// </summary>
public enum DbAction : byte
{
    /// <summary>
    /// Get an integer value.
    /// </summary>
    GetInt64,
    
    /// <summary>
    /// Increment an integer value.
    /// </summary>
    IncrementInt64,
    
    /// <summary>
    /// Decrement an integer value.
    /// </summary>
    DecrementInt64,
    
    /// <summary>
    /// Access an item in a table.
    /// </summary>
    AccessItem,
    
    /// <summary>
    /// Remove an item from a table.
    /// </summary>
    RemoveItem,
    
    /// <summary>
    /// Update an existing item in a table.
    /// </summary>
    UpdateExistingItem,
    
    /// <summary>
    /// Add a new item to a table.
    /// </summary>
    AddNewItem,
    
    /// <summary>
    /// Update an existing item or add a new item to a table.
    /// </summary>
    UpdateExistingOrAddItem,
    
    /// <summary>
    /// Add a new table to the database.
    /// </summary>
    AddTable,
    
    /// <summary>
    /// Clear a table.
    /// </summary>
    ClearTable,
    
    /// <summary>
    /// Remove a table from the database.
    /// </summary>
    RemoveTable,
    
    /// <summary>
    /// Clear all tables in the database.
    /// </summary>
    ClearAllTables
}