using NiveraAPI.Commands.Interfaces;

namespace NiveraAPI.Commands.Results;

/// <summary>
/// Represents a result that sends a text message to the sender.
/// </summary>
public struct TextResult : IResult
{
    /// <summary>
    /// Whether or not the command's execution was successful.
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// The text to send to the sender.
    /// </summary>
    public string Text { get; }
    
    /// <summary>
    /// Creates a new <see cref="TextResult"/> instance.
    /// </summary>
    /// <param name="success">Whether or not the command's execution was successful.</param>
    /// <param name="text">The text to send to the sender.</param>
    public TextResult(bool success, string text)
    {
        Success = success;
        Text = text;
    }
}