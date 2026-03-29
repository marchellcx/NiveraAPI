using NiveraAPI.TokenParsing;
using NiveraAPI.TypeParsing.API;

namespace NiveraAPI.TypeParsing;

/// <summary>
/// Represents the result of a token parsing operation, providing access to parsed tokens,
/// parameter parsing results, and parameter definitions.
/// </summary>
public struct ParserResult
{
    /// <summary>
    /// Gets the list of <see cref="Token"/> objects parsed during the operation.
    /// </summary>
    public List<Token> Tokens { get; }

    /// <summary>
    /// Gets the collection of <see cref="ParameterResult"/> objects produced during the type parsing process.
    /// </summary>
    public List<ParameterResult>? Results { get; }

    /// <summary>
    /// Gets the collection of <see cref="ParameterDefinition"/> objects that define the parameters
    /// to be parsed during the type parsing operation.
    /// </summary>
    public List<ParameterDefinition> Parameters { get; }
    
    /// <summary>
    /// Gets the error encountered during the parsing operation, if any.
    /// </summary>
    public ParserError? Error { get; }
    
    /// <summary>
    /// Gets the number of parser results.
    /// </summary>
    public int ResultsCount => Results.Count;
    
    /// <summary>
    /// Gets the number of parameters.
    /// </summary>
    public int ParametersCount => Parameters.Count;
    
    /// <summary>
    /// Gets the number of required parameters.
    /// </summary>
    public int RequiredParametersCount => Parameters.Count(p => !p.IsOptional);
    
    /// <summary>
    /// Gets the number of optional parameters.
    /// </summary>
    public int OptionalParametersCount => Parameters.Count(p => p.IsOptional);

    /// <summary>
    /// Gets the number of successfully parsed parameters.
    /// </summary>
    public int ParsedParametersCount => Results?.Count(x => x.IsSuccess) ?? 0;
    
    /// <summary>
    /// Gets the number of parameters that failed to parse.
    /// </summary>
    public int FailedParametersCount => Results?.Count(x => !x.IsSuccess) ?? 0;
    
    /// <summary>
    /// Gets the percentage of successfully parsed parameters.
    /// </summary>
    public int SuccessPercentage => ParsedParametersCount * 100 / ParametersCount;
    
    /// <summary>
    /// Gets the percentage of parameters that failed to parse.
    /// </summary>
    public int FailurePercentage => FailedParametersCount * 100 / ParametersCount;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ParserResult"/> struct.
    /// </summary>
    /// <param name="tokens">The list of tokens parsed during the operation.</param>
    /// <param name="results">The collection of parameter results produced during the parsing process.</param>
    /// <param name="parameters">The collection of parameter definitions for the parsing operation.</param>
    /// <param name="error">The error encountered during the parsing operation, if any.</param>
    /// <exception cref="ArgumentNullException">Thrown if any of the provided parameters are null.</exception>
    public ParserResult(ParserError? error, List<Token> tokens, List<ParameterResult>? results, List<ParameterDefinition> parameters)
    {
        Error = error;
        Results = results;
        
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }
}