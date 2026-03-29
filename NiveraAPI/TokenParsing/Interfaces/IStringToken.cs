namespace NiveraAPI.TokenParsing.Interfaces;

/// <summary>
/// Represents a token that can be converted to a string.
/// </summary>
public interface IStringToken
{
    /// <summary>
    /// Converts the token to a string.
    /// </summary>
    /// <returns>The string representation of the token.</returns>
    string ConvertToString();
}