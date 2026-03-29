using System.Reflection;
using NiveraAPI.Commands.API;
using NiveraAPI.Commands.Attributes;
using NiveraAPI.Commands.Awaiters;
using NiveraAPI.Commands.Enums;
using NiveraAPI.Commands.Interfaces;
using NiveraAPI.Commands.Results;
using NiveraAPI.Extensions;
using NiveraAPI.Logs;
using NiveraAPI.Pooling;
using NiveraAPI.TokenParsing;
using NiveraAPI.TypeParsing;
using NiveraAPI.TypeParsing.API;

namespace NiveraAPI.Commands;

/// <summary>
/// Manages the registration, unregistration, and execution of commands designed to interact with certain operations
/// in the context of type <typeparamref name="TSender"/>.
/// </summary>
/// <typeparam name="TSender">
/// Represents the type of the sender that initiates commands. It must be a reference type.
/// </typeparam>
public class CommandManager<TSender> where TSender : class
{
    static CommandManager()
    {
        CMD_DEBUG = LibraryLoader.cmdDebugToggle;
    }

    /// <summary>
    /// Indicates whether the command manager should output debugging information
    /// during the registration, processing, and execution of commands.
    /// </summary>
    public static bool CMD_DEBUG;
    
    /// <summary>
    /// Represents a delegate used to construct an instance of <see cref="IAwaiter{TSender}"/>
    /// in the context of a command execution.
    /// </summary>
    /// <typeparam name="TSender">
    /// The type of the sender executing the command. Must be a reference type.
    /// </typeparam>
    /// <param name="context">
    /// The <see cref="CommandContext{TSender}"/> that provides information about
    /// the command execution, including the sender, command arguments, and related metadata.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IAwaiter{TSender}"/> associated with the specified
    /// command execution context.
    /// </returns>
    public delegate IAwaiter<TSender> AwaiterConstructor(CommandContext<TSender> context);
    
    /// <summary>
    /// Represents a delegate used to construct an instance of a type in the context of a command execution.
    /// </summary>
    public delegate object TypeConstructor(CommandContext<TSender> context);
    
    /// <summary>
    /// Represents a delegate used to check the permissions of a command execution.
    /// </summary>
    public delegate bool PermissionsChecker(CommandContext<TSender> context);

    /// <summary>
    /// Represents a delegate method intended to process a specific sub-attribute associated with a parameter within a command.
    /// </summary>
    /// <param name="method">
    /// The <see cref="MethodInfo"/> instance representing the method where the sub-attribute is defined.
    /// </param>
    /// <param name="parameter">
    /// The parameter metadata of the command that is being processed.
    /// </param>
    /// <param name="attributes">
    /// The attributes of the command parameter being analyzed.
    /// </param>
    /// <param name="attribute">
    /// The <see cref="ParameterSubAttribute"/> instance applied to the parameter, indicating the sub-attribute to be processed.
    /// </param>
    public delegate void ProcessSubAttribute(MethodInfo method, CommandParameter parameter, 
        CommandParameter.ParameterAttributes attributes, ParameterSubAttribute attribute);
    
    private List<CommandInfo<TSender>> commands = new();
    
    private Dictionary<Type, AwaiterConstructor> awaiters = new()
    {
        { typeof(Task), ctx => new TaskAwaiter<TSender>() },
        { typeof(void), ctx => new VoidAwaiter<TSender>() },
    };

    private LogSink log = new("Commands", "Manager");

    /// <summary>
    /// Gets or sets the delegate responsible for checking permissions
    /// during the execution of a command.
    /// </summary>
    /// <remarks>
    /// The assigned delegate takes a <see cref="CommandContext{TSender}"/>
    /// as input and returns a boolean value indicating whether
    /// the execution is permitted.
    /// </remarks>
    public PermissionsChecker? CheckPermissions { get; set; }
    
    /// <summary>
    /// Gets or sets the delegate responsible for processing sub-attributes
    /// associated with a command parameter during command registration or execution.
    /// </summary>
    /// <remarks>
    /// The delegate takes a <see cref="MethodInfo"/>, a <see cref="CommandParameter"/>,
    /// associated parameter attributes, and the specific sub-attribute as input.
    /// It allows for custom handling of sub-attributes applied to command parameters.
    /// </remarks>
    public ProcessSubAttribute? SubAttributeProcessor { get; set; }
    
    /// <summary>
    /// Gets the type of the context associated with the <see cref="CommandManager{TSender}"/>,
    /// </summary>
    public Type ContextByRefType { get; } = typeof(CommandContext<TSender>).MakeByRefType();
    
    /// <summary>
    /// Gets a collection of all registered commands.
    /// </summary>
    public IReadOnlyList<CommandInfo<TSender>> Commands => commands;
    
    /// <summary>
    /// Gets a collection of all registered result awaiter constructors.
    /// </summary>
    public IReadOnlyDictionary<Type, AwaiterConstructor> Awaiters => awaiters;
    
    /// <summary>
    /// Gets called before a command is executed.
    /// </summary>
    public event Action<CommandContext<TSender>, CommandSearchResult<TSender>>? Executing;

    /// <summary>
    /// Gets called after a command's method is executed.
    /// </summary>
    /// <remarks>The command may not have completed execution at this state, use the <see cref="Awaited"/> event.</remarks>
    public event Action<CommandContext<TSender>, CommandSearchResult<TSender>, object?>? Executed;

    /// <summary>
    /// Gets called when a command's result starts to be awaited.
    /// </summary>
    public event Action<CommandContext<TSender>, CommandSearchResult<TSender>, IAwaiter<TSender>>? Awaiting;

    /// <summary>
    /// Gets called when a command's result has been awaited (this marks the command's full execution).
    /// </summary>
    public event Action<CommandContext<TSender>, CommandSearchResult<TSender>, IAwaiter<TSender>>? Awaited;
    
