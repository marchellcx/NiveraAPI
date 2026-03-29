using System.Collections.Concurrent;
using NiveraAPI.Logs;
using NiveraAPI.Pooling;
using NiveraAPI.TokenParsing;
using NiveraAPI.TokenParsing.Interfaces;
using NiveraAPI.TokenParsing.Tokens;
using NiveraAPI.TypeParsing.API;
using NiveraAPI.Extensions;

namespace NiveraAPI.TypeParsing;

/// <summary>
/// Represents an abstract base class for parsing parameters of a specified type.
/// Implementations of this class provide custom logic for determining type support
/// and parsing parameter contexts.
/// </summary>
public abstract class ParameterParser
{
    private static volatile LogSink log = LogManager.GetSource("Parsing", "Parameters");
    
    /// <summary>
    /// Gets a collection of all registered <see cref="ParameterParser"/> instances.
    /// </summary>
    public static List<ParameterParser> AllParsers = new();
    
    /// <summary>
    /// Gets a collection of all cached <see cref="ParameterParser"/> instances.
    /// </summary>
    public static Dictionary<Type, ParameterParser> CachedParsers = new();

    /// <summary>
    /// Registers the specified <see cref="ParameterParser"/> into the list of available parsers.
    /// Only unique parsers not already registered will be added.
    /// </summary>
    /// <param name="parser">The <see cref="ParameterParser"/> instance to register.</param>
    /// <returns>
    /// true if the parser was successfully registered; otherwise, false if the parser was already registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="parser"/> is null.</exception>
    public static bool RegisterParser(ParameterParser parser)
    {
        if (parser == null)
            throw new ArgumentNullException(nameof(parser));

        if (AllParsers.Contains(parser))
            return false;
        
        AllParsers.Add(parser);
        
        log.Info($"Registered global parser &1{parser.GetType().FullName}&r");
        return true;
    }

