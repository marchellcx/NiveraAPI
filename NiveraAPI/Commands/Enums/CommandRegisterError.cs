namespace NiveraAPI.Commands.Enums;

public enum CommandRegisterError
{
    /// <summary>
    /// The command was registered successfully.
    /// </summary>
    Ok,
    
    /// <summary>
    /// A command with the same name and flags already exists.
    /// </summary>
    CommandExists,
    
    /// <summary>
    /// The command has no overloads.
    /// </summary>
    OverloadsEmpty,
    
    /// <summary>
    /// The command is missing a constructor.
    /// </summary>
    NoConstructor,
    
    /// <summary>
    /// The command's return type is not awaitable.
    /// </summary>
    NoAwaiter,
    
    /// <summary>
    /// The method has no parameters.
    /// </summary>
    NoParameters,
    
    /// <summary>
    /// The first parameter of the method is not a context.
    /// </summary>
    FirstParameterNotContext,
    
    /// <summary>
    /// The command contains a parameter that has no parser.
    /// </summary>
    NoParser,
}