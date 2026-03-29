using NiveraAPI.TokenParsing;
using NiveraAPI.TokenParsing.Tokens;
using NiveraAPI.Utilities;

namespace NiveraAPI.TypeParsing.API;

/// <summary>
/// Represents a context for managing and processing command parameters within a parsing system.
/// This class provides access to information about the raw argument line, spaced arguments,
/// quoted arguments, as well as associated tokens, parsers, and parameters.
/// </summary>
public class ParameterContext
{
    /// <summary>
    /// Gets the tokens parsed from the command line.
    /// </summary>
    public IndexedValue<Token> Tokens { get; internal set; }
    
    /// <summary>
    /// Gets the results of the parameter parsers.
    /// </summary>
    public IndexedValue<ParameterResult> Results { get; internal set; }
    
    /// <summary>
    /// Gets the parameters of the command overload.
    /// </summary>
    public IndexedValue<ParameterDefinition> Parameters { get; internal set; }

    /// <summary>
    /// Gets the next token in the sequence, if available.
    /// </summary>
    /// <remarks>
    /// The property retrieves the next token from the underlying indexed collection of tokens.
    /// If there are no additional tokens after the current one, the value is null.
    /// </remarks>
    public Token? NextToken => Tokens.Next;

    /// <summary>
    /// Gets the current token in the parsing sequence.
    /// </summary>
    public Token CurrentToken => Tokens.Current;

    /// <summary>
    /// Gets the token that precedes the current token in the parsing sequence.
    /// </summary>
    public Token? PreviousToken => Tokens.Previous;

    /// <summary>
    /// Gets the next available command parameter in the parameter sequence, or null if no more parameters are available.
    /// </summary>
    public ParameterDefinition NextParameter => Parameters.Next;

    /// <summary>
    /// Gets the current command parameter being processed in the parsing context.
    /// </summary>
    public ParameterDefinition CurrentParameter => Parameters.Current;

    /// <summary>
    /// Gets the previous command parameter in the sequence, if it exists.
    /// </summary>
    public ParameterDefinition? PreviousParameter => Parameters.Previous;

    /// <summary>
    /// Gets the result of the previous parameter parsing operation, if valid.
    /// Returns null if the previous result is invalid or not available.
    /// </summary>
    public ParameterResult? PreviousResult => Results.Previous.IsValid ? Results.Previous : null;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterContext"/> class.
    /// </summary>
    public ParameterContext()
    {

    }

    /// <summary>
    /// Determines whether the current token is of the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of token to compare against.</typeparam>
    /// <returns><c>true</c> if the current token is of type <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
    public bool CurTokenIs<T>() where T : Token
        => CurrentToken is T;

    /// <summary>
    /// Determines whether the previous token in the token sequence is of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the token to check for. Must derive from <see cref="Token"/>.</typeparam>
    /// <returns>True if the previous token is of the specified type; otherwise, false.</returns>
    public bool PrevTokenIs<T>() where T : Token
        => PreviousToken is T;

    /// <summary>
    /// Determines whether the next token in the sequence is of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of token to check for.</typeparam>
    /// <returns>true if the next token is of type <typeparamref name="T"/>; otherwise, false.</returns>
    public bool NextTokenIs<T>() where T : Token
        => NextToken is T;

    /// <summary>
    /// Determines whether the current token is a <see cref="StringToken"/> and extracts its value if true.
    /// </summary>
    /// <param name="value">When this method returns, contains the value of the current token if it is a <see cref="StringToken"/>, otherwise an empty string.</param>
    /// <returns>
    /// <c>true</c> if the current token is a <see cref="StringToken"/>; otherwise, <c>false</c>.
    /// </returns>
    public bool CurTokenIsString(out string value)
    {
        value = string.Empty;

        if (CurrentToken is not StringToken stringToken)
            return false;
        
        value = stringToken.Value;
        return true;
    }

    /// <summary>
    /// Checks if the next token is a string token and retrieves its value if it is.
    /// </summary>
    /// <param name="value">The output parameter to store the value of the string token if the next token is a string.</param>
    /// <returns>True if the next token is a string token, otherwise false.</returns>
    public bool NextTokenIsString(out string value)
    {
        value = string.Empty;

        if (NextToken is not StringToken stringToken)
            return false;
        
        value = stringToken.Value;
        return true;
    }

    /// <summary>
    /// Determines if the previous token is a string token and retrieves its value if so.
    /// </summary>
    /// <param name="value">The string value of the previous token, if it is a string token. Empty if not.</param>
    /// <returns>True if the previous token is a string token; otherwise, false.</returns>
    public bool PrevTokenIsString(out string value)
    {
        value = string.Empty;

        if (PreviousToken is not StringToken stringToken)
            return false;
        
        value = stringToken.Value;
        return true;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ParameterResult"/> class that encapsulates
    /// the tokens, parsers, and parameters of the current parsing context, along with
    /// the provided result object.
    /// </summary>
    /// <param name="result">The result object to associate with the created parameter result.</param>
    /// <returns>A new <see cref="ParameterResult"/> containing the provided result and the current parsing context.</returns>
    public ParameterResult CreateOkResult(object result)
        => new(Tokens, Parameters, null, result);

    /// <summary>
    /// Creates a new instance of <see cref="ParameterResult"/> using the specified exception.
    /// This method associates the provided exception with the parsing context, indicating
    /// an error occurred during parameter processing.
    /// </summary>
    /// <param name="exception">The exception representing the error encountered during parameter parsing.</param>
    /// <returns>A <see cref="ParameterResult"/> instance reflecting the parsing state with the specified exception.</returns>
    public ParameterResult CreateResult(Exception exception)
        => new(Tokens, Parameters, exception, null);

    /// <summary>
    /// Creates a <see cref="ParameterResult"/> object containing an error message.
    /// This method is used to build a result representing an error condition
    /// encountered during the processing of a parameter.
    /// </summary>
    /// <param name="errorMessage">The error message describing the issue that occurred.</param>
    /// <returns>A <see cref="ParameterResult"/> object encapsulating the error details.</returns>
    public ParameterResult CreateResult(string errorMessage)
        => new(Tokens, Parameters, new ParameterException(errorMessage), null);
}