    /// <summary>
    /// Unregisters the specified <see cref="ParameterParser"/> from the list of available parsers and
    /// removes any associated cached entries.
    /// </summary>
    /// <param name="parser">The <see cref="ParameterParser"/> instance to unregister.</param>
    /// <returns>
    /// true if the parser was successfully unregistered; otherwise, false if the parser was not found in the registered list.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="parser"/> is null.</exception>
    public static bool UnregisterParser(ParameterParser parser)
    {
        if (parser == null)
            throw new ArgumentNullException(nameof(parser));

        if (!AllParsers.Remove(parser))
            return false;
        
        log.Info($"Unregistered global parser &1{parser.GetType().FullName}&r");

        foreach (var pair in CachedParsers)
        {
            if (pair.Value == parser)
            {
                CachedParsers.Remove(pair.Key);
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Attempts to retrieve a registered <see cref="ParameterParser"/> that supports the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> for which to retrieve a matching parser.</param>
    /// <param name="parser">
    /// When this method returns, contains the <see cref="ParameterParser"/> that supports the specified type,
    /// if a match was found; otherwise, null.
    /// </param>
    /// <returns>
    /// true if a matching parser was found for the specified <paramref name="type"/>; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is null.</exception>
    public static bool TryGetParser(Type type, out ParameterParser? parser)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (CachedParsers.TryGetValue(type, out parser))
            return true;

        foreach (var registeredParser in AllParsers)
        {
            if (registeredParser.SupportsType(type))
            {
                parser = registeredParser;
                
                CachedParsers.Add(type, registeredParser);
                return true;
            }
        }

        parser = null;
        return false;
    }

    /// <summary>
    /// Attempts to parse the provided input string into the specified type <typeparamref name="T"/>.
    /// The method also captures any exception that occurs during the parsing process.
    /// </summary>
    /// <typeparam name="T">The target type to parse the input into.</typeparam>
    /// <param name="input">The input string to be parsed.</param>
    /// <param name="value">
    /// When this method returns, contains the parsed value of type <typeparamref name="T"/> if parsing was successful;
    /// otherwise, its default value if parsing failed.
    /// </param>
    /// <param name="exception">
    /// When this method returns, contains the exception that occurred during parsing if parsing failed;
    /// otherwise, null if parsing was successful.
    /// </param>
    /// <returns>
    /// true if the input string was successfully parsed into the specified type <typeparamref name="T"/>; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the provided <paramref name="input"/> is null or an empty string.
    /// </exception>
    public static bool TryParseString<T>(string input, out T? value, out Exception? exception)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Cannot parse an empty string.", nameof(input));

        value = default;
        exception = null;

        var parameters = ListPool<ParameterDefinition>.Shared.Rent();
        
        parameters.Add(new(typeof(T), 0));

        try
        {
            var result = ParseString(input, parameters);

            if (result.Results.Count < 1)
            {
                exception = new Exception("Unexpected number of results.");
                return false;
            }

            var parameter = result.Results[0];

            if (parameter.Exception != null)
            {
                exception = parameter.Exception;
                return false;
            }

            if (parameter.Result is not T cast)
            {
                exception = new InvalidCastException($"Cannot cast result ({parameter.Result?.GetType().FullName ?? "(null)"}) " +
                                                    $"to type {typeof(T)}");
                return false;
            }

            value = cast;
            return true;
        }
        finally
        {
            parameters.ReturnToPool();
        }
    }

    /// <summary>
    /// Parses the specified string input into a value of the desired type <typeparamref name="T"/>.
    /// The method uses predefined parsers to transform the input string into the target type.
    /// </summary>
    /// <param name="input">The string input to parse into the target type.</param>
    /// <typeparam name="T">The target type to which the input string should be parsed.</typeparam>
    /// <returns>The parsed value of type <typeparamref name="T"/> if the parsing is successful.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="input"/> is null or an empty string.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown if there is an error during the parsing process specific to the input or registered parsers.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// Thrown if the parsed value cannot be cast to the specified type <typeparamref name="T"/>.
    /// </exception>
    public static T ParseStringToValue<T>(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Cannot parse an empty string.", nameof(input));

        var parameters = ListPool<ParameterDefinition>.Shared.Rent();

        parameters.Add(new(typeof(T), 0));

        try
        {
            var result = ParseString(input, parameters);
            
            if (result.Results.Count < 1)
                throw new Exception("Unexpected number of results.");
            
            var parameter = result.Results[0];

            if (parameter.Exception != null)
                throw parameter.Exception;

            if (parameter.Result is not T cast)
                throw new InvalidCastException($"Cannot cast result ({parameter.Result?.GetType().FullName ?? "(null)"}) " +
                                               $"to type {typeof(T)}");

            return cast;
        }
        finally
        {
            parameters.ReturnToPool();
        }
    }
    
    /// <summary>
    /// Attempts to parse the specified input string into parameters based on the provided generic type <typeparamref name="T"/>.
    /// Internally, it derives the parameter definitions for <typeparamref name="T"/> and performs parsing.
    /// </summary>
    /// <typeparam name="T">The type for which the input string is parsed.</typeparam>
    /// <param name="input">The input string to parse. This cannot be null or empty.</param>
    /// <returns>
    /// A <see cref="ParserResult"/> containing the outcome of the parsing process, including the tokens
    /// and resolved parameter values.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="input"/> is null or empty.</exception>
    public static ParserResult ParseString<T>(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Cannot parse an empty string.", nameof(input));

        var parameters = ListPool<ParameterDefinition>.Shared.Rent();
        
        parameters.Add(new(typeof(T), 0));

        try
        {
            return ParseString(input, parameters);
        }
        finally
        {
            parameters.ReturnToPool();
        }
    }

    /// <summary>
    /// Parses the given input string into a structured representation based on the provided parameters
    /// and token parsers, returning the parsing result.
    /// </summary>
    /// <param name="input">The string input to parse. Must not be null or empty.</param>
    /// <param name="parameters">A list of <see cref="ParameterDefinition"/> objects that define the expected parameters for parsing. Must not be null.</param>
    /// <param name="tokenParsers">
    /// An optional collection of <see cref="TokenParser"/> instances to use when parsing the input into tokens.
    /// If not provided, default parsers will be used.
    /// </param>
    /// <returns>
    /// A <see cref="ParserResult"/> containing the outcomes of the parsing operation.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="input"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="parameters"/> argument is null.</exception>
    public static ParserResult ParseString(string input, List<ParameterDefinition> parameters,
        List<TokenParser>? tokenParsers = null)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Cannot parse an empty string.", nameof(input));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var tokens = ListPool<Token>.Shared.Rent();

        try
        {
            TokenParser.Parse(input, tokens, tokenParsers);
            return ParseTokens(tokens, parameters);
        }
        finally
        {
            tokens.ReturnToPool();
        }
    }

