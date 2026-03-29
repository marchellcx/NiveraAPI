using NiveraAPI.Commands.API;
using NiveraAPI.TokenParsing;
using NiveraAPI.TypeParsing;

namespace NiveraAPI.Commands;

/// <summary>
/// Represents the result of a command search.
/// </summary>
public struct CommandSearchResult<TSender> where TSender : class
{
    /// <summary>
    /// Whether or not the command was found.
    /// </summary>
    public bool WasFound;

    /// <summary>
    /// The original query with the command's name removed.
    /// </summary>
    public string ArgsQuery;
    
    /// <summary>
    /// The original query.
    /// </summary>
    public string SourceQuery;

    /// <summary>
    /// The tokens that were parsed from the query.
    /// </summary>
    public List<Token>? Tokens;

    /// <summary>
    /// The results of the argument parsing.
    /// </summary>
    public ParserResult? ParsedArgs;

    /// <summary>
    /// The command that was found.
    /// </summary>
    public CommandInfo<TSender>? Command;
    
    /// <summary>
    /// The target overload that was found.
    /// </summary>
    public CommandOverload<TSender>? Overload;
    
    /// <summary>
    /// The possible overloads that were found.
    /// </summary>
    public CommandOverload<TSender>[]? PossibleOverloads;

    /// <summary>
    /// Creates a new command search result.
    /// </summary>
    /// <param name="command">The command that was found.</param>
    /// <param name="overload">The target overload that was found.</param>
    /// <param name="wasFound">Whether the command was found.</param>
    /// <param name="argsQuery">The original query with the command's name removed.</param>
    /// <param name="sourceQuery">The original query.</param>
    /// <param name="tokens">The tokens that were parsed from the query.</param>
    /// <param name="parsedArgs">The results of the argument parsing.</param>
    public CommandSearchResult(CommandInfo<TSender>? command, CommandOverload<TSender>? overload, CommandOverload<TSender>[]? possible,
        bool wasFound, string argsQuery, string sourceQuery, List<Token>? tokens, ParserResult? parsedArgs)
    {
        WasFound = wasFound;
        ArgsQuery = argsQuery;
        SourceQuery = sourceQuery;
        Tokens = tokens;
        ParsedArgs = parsedArgs;
        Command = command;
        Overload = overload;
        PossibleOverloads = possible;
    }
}