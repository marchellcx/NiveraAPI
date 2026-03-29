namespace NiveraAPI.Commands.Interfaces;

/// <summary>
/// Used to await the result of a command.
/// </summary>
public interface IAwaiter<TSender> where TSender : class
{
    /// <summary>
    /// Awaits the result of a command and invokes the provided callback upon completion.
    /// </summary>
    /// <param name="context">The command context containing information about the sender of the command.</param>
    /// <param name="result">The result object to be awaited.</param>
    /// <param name="callback">The callback action to invoke once the result is processed.</param>
    void AwaitResult(ref CommandContext<TSender> context, object? result, Action callback);
}