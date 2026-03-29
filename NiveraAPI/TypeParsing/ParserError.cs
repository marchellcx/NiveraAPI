namespace NiveraAPI.TypeParsing;

/// <summary>
/// Represents the possible errors that can occur during parsing.
/// </summary>
public enum ParserError
{
    /// <summary>
    /// No tokens were provided for parsing.
    /// </summary>
    NoTokens,
    
    /// <summary>
    /// Not enough tokens were provided for parsing.
    /// </summary>
    InsufficientTokens,
    
    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    Other
}