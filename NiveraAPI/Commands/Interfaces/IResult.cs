namespace NiveraAPI.Commands.Interfaces;

/// <summary>
/// Base interface for command results.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Whether or not the command's execution was successful.
    /// </summary>
    bool Success { get; }
}