using System.Reflection;

using NiveraAPI.Pooling;
using NiveraAPI.Extensions;
using NiveraAPI.TypeParsing;

namespace NiveraAPI.Commands.API;

/// <summary>
/// Represents a specific overload of a command, including its associated metadata and execution details.
/// </summary>
/// <typeparam name="TSender">
/// The type of the sender or context that is associated with this command overload.
/// </typeparam>
public class CommandOverload<TSender> where TSender : class
{
    /// <summary>
    /// Gets the index of the context parameter.
    /// </summary>
    public int ContextIndex { get; }
    
    /// <summary>
    /// Gets the number of parameters that are not context parameters.
    /// </summary>
    public int NoContextParameterCount { get; }
    
    /// <summary>
    /// Gets the name of the command's overload.
    /// </summary>
    public string[] Name { get; }

    /// <summary>
    /// Gets the permissions required to use this overload.
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Gets the full name of the command's overload.
    /// </summary>
    public string FullName { get; }
    
    /// <summary>
    /// Gets the full name of the command's overload (including the parent command name).
    /// </summary>
    public string FullCmdName { get; }
    
    /// <summary>
    /// Gets the description of the command's overload.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Whether or not this overload requires all provided permissions.
    /// </summary>
    public bool RequiresAllPermissions { get; }
    
    /// <summary>
    /// Gets the method of the command's overload.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the parameters of the command's overload.
    /// </summary>
    public ParameterInfo[] MethodParameters { get; }
    
    /// <summary>
    /// Gets the parent command.
    /// </summary>
    public CommandInfo<TSender> Command { get; internal set; }

    /// <summary>
    /// Gets the parameters of the command's overload.
    /// </summary>
    public List<CommandParameter> Parameters { get; } = new();

    /// <summary>
    /// Gets the parameters of the command's overload.
    /// </summary>
    public List<ParameterDefinition> ParserParameters { get; } = new();

    /// <summary>
    /// Gets the collection of additional command flags.
    /// </summary>
    public List<object> Flags { get; } = new();
    
    /// <summary>
    /// Gets the pool of arguments used to store command parameters.
    /// </summary>
    public FixedArrayPool<object> ArgsPool { get; }

    /// <summary>
    /// Gets the awaiter associated with this overload.
    /// </summary>
    public CommandManager<TSender>.AwaiterConstructor Awaiter { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="CommandOverload{TSender}"/>.
    /// </summary>
    /// <param name="command">The command information associated with this overload.</param>
    /// <param name="method">The method representing the command's execution logic.</param>
    /// <param name="name">The names associated with this command overload.</param>
    /// <param name="description">A description of the command's purpose and usage.</param>
    /// <param name="perms">The required permissions for executing this command.</param>
    /// <param name="requireAll">Indicates whether all permissions must be met for execution.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the description is empty or null.</exception>
    public CommandOverload(CommandInfo<TSender> command, MethodInfo method, string[] name, string description, string[]? perms,
        bool requireAll)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (name.Length < 1)
            throw new ArgumentException("At least one name must be provided.", nameof(name));
        
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        
        Permissions = perms ?? Array.Empty<string>();
        
        Name = name;
        RequiresAllPermissions = requireAll;

        FullName = string.Join(" ", name);
        FullCmdName = string.Concat(command.FullName, " ", this.FullName).TrimStart(' ');
        
        Method = method ?? throw new ArgumentNullException(nameof(method));
        MethodParameters = method.GetAllParameters();

        ContextIndex = MethodParameters.FindIndex(p => p.ParameterType == command.Manager.ContextByRefType);
        NoContextParameterCount = MethodParameters.Count(x => x.ParameterType != command.Manager.ContextByRefType);
        
        ArgsPool = new(MethodParameters.Length, 10);
    }

    internal void ProcessParsers()
    {
        foreach (var parameter in Parameters)
        {
            var mainParser = parameter.Parsers
                .FirstOrDefault(x => x.Value == null)
                .Key;
            
            var otherParsers = parameter.Parsers
                .Where(x => x.Value != null)
                .Select(x => x.Key)
                .ToList();
            
            var definition = new ParameterDefinition(parameter.Type, parameter.PositionIndex, mainParser, otherParsers, parameter.IsOptional, 
                parameter.DefaultValue);
            
            ParserParameters.Add(definition);
        }
    }
}