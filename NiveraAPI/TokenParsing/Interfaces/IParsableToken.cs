using NiveraAPI.TypeParsing;
using NiveraAPI.TypeParsing.API;

namespace NiveraAPI.TokenParsing.Interfaces;

/// <summary>
/// Represents a token that can parse itself.
/// </summary>
public interface IParsableToken
{
    /// <summary>
    /// Attempts to parse the token using the provided context.
    /// </summary>
    /// <param name="context">The parameter context containing information about the parameter being parsed.</param>
    /// <returns>A <see cref="ParserResult"/> containing the parsing result and any exceptions encountered.</returns>
    ParameterResult ParseToken(ParameterContext context);
}