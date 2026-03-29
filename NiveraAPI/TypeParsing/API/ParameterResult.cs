using NiveraAPI.TokenParsing;
using NiveraAPI.Utilities;

namespace NiveraAPI.TypeParsing.API;

/// <summary>
/// Represents the result of parameter parsing and evaluation in the context of a command execution.
/// </summary>
/// <remarks>
/// The <see cref="ParameterResult"/> struct encapsulates the outcomes of parsing command parameters,
/// including the tokens involved, the parameter parsers used, the extracted parameters, any exception encountered,
/// and the final computed result.
/// </remarks>
public struct ParameterResult
{
    /// <summary>
    /// Represents an indexed collection of <see cref="Token"/> objects tied to the parsing of a command's parameters.
    /// Provides access to the current token and allows for navigation through adjacent tokens within the collection.
    /// </summary>
    /// <remarks>
    /// This property is used to handle tokens parsed from command inputs, encapsulating both the token itself
    /// and its index in the overall collection. It enables structured navigation and manipulation of tokens
    /// during command execution.
    /// </remarks>
    public IndexedValue<Token> Tokens { get; }

    /// <summary>
    /// Represents an indexed collection of <see cref="ParameterDefinition"/> objects tied to the parsing and evaluation
    /// of a command's parameters.
    /// Provides access to the current parameter definition and enables navigation through adjacent elements
    /// in the collection.
    /// </summary>
    /// <remarks>
    /// This property encapsulates the parsed parameter definitions used during execution of a command. It allows for
    /// structured traversal, modification, and analysis of parameter definitions to evaluate commands accurately.
    /// </remarks>
    public IndexedValue<ParameterDefinition> Parameters { get; }

    /// <summary>
    /// Represents an exception encountered during the parsing or processing of a command's parameters.
    /// Captures errors that occur within the context of a <see cref="ParameterResult"/> evaluation, providing
    /// detailed information about the failure.
    /// </summary>
    /// <remarks>
    /// The <c>Exception</c> property may contain details about specific errors encountered during the execution of
    /// parameter parsing, token evaluation, or command processing. It is used to facilitate debugging and
    /// error handling by encapsulating the encountered issue in a centralized manner.
    /// </remarks>
    public Exception? Exception { get; }

    /// <summary>
    /// Represents the final computed outcome of a parameter parsing operation.
    /// This property holds the result derived from processing command input parameters,
    /// encapsulating the resolved value or object generated through the parsing process.
    /// </summary>
    /// <remarks>
    /// The <see cref="Result"/> property serves as the output of the parameter resolution workflow,
    /// relying on the provided tokens, parsers, and command parameters. In case of a parsing failure
    /// or an exception during the process, this property may remain null.
    /// </remarks>
    public object? Result { get; }

    /// <summary>
    /// Indicates whether the parameter parsing operation resulted in a valid outcome.
    /// </summary>
    /// <remarks>
    /// This property evaluates the state of the parameter parsing result by checking for the presence of an exception
    /// and the validity of the computed result. It returns <c>true</c> if the parsing was successful and produced a valid result
    /// without any exception, or <c>false</c> otherwise. The validity is defined as having either a non-null result without an exception
    /// or an exception with a null result.
    /// </remarks>
    public bool IsValid { get; }

    /// <summary>
    /// Indicates whether the parameter parsing operation was successful.
    /// </summary>
    public bool IsSuccess => IsValid && Exception == null && Result != null;
    
    /// <summary>
    /// Creates a new instance of <see cref="ParameterResult"/>.
    /// </summary>
    /// <param name="tokens">An indexed collection of tokens representing the parsed input.</param>
    /// <param name="parameters">An indexed collection of command parameters and their parsed values.</param>
    /// <param name="exception">An optional exception encountered during parameter parsing.</param>
    /// <param name="result">The final computed outcome of the parameter parsing operation.</param>
    public ParameterResult(IndexedValue<Token> tokens, IndexedValue<ParameterDefinition> parameters, 
        Exception? exception, object? result)
    {
        Tokens = tokens;
        Parameters = parameters;
        Exception = exception;
        Result = result;

        IsValid = (exception != null && result == null) || (exception == null && result != null);
    }
}