using NiveraAPI.Commands.Interfaces;
using NiveraAPI.Utilities;

namespace NiveraAPI.Commands.Awaiters;

/// <summary>
/// Represents an awaiter for commands that do not return a value.
/// </summary>
/// <typeparam name="TSender"></typeparam>
public class VoidAwaiter<TSender> : IAwaiter<TSender> where TSender : class
{
    /// <summary>
    /// A static, shared instance of <see cref="VoidAwaiter{TSender}"/> configured to execute callbacks on the current thread.  
    /// </summary>
    public static readonly VoidAwaiter<TSender> Singleton = new();

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

        if (!ThreadHelper.IsMainThread)
        {
            callback.RunOnMainThread();
        }
        else
        {
            callback();
        }
    }
}