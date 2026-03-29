using System.Reflection;
using NiveraAPI.Commands.API;
using NiveraAPI.Commands.Enums;

namespace NiveraAPI.Commands;

/// <summary>
/// Represents the result of a command registration attempt. It contains details about the targeted method,
/// the registered command overload (if any), and any error encountered during the registration process.
/// </summary>
/// <typeparam name="TSender">
/// The type of the sender associated with the command. This is typically a class that represents the context
/// in which the command is executed.
/// </typeparam>
public struct CommandRegisterResult<TSender> where TSender : class
{
    /// <summary>
    /// The method that was targeted for registration.
    /// </summary>
    public MethodInfo Method;
    
    /// <summary>
    /// The overload that was registered.
    /// </summary>
    public CommandOverload<TSender>? Overload;

    /// <summary>
    /// The error that occurred during registration.
    /// </summary>
    public CommandRegisterError Error;
    
    /// <summary>
    /// Creates a new command registration result.
    /// </summary>
    /// <param name="method">The method that was targeted for registration.</param>
    /// <param name="overload">The overload that was registered.</param>
    /// <param name="error">The error that occurred during registration.</param>
    public CommandRegisterResult(MethodInfo method, CommandOverload<TSender>? overload, CommandRegisterError error)
    {
        Method = method;
        Overload = overload;
        Error = error;
    }
}