    /// <summary>
    /// Gets called when a command execution fails.
    /// </summary>
    public event Action<CommandContext<TSender>, CommandSearchResult<TSender>>? Failed;

    /// <summary>
    /// Unregisters an awaiter associated with the specified type from the command manager.
    /// </summary>
    /// <param name="type">The type associated with the awaiter to be unregistered.</param>
    /// <returns>
    /// A boolean value indicating whether the awaiter was successfully unregistered.
    /// Returns false if no awaiter is associated with the specified type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="type"/> is null.
    /// </exception>
    public bool UnregisterAwaiter(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return awaiters.Remove(type);
    }

    /// <summary>
    /// Registers an awaiter with the command manager, associating it with the specified type.
    /// </summary>
    /// <param name="type">The type to associate the awaiter with.</param>
    /// <param name="constructor">
    /// A delegate that constructs an instance of the awaiter when a command context is provided.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the awaiter was successfully registered.
    /// Returns false if an awaiter is already registered for the specified type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="type"/> or <paramref name="constructor"/> is null.
    /// </exception>
    public bool RegisterAwaiter(Type type, AwaiterConstructor constructor)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (constructor == null)
            throw new ArgumentNullException(nameof(constructor));
        
        if (awaiters.ContainsKey(type))
            return false;

        awaiters[type] = constructor;
        
