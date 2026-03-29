namespace NiveraAPI.TokenParsing.Interfaces;

/// <summary>
/// Represents a token that can be converted to a specified type.
/// </summary>
public interface IConvertableToken
{
    /// <summary>
    /// Attempts to convert the current token to the specified type.
    /// </summary>
    /// <param name="type">The target type to which the token should be converted.</param>
    /// <param name="value">When this method returns, contains the converted value if the conversion succeeded, or null if it failed.</param>
    /// <returns>True if the conversion was successful; otherwise, false.</returns>
    bool TryConvert(Type type, out object? value);
}