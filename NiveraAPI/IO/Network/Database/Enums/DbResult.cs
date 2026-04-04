namespace NiveraAPI.IO.Network.Database.Enums;

/// <summary>
/// Represents the result of a database operation.
/// </summary>
public enum DbResult : byte
{
    /// <summary>
    /// The operation was successful.
    /// </summary>
    Ok,
    
    /// <summary>
    /// The operation failed.
    /// </summary>
    Failed,
    
    /// <summary>
    /// The user is not authorized to perform this action.
    /// </summary>
    Unauthorized,
    
    /// <summary>
    /// The table targeted by the action was not found.
    /// </summary>
    TableNotFound,
    
    /// <summary>
    /// The item targeted by the action was not found.
    /// </summary>
    ItemNotFound,
    
    /// <summary>
    /// The user is not authenticated.
    /// </summary>
    NotAuthenticated,
    
    /// <summary>
    /// Item cannot be added because it already exists.
    /// </summary>
    ItemExists,
    
    /// <summary>
    /// An exception occurred during the operation.
    /// </summary>
    Exception,
    
    /// <summary>
    /// Invalid arguments were provided.
    /// </summary>
    InvalidArguments
}