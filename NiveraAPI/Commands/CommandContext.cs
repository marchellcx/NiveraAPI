using NiveraAPI.Commands.API;
using NiveraAPI.Commands.Interfaces;
using NiveraAPI.Commands.Results;
using NiveraAPI.TokenParsing;
using NiveraAPI.TypeParsing.API;
using NiveraAPI.Extensions;

namespace NiveraAPI.Commands;

/// <summary>
/// Represents the context for a command execution, encapsulating information about
/// the command, arguments, sender, and the related command manager, among others.
/// </summary>
/// <typeparam name="TSender">
/// The type of the sender executing the command. Must be a reference type.
/// </typeparam>
public struct CommandContext<TSender> where TSender : class
{
    /// <summary>
    /// Gets the full command line received by the command manager.
    /// </summary>
    public string FullLine { get; }

    /// <summary>
    /// Gets the command line without the command's name.
    /// </summary>
    public string ArgsLine { get; }
    
    /// <summary>
    /// Gets the command line (without the command's name) split by a space.
    /// </summary>
    public string[] SpaceParsedArgs { get; }

    /// <summary>
    /// Gets the command line (without the command's name) split by a space while ignoring spaces in quotation marks.
    /// </summary>
    public string[] QuoteParsedArgs { get; }
    
    /// <summary>
    /// Gets the parsed tokens from the command line.
    /// </summary>
    public List<Token> Tokens { get; }
    
    /// <summary>
    /// Gets the results of the token parsers.
    /// </summary>
    public List<ParameterResult> ParserResults { get; }
    
    /// <summary>
    /// Gets the sender of the command.
    /// </summary>
    public TSender Sender { get; }
    
    /// <summary>
    /// Gets the command that was executed.
    /// </summary>
    public CommandInfo<TSender> Command { get; }
    
    /// <summary>
    /// Gets the command manager that executed the command.
    /// </summary>
    public CommandManager<TSender> Manager { get; }
    
    /// <summary>
    /// Gets the overload that was executed.
    /// </summary>
    public CommandOverload<TSender> Overload { get; }
    
    /// <summary>
    /// Gets the active awaiter for the command.
    /// </summary>
    public IAwaiter<TSender>? Awaiter { get; internal set; }

    /// <summary>
    /// Gets or sets the result of the command execution.
    /// Must be an instance of a type that implements the <see cref="IResult"/> interface.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when attempting to set the property to null.
    /// </exception>
    public IResult? Result { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="CommandContext{TSender}"/> class.
    /// </summary>
    /// <param name="fullCmdLine">The full command line.</param>
    /// <param name="argCmdLine">The command line without the command's name.</param>
    /// <param name="parsedTokens">The tokens parsed from the command line.</param>
    /// <param name="parameterResults">The parsed results of the command parameters.</param>
    /// <param name="sender">The sender of the command.</param>
    /// <param name="command">The command that was executed.</param>
    /// <param name="manager">The command manager that executed the command.</param>
    /// <param name="overload">The overload that was executed.</param>
    /// <exception cref="ArgumentNullException">Thrown if any of the required parameters are null.</exception>
    public CommandContext(string fullCmdLine, string argCmdLine, List<Token> parsedTokens,
        List<ParameterResult> parameterResults, TSender sender, CommandInfo<TSender> command,
        CommandManager<TSender> manager, CommandOverload<TSender> overload)
    {
        FullLine = fullCmdLine ?? throw new ArgumentNullException(nameof(fullCmdLine));
        ArgsLine = argCmdLine ?? throw new ArgumentNullException(nameof(argCmdLine));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        Overload = overload ?? throw new ArgumentNullException(nameof(overload));
        Tokens = parsedTokens;
        ParserResults = parameterResults;
        Sender = sender;
        
        SpaceParsedArgs = argCmdLine.Split(' ');
        QuoteParsedArgs = argCmdLine.SplitOutsideQuotes('"', ' ');
    }
    
    /// <summary>
    /// Sets the result to a <see cref="TextResult"/> with the specified success status and message.
    /// </summary>
    /// <param name="success">Whether the command execution was successful.</param>
    /// <param name="message">The message to be displayed to the user.</param>
    public void SetText(bool success, string message)
        => Result = new TextResult(success, message);
    
    /// <summary>
    /// Sets the result to a successful <see cref="TextResult"/> with the specified message.
    /// </summary>
    /// <param name="message">The message to be displayed to the user.</param>
    public void SetOkText(string message)
        => SetText(true, message);
    
    /// <summary>
    /// Sets the result to a failed <see cref="TextResult"/> with the specified message.
    /// </summary>
    /// <param name="message">The error message to be displayed to the user.</param>
    public void SetFailText(string message)
        => SetText(false, message);
}