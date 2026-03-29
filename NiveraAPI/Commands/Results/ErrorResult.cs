using NiveraAPI.Commands.Interfaces;

namespace NiveraAPI.Commands.Results;

/// <summary>
/// Represents a result that indicates an error occurred during command execution.
/// </summary>
public struct ErrorResult : IResult
{
    /// <summary>
    /// Whether or not the command's execution was successful.
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// Additional error message.
    /// </summary>
    public string? Message { get; }
    
    /// <summary>
    /// The exception that caused the error, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Creates a new error result.
    /// </summary>
    /// <param name="message">The error message to include in the result.</param>
    /// <param name="exception">The exception that caused the error, if any.</param>
    public ErrorResult(string? message = null, Exception? exception = null)
    {
        Success = false;
        
        Message = message;
        Exception = exception;
    }

    /// <summary>
    /// Returns a string representation of the error result, including the error message and the exception details if available.
    /// </summary>
    /// <returns>A string that represents the error result, containing the message and/or exception information.</returns>
    public override string ToString()
    {
        var str = string.Empty;
        
        if (Message != null) 
            str += Message;

        if (Exception != null)
        {
            if (Message == null)
            {
                str = Exception.ToString();
            }
            else
            {
                str += $"\n{Exception}";
            }
        }
        
        return str;
    }
}