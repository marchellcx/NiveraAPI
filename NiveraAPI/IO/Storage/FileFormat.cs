namespace NiveraAPI.IO.Storage;

/// <summary>
/// Represents the format of a file.
/// </summary>
public enum FileFormat
{
    /// <summary>
    /// JSON with indentation.
    /// </summary>
    JsonIndented,
    
    /// <summary>
    /// JSON without indentation.
    /// </summary>
    JsonNotIndented,
    
    /// <summary>
    /// Binary.
    /// </summary>
    Binary
}