    /// <summary>
    /// Parses a list of <see cref="Token"/> objects into corresponding parameter results based on the provided
    /// <see cref="ParameterDefinition"/> configurations.
    /// </summary>
    /// <param name="tokens">The list of <see cref="Token"/> objects to parse. Cannot be null or empty.</param>
    /// <param name="parameters">
    /// The list of <see cref="ParameterDefinition"/> objects defining the expected parameter configurations,
    /// including their parsers and optionality. Cannot be null.
    /// </param>
    /// <returns>
    /// A <see cref="ParserResult"/> containing the parsed tokens, matching results, and the parameter definitions.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="tokens"/> or <paramref name="parameters"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="tokens"/> is empty or if the number of tokens is insufficient for the required parameters.
    /// </exception>
    public static ParserResult ParseTokens(List<Token> tokens, List<ParameterDefinition> parameters)
    {
        if (tokens == null)
            throw new ArgumentNullException(nameof(tokens));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
        
        log.Debug($"Parsing {tokens.Count} tokens into {parameters.Count} parameters.");
        
        for (var i = 0; i < tokens.Count; i++)
            log.Debug($"Token &3{i}&r: {tokens[i]?.GetType().Name ?? "null"} :: {tokens[i]?.ToString() ?? "null"}");
        
        for (var i = 0; i < parameters.Count; i++)
            log.Debug($"Parameter &3{i}&r: {parameters[i].Index} {parameters[i].Type?.Name ?? "null"}");

        if (tokens.Count == 0)
        {
            log.Debug("No tokens to parse.");
            return new(ParserError.NoTokens, tokens, null, parameters);
        }

        if (parameters.Count(x => x.IsOptional) > tokens.Count)
        {
            log.Debug($"Insufficient tokens for {parameters.Count} parameters with {tokens.Count} available.");
            return new(ParserError.InsufficientTokens, tokens, null, parameters);
        }

        var tokenIndex = tokens.GetIndexedValue();
        var parameterIndex = parameters.GetIndexedValue();
        
        var results = ListPool<ParameterResult>.Shared.Rent();
        var resultsIndex = results.GetIndexedValue();

        try
        {
            var context = new ParameterContext();

            context.Tokens = tokenIndex;
            context.Results = resultsIndex;
            context.Parameters = parameterIndex;

            while (!tokenIndex.IsOutOfRange)
            {
                ParameterResult? result = default;
                
                log.Debug($"Parsing token at index {tokenIndex.CurrentIndex}.");
                log.Debug($"Current parameter: {parameterIndex.CurrentIndex} {parameterIndex.Current.Type?.Name ?? "null"}");

                try
                {
                    if (parameterIndex.Current.MainParser == null
                        && (parameterIndex.Current.OtherParsers == null ||
                            parameterIndex.Current.OtherParsers.Count == 0))
                    {
                        log.Debug("Parameter has no parsers available");
                        
                        if (tokenIndex.Current is IParsableToken parsableToken)
                        {
                            log.Debug("Token is IParsableToken");
                            
                            result = parsableToken.ParseToken(context);
                        }
                        else if (parameterIndex.Current.Type == typeof(string))
                        {
                            log.Debug("Parameter type is string");
                            
                            if (tokenIndex.Current is StringToken stringToken)
                            {
                                log.Debug("Token is StringToken");
                                
                                result = context.CreateOkResult(stringToken.Value);
                            }
                            else if (tokenIndex.Current is IStringToken stringConvertableToken)
                            {
                                log.Debug("Token is IStringToken");
                                
                                result = context.CreateOkResult(stringConvertableToken.ConvertToString());
                            }
                        }
                        else if (tokenIndex.Current is IConvertableToken convertableToken
                                 && convertableToken.TryConvert(parameterIndex.Current.Type, out var convertedValue))
                        {
                            log.Debug("Token is IConvertableToken");
                            
                            result = context.CreateOkResult(convertedValue);
                        }
                        else
                        {
                            log.Debug("No parser available for token");
                            
                            result = context.CreateResult($"Missing parser at index {parameterIndex.CurrentIndex}.");
                        }
                    }
                    else
                    {
                        if (parameterIndex.Current.MainParser != null)
                        {
                            log.Debug("Parameter has a main parser");
                            
                            result = parameterIndex.Current.MainParser.ParseContext(context);
                        }

                        if (result is not { Exception: null, IsValid: true }
                            && parameterIndex.Current.OtherParsers != null
                            && parameterIndex.Current.OtherParsers.Count > 0)
                        {
                            log.Debug("Trying other parsers for parameter");
                            
                            ParameterResult? successParser = null;

                            foreach (var parser in parameterIndex.Current.OtherParsers)
                            {
                                var parserResult = parser.ParseContext(context);

                                if (parserResult is { IsValid: true, Exception: not null })
                                {
                                    successParser = parserResult;
                                    break;
                                }
                            }

                            if (successParser is not null)
                            {
                                result = successParser;
                            }
                            else
                            {
                                if (parameterIndex.Current.Type == typeof(string))
                                {
                                    if (tokenIndex.Current is StringToken stringToken)
                                    {
                                        result = context.CreateOkResult(stringToken.Value);
                                    }
                                    else if (tokenIndex.Current is IStringToken stringConvertableToken)
                                    {
                                        result = context.CreateOkResult(stringConvertableToken.ConvertToString());
                                    }
                                }
                                else if (tokenIndex.Current is IConvertableToken convertableToken
                                         && convertableToken.TryConvert(parameterIndex.Current.Type, out var convertedValue))
                                {
                                    result = context.CreateOkResult(convertedValue);
                                }
                                else
                                {
                                    result = context.CreateResult(
                                        $"Could not convert token to type {parameterIndex.Current.Type.FullName}.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error parsing token at index {tokenIndex.CurrentIndex}:\n{ex}");
                    
                    result = context.CreateResult(ex);
                }
                
                result ??= context.CreateResult($"Unexpected error parsing parameter at index {parameterIndex.CurrentIndex}.");

                if (result.Value.IsValid
                    && result.Value.Exception == null
                    && result.Value.Result != null
                    && tokenIndex.Current is INamedToken namedToken)
                    result = context.CreateOkResult(new NamedParameter(namedToken.Name, result.Value.Result));
                
                context.Tokens = tokenIndex = tokenIndex.MoveNext();
                context.Parameters = parameterIndex = parameterIndex.MoveNext();
                context.Results = resultsIndex = resultsIndex.AddAndMoveNext(result.Value);
                
                log.Debug($"Parsed token: {result.Value is { IsValid: true, Exception: null, Result: not null }}");
            }

            log.Debug("Filling in missing parameters with default values.");
            
            while (!parameterIndex.IsOutOfRange)
            {
                log.Debug($"Filling in missing parameter at index {parameterIndex.CurrentIndex}.");
                
                if (!parameterIndex.Current.IsOptional)
                {
                    log.Debug("Parameter is not optional");
                    
                    context.Results = resultsIndex = resultsIndex.AddAndMoveNext(context.CreateResult(
                        $"Missing token for parameter at {parameterIndex.CurrentIndex}."));
                }
                else
                {
                    log.Debug("Parameter is optional, using default value");
                    
                    context.Results = resultsIndex = resultsIndex.AddAndMoveNext(context.CreateOkResult(parameterIndex.Current.DefaultValue));
                }

                context.Parameters = parameterIndex = parameterIndex.MoveNext();
                
                log.Debug($"Moved to parameter index {parameterIndex.CurrentIndex}");
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error parsing tokens:\n{ex}");

            ListPool<ParameterResult>.Shared.Return(results);
            return new(ParserError.Other, tokens, null, parameters);
        }

        log.Debug("Parsing complete.");
        return new(null, tokens, results, parameters);
    }
    
    private volatile HashSet<Type> knownSupported = new();
    private volatile HashSet<Type> knownUnsupported = new();

    /// <summary>
    /// Determines if the specified <see cref="Type"/> is supported by this parser.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check for support.</param>
    /// <returns>true if the <paramref name="type"/> is supported; otherwise, false.</returns>
    public virtual bool SupportsType(Type type)
    {
        if (type == null)
            return false;

        if (knownSupported.Contains(type))
            return true;

        if (knownUnsupported.Contains(type))
            return false;

        if (CheckSupport(type))
        {
            knownSupported.Add(type);
            return true;
        }

        knownUnsupported.Add(type);
        return false;
    }

    /// <summary>
    /// Verifies if the specified <see cref="Type"/> satisfies the conditions for support by this parser.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to be evaluated for support conditions.</param>
    /// <returns>true if the <paramref name="type"/> meets the support criteria; otherwise, false.</returns>
    public abstract bool CheckSupport(Type type);

    /// <summary>
    /// Parses the given <see cref="ParameterContext"/> and returns the evaluation result as a <see cref="ParameterResult"/>.
    /// </summary>
    /// <param name="context">The context containing parameters, tokens, parsers, and associated data.</param>
    /// <returns>A <see cref="ParameterResult"/> representing the outcome of the parsing operation.</returns>
    public abstract ParameterResult ParseContext(ParameterContext context);
}