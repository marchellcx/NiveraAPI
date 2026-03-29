namespace NiveraAPI.TokenParsing.Interfaces;

/// <summary>
/// Represents a token that has a name.
/// </summary>
public interface INamedToken
{
    /// <summary>
    /// The name of the token.
    /// </summary>
    string Name { get; }
}