        log.Info($"Registered awaiter for type &3{type.FullName}&r");
        return true;
    }

    /// <summary>
    /// Registers all commands found in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for commands to register.</param>
    /// <returns>
    /// A dictionary mapping each type that was registered as a command to its corresponding
    /// <see cref="CommandInfo{TSender}"/> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="assembly"/> is null.
    /// </exception>
    public Dictionary<Type, CommandInfo<TSender>> RegisterCommands(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var dict = new Dictionary<Type, CommandInfo<TSender>>();
        
        foreach (var type in assembly.GetTypes())
        {
            RegisterCommand(type, null, out var command, out _);

            if (command != null)
                dict.Add(type, command);   
        }

        return dict;
    }

    /// <summary>
    /// Registers a new command and its associated constructor in the command manager.
    /// </summary>
    /// <typeparam name="T">The data type associated with the command being registered.</typeparam>
    /// <param name="constructor">A delegate that creates an instance of the specified type using the provided command context.</param>
    /// <param name="command">When this method returns, contains the registered command information if the operation was successful; otherwise, null.</param>
    /// <param name="results">When this method returns, contains the list of results for the command registration process.</param>
    /// <returns>
    /// A <see cref="CommandRegisterError"/> value that indicates the result of the registration process. Possible values include:
    /// <see cref="CommandRegisterError.Ok"/> if the command was successfully registered,
    /// or another value if registration failed.
    /// </returns>
    public CommandRegisterError RegisterCommand<T>(TypeConstructor constructor,
        out CommandInfo<TSender>? command, out List<CommandRegisterResult<TSender>> results)
    {
        return RegisterCommand(typeof(T), constructor, out command, out results);
    }
        
    /// <summary>
    /// Attempts to register a new command using the specified type and constructor, and returns the results of the registration process.
    /// </summary>
    /// <param name="type">The type representing the command to be registered.</param>
    /// <param name="constructor">The optional type constructor used to create instances for the command. Can be null if no constructor is needed.</param>
    /// <param name="command">An output parameter containing the registered command information, or null if the registration failed.</param>
    /// <param name="results">An output parameter containing detailed results of the registration process.</param>
    /// <returns>
    /// A <see cref="CommandRegisterError"/> indicating the outcome of the registration process. Returns <c>Ok</c> if successful, otherwise returns an error code.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="type"/> is null.</exception>
    public CommandRegisterError RegisterCommand(Type type, TypeConstructor? constructor,
        out CommandInfo<TSender>? command, out List<CommandRegisterResult<TSender>> results)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        results = new();
        command = null;

        var name = Array.Empty<string>();
        var flags = Array.Empty<object>();
        var perms = Array.Empty<string>();
        var permsAll = false;
        
        if (type.HasAttribute<GroupAttribute>(out var groupAttribute))
        {
            name = groupAttribute.Name;
            flags = groupAttribute.Flags;
            perms = groupAttribute.Permissions;
            permsAll = groupAttribute.AllPermissions;
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (method == null) 
                continue;
            
            if (!method.HasAttribute<OverloadAttribute>(out var overloadAttribute)) 
                continue;
            
            if (CMD_DEBUG)
                log.Debug($"Attempting to register command &3{method.Name}&r in &1{type.FullName}&r");

            if (!method.IsStatic && constructor == null)
            {
                log.Warn($"Could not register command method &3{method.Name}&r in &1{type.FullName}&r: " +
                         $"The method is not static and no constructor was provided. Ignoring method.");
                
                results.Add(new(method, null, CommandRegisterError.NoConstructor));
                continue;
            }

            if (!awaiters.TryGetValue(method.ReturnType ?? typeof(void), out var awaiterConstructor))
            {
                log.Warn($"Could not register command method &3{method.Name}&r in &1{type.FullName}&r: " +
                         $"The command's result type does not have a registered awaiter. Ignoring method.");
                
                results.Add(new(method, null, CommandRegisterError.NoAwaiter));
                continue;
            }

            var parameters = method.GetAllParameters();

            if (parameters.Length < 1)
            {
                log.Warn($"Could not register command method &3{method.Name}&r in &1{type.FullName}&r: " +
                         $"The command's method does not have any parameters. Ignoring method.");
                
                results.Add(new(method, null, CommandRegisterError.NoParameters));
                continue;
            }
            
            if (CMD_DEBUG)
                log.Debug($"Processing parameters for command &3{method.Name}&r in &1{type.FullName}&r");

            var parametersResult = TryProcessParameters(method, parameters, out var processed);

            if (parametersResult != CommandRegisterError.Ok)
            {
                log.Warn($"Parameters for command &3{method.Name}&r in &1{type.FullName}&r failed to process");
                
                results.Add(new(method, null, parametersResult));
                continue;
            }

            if (command == null)
            {
                command = new(name, type, this);
                command.Flags.AddRange(flags);
                command.Constructor = constructor;
            }

            var ovName = name.Length > 0 ? name.ConcatArray(overloadAttribute.Name) : overloadAttribute.Name;
            var ovFlags = flags.Length > 0 ? flags.ConcatArray(overloadAttribute.Flags) : overloadAttribute.Flags;
            var ovPerms = perms.Length > 0 ? perms.ConcatArray(overloadAttribute.Permissions) : overloadAttribute.Permissions;
            var ovPermsAll = overloadAttribute.AllPermissions || (permsAll && perms.Length > 0);

            var overload = new CommandOverload<TSender>(command, method, ovName, overloadAttribute.Description, ovPerms,
                ovPermsAll) { Awaiter = awaiterConstructor };
            
            overload.Flags.AddRange(ovFlags);
            overload.Parameters.AddRange(processed);
            
            overload.ProcessParsers();
            
            command.Overloads.Add(overload);
            
            results.Add(new(method, overload, CommandRegisterError.Ok));
            
            log.Info($"Registered overload &3{method.Name}&r in command &1{type.FullName}&r");
        }

        if (command != null)
            return RegisterCommand(command);

        return CommandRegisterError.OverloadsEmpty;
    }
    
    /// <summary>
    /// Registers a new command within the command manager.
    /// </summary>
    /// <param name="command">The command information object containing details about the command to be registered.</param>
    /// <returns>
    /// A value of type <see cref="CommandRegisterError"/> that represents the result of the registration operation.
    /// Possible return values include:
    /// <list type="bullet">
    /// <item><see cref="CommandRegisterError.Ok"/> if the command was successfully registered.</item>
    /// <item><see cref="CommandRegisterError.CommandExists"/> if a command with the same name and flags already exists.</item>
    /// <item><see cref="CommandRegisterError.OverloadsEmpty"/> if the command has no associated overloads.</item>
    /// <item><see cref="CommandRegisterError.NoConstructor"/> if the command is not static and no constructor is provided.</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="command"/> is null.
    /// </exception>
    public CommandRegisterError RegisterCommand(CommandInfo<TSender> command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (!string.IsNullOrEmpty(command.FullName) 
            && commands.Any(c => !string.IsNullOrEmpty(command.FullName) &&
                c.FullName == command.FullName && c.Flags.SequenceEqual(command.Flags)))
        {
            log.Warn($"Could not register command &3{command.Type.FullName}&r: " +
                     $"A command with the same name and flags already exists. Ignoring command.");
            return CommandRegisterError.CommandExists;
        }

        if (command.Overloads.Count < 1)
        {
            log.Warn($"Could not register command &3{command.Type.FullName}&r: " +
                     $"Command has no overloads. Ignoring command.");
            return CommandRegisterError.OverloadsEmpty;
        }

        if (command is { AllStatic: false, Constructor: null })
        {
            log.Warn($"Could not register command &3{command.Type.FullName}&r: " +
                     $"Command contains non-static methods and no constructor was provided. Ignoring command.");
            return CommandRegisterError.NoConstructor;
        }

        commands.Add(command);
        
        log.Info($"Registered command &1{command.Type.FullName}&r with &3{command.Overloads.Count}&r overload(s)");
        return CommandRegisterError.Ok;
    }

    /// <summary>
    /// Registers a new command overload for the specified command.
    /// </summary>
    /// <param name="command">The command to which the overload should be added.</param>
    /// <param name="overload">The overload to add to the command.</param>
    /// <returns>
    /// A boolean value indicating whether the overload was successfully registered.
    /// Returns false if an overload with the same full name already exists for the command.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="command"/> or <paramref name="overload"/> is null.
    /// </exception>
    public bool RegisterOverload(CommandInfo<TSender> command, CommandOverload<TSender> overload)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (overload == null)
            throw new ArgumentNullException(nameof(overload));

        if (command.Overloads.Any(o => o.FullName == overload.FullName))
        {
            log.Warn($"Could not register command overload &1{overload.Method.Name}&r in command &3{command.Type.FullName}&r: " +
                     $"Command already has an overload with the same full name. Ignoring overload.");
            return false;
        }
        
        command.Overloads.Add(overload);
        return true; 
    }

    /// <summary>
    /// Registers a new overload for an existing command based on the specified parameters.
    /// </summary>
    /// <param name="command">The name of the command to which the overload will be added.</param>
    /// <param name="flags">Optional flags used to refine the search for the command, such as attributes or matching criteria.</param>
    /// <param name="overload">The overload instance to be registered with the command.</param>
    /// <returns>
    /// A boolean value indicating whether the overload was successfully registered.
    /// Returns false if the command is not found, the overload is null, or the overload already exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="overload"/> is null.
    /// </exception>
    public bool RegisterOverload(string command, object[]? flags, CommandOverload<TSender> overload)
    {
        if (string.IsNullOrEmpty(command))
            return false;

        if (overload == null)
            throw new ArgumentNullException(nameof(overload));

        if (overload.Method == null)
            throw new ArgumentNullException(nameof(overload.Method));

        if (!TryGetCommand(command, flags, out var c, out _))
        {
            log.Warn($"Could not register overload &1{overload.Method.Name}&r in command &3{command}&r: " +
                     $"No command with the specified name was found. Ignoring overload.");
            return false;
        }

        if (c.Overloads.Any(o => o.FullName == overload.FullName))
        {
            log.Warn($"Could not register overload &1{overload.Method.Name}&r in command &3{c.Type.FullName}&r: " +
                     $"Command already has an overload with the same full name. Ignoring overload.");
            return false;
        }

        if (!overload.Method.IsStatic && c.Constructor == null)
        {
            log.Warn($"Could not register overload &1{overload.Method.Name}&r in command &3{c.Type.FullName}&r: " +
                     $"The command is not static and no constructor was provided. Ignoring overload.");
            return false;
        }

        overload.Command = c;
        
        c.Overloads.Add(overload);
        
        log.Info($"Registered overload &1{overload.Method.Name}&r in command &1{c.Type.FullName}&r");
        return true;
    }

    /// <summary>
    /// Unregisters a specific overload from the provided command.
    /// </summary>
    /// <param name="command">The command from which the overload will be unregistered.</param>
    /// <param name="overload">The full name of the overload to unregister.</param>
    /// <returns>
    /// A boolean value indicating whether the overload was successfully unregistered.
    /// Returns false if the overload is not found or if the provided overload name is null or empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="command"/> is null.
    /// </exception>
    public bool UnregisterOverload(CommandInfo<TSender> command, string overload)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrEmpty(overload))
            return false;

        if (!command.Overloads.TryGetFirst(x => string.Equals(x.FullName, overload, StringComparison.OrdinalIgnoreCase),
                out var o))
        {
            log.Warn($"Could not unregister overload &1{overload}&r from command &3{command.Type.FullName}&r: " +
                     $"No overload with the specified name was found. Ignoring overload.");
            return false; 
        }
        
        command.Overloads.Remove(o);
        
        log.Info($"Unregistered overload &1{overload}&r from command &1{command.Type.FullName}&r");
        return true;  
    }

    /// <summary>
    /// Unregisters the specified command from the command manager.
    /// </summary>
    /// <param name="command">The command to be unregistered.</param>
    /// <returns>
    /// A boolean value indicating whether the command was successfully unregistered.
    /// Returns false if the command does not exist in the manager.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="command"/> is null.
    /// </exception>
    public bool UnregisterCommand(CommandInfo<TSender> command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (!commands.Contains(command))
            return false;
        
        commands.Remove(command);
        
        log.Info($"Unregistered command &1{command.Type.FullName}&r");
        return true;   
    }

    /// <summary>
    /// Unregisters all commands currently registered in the command manager.
    /// </summary>
    /// <returns>
    /// The total number of commands that were unregistered.
    /// </returns>
    public int UnregisterCommands()
    {
        var count = commands.Count;
        
        commands.Clear();
        
        log.Info($"Unregistered &1{count}&r commands");
        return count;
    }

    /// <summary>
    /// Unregisters all commands associated with the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the commands to be unregistered.</param>
    /// <returns>
    /// The number of commands successfully unregistered from the command manager.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="assembly"/> is null.
    /// </exception>
    public int UnregisterCommands(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var count = 0;
        
        foreach (var type in assembly.GetTypes())
            count += UnregisterCommand(type) ? 1 : 0;
        
        return count;   
    }
    
    /// <summary>
    /// Unregisters a command associated with the specified type from the command manager.
    /// </summary>
    /// <typeparam name="T">The type associated with the command to be unregistered.</typeparam>
    /// <returns>true if the command was successfully unregistered, false otherwise.</returns>
    public bool UnregisterCommand<T>()
    {
        return UnregisterCommand(typeof(T));
    }

    /// <summary>
    /// Unregisters a command associated with the specified type from the command manager.
    /// </summary>
    /// <param name="type">The type associated with the command to be unregistered.</param>
    /// <returns>
    /// A boolean value indicating whether the command was successfully unregistered.
    /// Returns false if no command is associated with the specified type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="type"/> is null.
    /// </exception>
    public bool UnregisterCommand(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return commands.RemoveAll(x => x.Type == type) > 0;
    }

    /// <summary>
    /// Unregisters a command identified by its name and optional flags from the command manager.
    /// </summary>
    /// <param name="name">The name of the command to be unregistered. Must be non-null and non-empty.</param>
    /// <param name="flags">Optional flags used to locate the command variant. Can be null to search without flags.</param>
    /// <returns>
    /// A boolean value indicating whether the command was successfully unregistered.
    /// Returns false if no matching command was found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="name"/> is null or empty.
    /// </exception>
    public bool UnregisterCommand(string name, object[]? flags = null)
    {
        if (!TryGetCommand(name, flags, out var command, out _))
        {
            log.Warn($"Could not unregister command &3{name}&r: No command with the specified name was found. Ignoring command.");
            return false;
        }

        commands.Remove(command);
        
        log.Info($"Unregistered command &1{command.Type.FullName}&r");
        return true;
    }

    /// <summary>
    /// Attempts to retrieve a command overload that matches the specified name and flags.
    /// </summary>
    /// <param name="name">The name of the command to search for.</param>
    /// <param name="flags">An optional array of flags to filter the commands being searched.</param>
    /// <param name="overload">When this method returns, contains the matching command overload, if found; otherwise, null.</param>
    /// <param name="cleanQuery">The remaining query string after removing the command name.</param>
    /// <returns>
    /// A boolean value indicating whether a matching command overload was found.
    /// Returns true if a matching overload is located; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="name"/> is null.
    /// </exception>
    public bool TryGetOverload(string name, object[]? flags, out CommandOverload<TSender>? overload, out string? cleanQuery)
    {
        overload = null;
        cleanQuery = null;
        
        if (string.IsNullOrEmpty(name))
            return false;
        
        if (CMD_DEBUG)
            log.Debug($"Searching for overload &3{name}&r in &1{commands.Count}&r commands");
        
        var spacedQuery = name.Split(' ');

        foreach (var command in commands)
        {
            if (CMD_DEBUG)
                log.Debug($"Searching command &1{command.Type.FullName}&r for &3{name}&r");
            
            if (flags?.Length > 0 && !flags.All(flag => command.Flags.Contains(flag)))
            {
                if (CMD_DEBUG) 
                    log.Debug($"Flags do not match for command &1{command.Type.FullName}&r");
                
                continue;
            }

            if (command.Name.Length > spacedQuery.Length)
            {
                if (CMD_DEBUG)
                    log.Debug($"Command name is longer than query string for command &1{command.Type.FullName}&r");
                
                continue;
            }
            
            var match = false;
            var offset = 0;

            if (command.Name.Length > 0)
            {
                if (CMD_DEBUG)
                    log.Debug($"Comparing command name &3{command.FullName}&r with query string &3{name}&r");
                
                for (var x = 0; x < command.Name.Length; x++)
                {
                    if (!string.Equals(command.Name[x], spacedQuery[x], StringComparison.OrdinalIgnoreCase))
                    {
                        if (CMD_DEBUG)
                            log.Debug($"Command name &3{command.Name[x]}&r does not match query string &3{spacedQuery[x]}&r");
                        
                        match = false;
                        offset = 0;

                        break;
                    }

                    match = true;
                    offset++;
                }

                if (!match)
                    continue;
            }
            
            if (CMD_DEBUG)
                log.Debug($"Matched command name &3{command.FullName}&r with query string &3{name}&r");

            var ovMatch = false;
            
            foreach (var o in command.Overloads)
            {
                if (o.Name.Length > spacedQuery.Length - offset)
                {
                    if (CMD_DEBUG)
                        log.Debug($"Overload name &1{o.FullName}&r is longer than query string &3{name}&r for command &1{command.Type.FullName}&r");
                    
                    continue;
                }
                
                for (var x = 0; x < o.Name.Length; x++)
                {
                    if (!string.Equals(o.Name[x], spacedQuery[x + offset], StringComparison.OrdinalIgnoreCase))
                    {
                        if (CMD_DEBUG)
                            log.Debug($"Overload name &1{o.FullName}&r does not match query string &3{name}&r for command &1{command.Type.FullName}&r");
                        
                        ovMatch = false;
                        break;
                    }

                    ovMatch = true;
                }

                if (ovMatch)
                {
                    overload = o;
                    cleanQuery = string.Join(" ", spacedQuery.Skip(offset + o.Name.Length));
                    
                    if (CMD_DEBUG)
                        log.Debug($"Found overload &1{o.FullName}&r in command &1{command.Type.FullName}&r, clean query: &6{cleanQuery}&r");
                    
                    return true;
                }
            }
        }

        if (CMD_DEBUG)
            log.Debug($"No matching overload found for query string &3{name}&r");
        
        return false;
    }

    /// <summary>
    /// Tries to retrieve a command by its name from the command manager's registry.
    /// </summary>
    /// <param name="name">The name of the command to retrieve. Comparison is case-insensitive.</param>
    /// <param name="flags">Optional array of flags to filter the commands being queried.</param>
    /// <param name="command">
    /// When this method returns, contains the command associated with the specified name if the command exists;
    /// otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <param name="cleanQuery">The remaining query string after removing the command name.</param>
    /// <returns>
    /// A boolean value indicating whether a command with the specified name was found.
    /// Returns false if the name is null, empty, or no matching command exists.
    /// </returns>
    public bool TryGetCommand(string name, object[]? flags, out CommandInfo<TSender>? command, out string? cleanQuery)
    {
        command = null;
        cleanQuery = null;
        
        if (string.IsNullOrEmpty(name))
            return false;
        
        if (CMD_DEBUG)
            log.Debug($"Searching for command &3{name}&r in &1{commands.Count}&r commands");

        var spacedQuery = name.Split(' ');
        
        foreach (var c in commands)
        {
            if (CMD_DEBUG)
                log.Debug($"Checking command &1{c.Type.FullName}&r for &3{name}&r");

            if (flags?.Length > 0 && !flags.All(flag => c.Flags.Contains(flag)))
            {
                if (CMD_DEBUG)
                    log.Debug($"Flags do not match for command &1{c.Type.FullName}&r");
                
                continue;
            }

            if (c.Name.Length > spacedQuery.Length)
            {
                if (CMD_DEBUG)
                    log.Debug($"Command name is longer than query string for command &1{c.Type.FullName}&r");
                
                continue;
            }
            
            var match = false;

            if (c.Name.Length > 0)
            {
                for (var x = 0; x < c.Name.Length; x++)
                {
                    if (!string.Equals(c.Name[x], spacedQuery[x], StringComparison.OrdinalIgnoreCase))
                    {
                        if (CMD_DEBUG)
                            log.Debug($"Command name &3{c.Name[x]}&r does not match query string &3{spacedQuery[x]}&r");
                        
                        match = false;
                        break;
                    }

                    match = true;
                }
            }

            if (match)
            {
                command = c;
                cleanQuery = string.Join(" ", spacedQuery.Skip(c.Name.Length));
                
                if (CMD_DEBUG)
                    log.Debug($"Found command &1{c.Type.FullName}&r, clean query: &6{cleanQuery}&r");
                return true;
            }
        }
        
        if (CMD_DEBUG)
            log.Debug($"No command found for query &3{name}&r");
        
        return false;
    }

    /// <summary>
    /// Executes a command search result, invoking the associated command and its overload, if available.
    /// </summary>
    /// <param name="result">The result of the command search, containing details about the command, overload, arguments, and source query.</param>
    /// <param name="sender">The sender initiating the command execution, or null if no sender is specified.</param>
    /// <param name="callback">An optional callback that receives the context of the command execution.</param>
    public void ExecuteSearch(ref CommandSearchResult<TSender> result, TSender? sender,
        Action<CommandContext<TSender>>? callback = null)
    {
        var context = new CommandContext<TSender>(result.SourceQuery, result.ArgsQuery,
            result.Tokens!, result.ParsedArgs?.Results ?? null!, sender!, result.Command!, this, result.Overload!);
        
        void SetErrorAndPool(IResult cmdResult, object[]? overloadArgs, ref CommandSearchResult<TSender> resCopy)
        {
            context.Result = cmdResult;
            
            if (cmdResult is TextResult textResult)
                log.Error(textResult.Text);
            else if (cmdResult is ErrorResult errorResult)
                log.Error(errorResult.ToString());

            Failed?.Invoke(context, resCopy);
            
            if (resCopy.Tokens != null)
                ListPool<Token>.Shared.Return(resCopy.Tokens);
                
            if (resCopy.ParsedArgs.HasValue && resCopy.ParsedArgs.Value.Results != null)
                ListPool<ParameterResult>.Shared.Return(resCopy.ParsedArgs.Value.Results);
            
            if (overloadArgs != null && resCopy.Overload != null)
                resCopy.Overload.ArgsPool.Return(overloadArgs);
        }
        
        if (!result.WasFound
            || result.Command == null
            || result.Overload == null)
        {
            SetErrorAndPool(new ErrorResult("No command found"), null, ref result);
            return;
        }

        if (CheckPermissions != null
            && result.Overload.Permissions.Length > 0
            && !CheckPermissions(context))
        {
            SetErrorAndPool(new MissingPermissionsResult(result.Overload.Permissions, result.Overload.RequiresAllPermissions), null, ref result);
            return;
        }
        
        if (!result.Overload.Method.IsStatic && result.Command.Constructor == null)
        {
            SetErrorAndPool(new ErrorResult("Command is not static and no constructor was provided"), null, ref result);
            return;
        }
        
        if (CMD_DEBUG)
            log.Debug($"Executing command &1{result.Command.Type.FullName}&r with overload &1{result.Overload.FullName}&r");

        var args = result.Overload.ArgsPool.Rent();

        try
        {
            if (result.Overload.NoContextParameterCount > 0)
            {
                if (result.ParsedArgs?.Results == null)
                {
                    SetErrorAndPool(new ErrorResult($"Parsed arguments are null for command '{result.Command.FullName}'"), args, ref result);
                    return;
                }

                if (result.ParsedArgs.Value.Results.Count != result.Overload.NoContextParameterCount)
                {
                    SetErrorAndPool(new ErrorResult($"Parsed arguments count does not match expected count for command '{result.Command.FullName}'"),
                        args, ref result);
                    return;
                }

                var results = result.ParsedArgs.Value.Results;

                for (var x = 0; x < results.Count; x++)
                {
                    var value = results[x];

                    if (value.Exception != null)
                    {
                        SetErrorAndPool(new ErrorResult($"Error parsing argument {x + 1} for command '{result.Command.FullName}': {value.Exception.Message}"), 
                            args, ref result);
                        return;
                    }

                    if (!value.IsValid)
                    {
                        SetErrorAndPool(new ErrorResult($"Invalid argument {x + 1} for command '{result.Command.FullName}': {value.Exception?.Message ?? "No exception message"}"),
                            args, ref result);
                        return;
                    }
                    
                    if (value.Result is NamedParameter namedParameter)
                    {
                        var index = result.Overload.MethodParameters
                            .FindIndex(p => string.Equals(namedParameter.Name, p.Name, StringComparison.OrdinalIgnoreCase));

                        if (index == -1)
                        {
                            SetErrorAndPool(new ErrorResult($"Named parameter '{namedParameter.Name}' not found for command '{result.Command.FullName}'"), args, ref result);
                            return;
                        }

                        if (args[index] != null)
                        {
                            SetErrorAndPool(new ErrorResult($"Named parameter '{namedParameter.Name}' already set for command '{result.Command.FullName}'"), args, ref result);
                            return;
                        }
                        
                        args[index] = namedParameter.Value;
                        
                        if (CMD_DEBUG)
                            log.Debug($"Assigned named parameter &1{namedParameter.Name}&r to index &1{index}&r");
                    }
                    else if (value.Parameters.Current.Index >= args.Length
                             || value.Parameters.Current.Index < 0)
                    {
                        SetErrorAndPool(new ErrorResult($"Argument {x + 1} for command '{result.Command.FullName}' is out of range"), args, ref result);
                        return;
                    }
                    else
                    {
                        args[value.Parameters.Current.Index] = value.Result;
                        
                        if (CMD_DEBUG)
                            log.Debug($"Assigned argument &1{value.Parameters.Current.Index}&r (&6{value.Parameters.Current.Type.Name}&r)");
                    }
                }
            }

            if (result.Overload.ContextIndex != -1)
            {
                args[result.Overload.ContextIndex] = context;
                
                if (CMD_DEBUG)
                    log.Debug($"Assigned context argument to index &1{result.Overload.ContextIndex}&r");
            }

            if (CMD_DEBUG)
                log.Debug($"Invoking command overload &1{result.Overload.FullName}&r");

            object? overloadResult = null;

            Executing?.Invoke(context, result);

            try
            {
                if (result.Overload.Method.IsStatic)
                {
                    overloadResult = result.Overload.Method.Invoke(null, args);
                }
                else
                {
                    overloadResult = result.Overload.Method.Invoke(result.Command.Constructor(context), args);
                }

                context = (CommandContext<TSender>)args[0];
            }
            catch (Exception ex)
            {
                SetErrorAndPool(new ErrorResult($"Error executing command overload {result.Overload.FullName}: {ex}"), args, ref result);
                return;
            }

            if (CMD_DEBUG)
                log.Debug($"Overload &1{result.Overload.FullName}&r returned &6{overloadResult?.ToString() ?? "(null)"}&r");

            Executed?.Invoke(context, result, overloadResult);

            var awaiter = result.Overload.Awaiter(context);

            if (awaiter == null)
            {
                SetErrorAndPool(new ErrorResult($"No awaiter found for command overload {result.Overload.FullName}"), args, ref result);
                return;
            }

            void AwaiterCallback(CommandSearchResult<TSender> resCopy)
            {
                callback?.Invoke(context);
                
                if (resCopy.Tokens != null)
                    ListPool<Token>.Shared.Return(resCopy.Tokens);
                
                if (resCopy.ParsedArgs is { Results: not null })
                    ListPool<ParameterResult>.Shared.Return(resCopy.ParsedArgs.Value.Results!);

                Awaited?.Invoke(context, resCopy, awaiter);

                if (awaiter is IDisposable disposable)
                    disposable.Dispose();
            }

            context.Awaiter = awaiter;

            Awaiting?.Invoke(context, result, awaiter);

            var resCopy = result;

            awaiter.AwaitResult(ref context, overloadResult, () => AwaiterCallback(resCopy));
        }
        catch (Exception ex)
        {
            SetErrorAndPool(new ErrorResult($"Error executing command overload {result.Overload.FullName}: {ex}"), args, ref result);
        }
    }

    /// <summary>
    /// Searches for a command within the command manager, matching the provided query string.
    /// </summary>
    /// <param name="query">The query string representing the command to search for.</param>
    /// <param name="tokenParsers">Optional list of token parsers to parse the command arguments.</param>
    /// <param name="flags">Optional array of flags to filter the commands being queried.</param>
    /// <returns>
    /// A <see cref="CommandSearchResult{TSender}"/> containing the search result which includes
    /// information about the matched command, its overload, parsing details, and match status.
    /// </returns>
    public CommandSearchResult<TSender> SearchCommand(string query, List<TokenParser>? tokenParsers = null,
        object[]? flags = null)
    {
        if (string.IsNullOrEmpty(query))
            return new(null, null, null, false, string.Empty,
                string.Empty, null, null);

        var spacedQuery = query.Split(' ');
        
        if (CMD_DEBUG)
            log.Debug($"Searching for command &3{query}&r");

        if (spacedQuery.Length < 1)
        {
            if (CMD_DEBUG)
                log.Debug("Query string is empty");   
            
            return new(null, null, null, false, string.Empty,
                string.Empty, null, null);
        }
        
        var possible = ListPool<CommandOverload<TSender>>.Shared.Rent();

        foreach (var command in commands)
        {
            if (CMD_DEBUG)
                log.Debug($"Checking command &1{command.Type.FullName}&r for &3{query}&r");

            if (flags?.Length > 0 && !flags.All(flag => command.Flags.Contains(flag)))
            {
                if (CMD_DEBUG)
                    log.Debug($"Flags do not match for command &1{command.Type.FullName}&r");
                
                continue;
            }

            if (command.Name.Length > spacedQuery.Length)
            {
                if (CMD_DEBUG)
                    log.Debug($"Command name &1{command.Name}&r is longer than query length");
                
                continue;
            }
            
            var match = false;
            var offset = 0;

            if (command.Name.Length > 0)
            {
                if (CMD_DEBUG)
                    log.Debug($"Comparing command name &3{command.FullName}&r with query string &3{query}&r");
                
                for (var x = 0; x < command.Name.Length; x++)
                {
                    if (CMD_DEBUG)
                        log.Debug($"Comparing string &3{command.Name[x]}&r with &3{spacedQuery[x]}&r");
                    
                    if (!string.Equals(command.Name[x], spacedQuery[x], StringComparison.OrdinalIgnoreCase))
                    {
                        if (CMD_DEBUG)
                            log.Debug($"Command name &3{command.Name[x]}&r does not match query string &3{spacedQuery[x]}&r");
                        
                        match = false;
                        offset = 0;

                        break;
                    }

                    match = true;
                    offset++;
                }

                if (!match)
                    continue;
            }
            
            if (CMD_DEBUG)
                log.Debug($"Searching for overload &3{query}&r in command &1{command.Type.FullName}&r");

            foreach (var overload in command.Overloads)
            {
                if (CMD_DEBUG)
                    log.Debug($"Checking overload &1{overload.FullName}&r for &3{query}&r");

                if (overload.Name.Length > spacedQuery.Length - offset)
                {
                    if (CMD_DEBUG) 
                        log.Debug($"Overload name &1{overload.FullName}&r is longer than query string &3{query}&r");
                    
                    continue;
                }

                var ovMatch = false;
                var ovOffset = 0;
                
                for (var x = 0; x < overload.Name.Length; x++)
                {
                    if (CMD_DEBUG)
                        log.Debug($"Comparing overload name &3{overload.Name[x]}&r with query string &3{spacedQuery[x + offset]}&r");
                    
                    if (!string.Equals(overload.Name[x], spacedQuery[x + offset], StringComparison.OrdinalIgnoreCase))
                    {
                        if (CMD_DEBUG)
                            log.Debug($"Overload name &3{overload.Name[x]}&r does not match query string &3{spacedQuery[x + offset]}&r");
                        
                        ovMatch = false;
                        break;
                    }

                    ovMatch = true;
                    ovOffset++;
                }

                if (ovMatch)
                {
                    if (CMD_DEBUG)
                        log.Debug($"Overload &1{overload.FullName}&r matches query string &3{query}&r");
                    
                    possible.Add(overload);

                    var args = string.Join(" ", spacedQuery.Skip(offset + ovOffset));

                    if (overload.NoContextParameterCount > 0)
                    {
                        var tokens = ListPool<Token>.Shared.Rent();

                        if (CMD_DEBUG)
                            log.Debug($"Parsing parameters, args query: &3{args}&r");

                        try
                        {
                            if (CMD_DEBUG)
                                log.Debug($"Parsing arguments &3{args}&r with &1{tokenParsers?.Count ?? 0}&r token parsers");

                            TokenParser.Parse(args, tokens, tokenParsers);
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Parsing arguments failed: &1{ex}&r");

                            ListPool<Token>.Shared.Return(tokens);
                            continue;
                        }

                        if (tokens.Count == 0)
                        {
                            if (CMD_DEBUG)
                                log.Debug("No tokens were parsed");

                            ListPool<Token>.Shared.Return(tokens);
                            continue;
                        }

                        if (CMD_DEBUG)
                        {
                            log.Debug($"Parsed &1{tokens.Count}&r tokens");

                            for (var i = 0; i < tokens.Count; i++)
                                log.Debug($"&1{i}&r = {tokens[i]?.ToString() ?? "null"}");

                            log.Debug(
                                $"Parsing arguments &3{args}&r with &1{tokenParsers?.Count ?? 0}&r tokens via parameter parsers");
                        }

                        var parser = ParameterParser.ParseTokens(tokens, overload.ParserParameters);

                        if (parser.Results == null
                            || parser.ResultsCount != tokens.Count
                            || !parser.Results.All(r => r is { IsValid: true, Exception: null }))
                        {
                            if (CMD_DEBUG)
                                log.Debug($"Parameter parsing failed: &1{parser.Error?.ToString() ?? "(null)"}&r");

                            ListPool<Token>.Shared.Return(tokens);
                            continue;
                        }

                        if (CMD_DEBUG)
                            log.Debug($"Parameter parsing succeeded: &1{parser.SuccessPercentage}%&r");
                        
                        return new(command, overload, null, true, args, query, tokens, parser);
                    }

                    if (CMD_DEBUG)
                        log.Debug("Overload has no parameters, skipped parsing");
                    
                    return new(command, overload, null, true, args, query, null, null);
                }
            }
        }
        
        if (CMD_DEBUG)
            log.Debug($"No matching command found for query &3{query}&r, &2{possible.Count}&r possible matches");
        
        return new(null, null, ListPool<CommandOverload<TSender>>.ReturnToArray(possible), 
            false, string.Empty, query, null, null);
    }

    private CommandRegisterError TryProcessParameters(MethodInfo method, ParameterInfo[] parameters,
        out List<CommandParameter> processed)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
        
        processed = new();
        
        if (CMD_DEBUG)
            log.Debug($"Processing &1{parameters.Length}&r parameters for method &1{method.Name}&r");

        if (parameters.Length < 1)
        {
            if (CMD_DEBUG)
                log.Debug($"Method &1{method.Name}&r has no parameters");
            
            return CommandRegisterError.NoParameters;
        }

        var firstParam = parameters[0];
        
        if (firstParam.ParameterType != ContextByRefType)
        {
            if (CMD_DEBUG)
                log.Debug($"Method &1{method.Name}&r first parameter (&6{firstParam.ParameterType}&r) is not of type &1{ContextByRefType}&");
            
            return CommandRegisterError.FirstParameterNotContext;
        }
        
        if (parameters.Length == 1)
        {
            if (CMD_DEBUG)
                log.Debug($"Method &1{method.Name}&r has only one parameter");
            
            return CommandRegisterError.Ok;
        }

        var attributes = CommandParameter.ParameterAttributes.GetAttributes(method,
                                                                        method.GetAllParameters()
                                                                            .Where(p => p.ParameterType != ContextByRefType)
                                                                            .ToArray());

        if (attributes.Count < 1)
        {
            log.Warn($"Method &1{method.Name}&r has no parameter attributes");
            
            attributes.ReturnToPool();
            return CommandRegisterError.NoParameters;
        }

        foreach (var attribute in attributes)
        {
            if (attribute.Target == null
                || attribute.MainAttribute == null
                || attribute.SubAttributes == null)
            {
                if (CMD_DEBUG)
                    log.Debug($"Encountered invalid parameter attribute");
                
                continue;
            }
            
            var name = string.IsNullOrEmpty(attribute.MainAttribute.Name)
                ? attribute.Target.Name 
                : attribute.MainAttribute.Name;
            
            var description = string.IsNullOrEmpty(attribute.MainAttribute.Description)
                ? attribute.Target.Name 
                : attribute.MainAttribute.Description;

            var parameter = new CommandParameter(attribute.Target, name, description);
            
            if (CMD_DEBUG)
                log.Debug($"Processing attributes for parameter &1{name}&r (&3{attribute.Target.Name}&r)");

            foreach (var subAttribute in attribute.SubAttributes)
            {
                if (subAttribute is PathAttribute pathAttribute
                    && pathAttribute.Parser != null
                    && pathAttribute.Path != null)
                {
                    if (CMD_DEBUG)
                        log.Debug($"Processing path attribute for parameter &1{name}&r (&3{pathAttribute.Parser.FullName}&r)");
                    
                    if (ParameterParser.AllParsers.TryGetFirst(x => x.GetType() == pathAttribute.Parser, out var parser)
                        || ParameterParser.TryGetParser(pathAttribute.Parser, out parser))
                    {
                        if (parser != null)
                        {
                            if (pathAttribute.Path.Length > 0)
                            {
                                parameter.Parsers[parser] = new(pathAttribute.Path);
                            }
                            else
                            {
                                parameter.Parsers[parser] = null;
                            }
                        }
                        else
                        {
                            log.Warn($"Parser &1{pathAttribute.Parser.FullName}&r was not found");
                        }
                    }
                }
                else if (SubAttributeProcessor != null)
                {
                    SubAttributeProcessor(method, parameter, attribute, subAttribute);
                }
            }
            
            attribute.SubAttributes.ReturnToPool();
            
            processed.Add(parameter);
        }
        
        attributes.ReturnToPool();

        if (processed.Count < 1)
        {
            log.Warn($"No parameters were processed for method &1{method.Name}&r");
            return CommandRegisterError.NoParameters;
        }

        if (CMD_DEBUG)
            log.Debug($"Processed &1{processed.Count}&r parameters for method &1{method.Name}&r");
        
        return CommandRegisterError.Ok;
    }
}