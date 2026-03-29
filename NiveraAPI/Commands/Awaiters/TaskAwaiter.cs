using NiveraAPI.Commands.Interfaces;
using NiveraAPI.Utilities;

namespace NiveraAPI.Commands.Awaiters;

/// <summary>
/// Represents a mechanism to await the result of a Task-based command execution and invoke a callback upon its completion.
/// Allows specifying whether the continuation should occur on the main thread.
/// </summary>
/// <typeparam name="TSender">
/// The type of the sender associated with the command. This must be a reference type.
/// </typeparam>
public class TaskAwaiter<TSender> : IAwaiter<TSender> where TSender : class
{
    /// <summary>
    /// Awaits the result of a command and invokes the provided callback upon completion.
    /// </summary>
    /// <param name="context">The command context containing information about the sender of the command.</param>
    /// <param name="result">The result object to be awaited.</param>
    /// <param name="callback">The callback action to invoke once the result is processed.</param>
    public void AwaitResult(ref CommandContext<TSender> context, object? result, Action callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        
        if (result is not Task task)
            throw new ArgumentException("Result must be a Task.", nameof(result));
        
        task.ContinueWithOnMain(_ => callback());
    }
}