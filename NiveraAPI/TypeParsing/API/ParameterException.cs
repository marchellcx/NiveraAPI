namespace NiveraAPI.TypeParsing.API;

/// <summary>
/// Represents an exception thrown during parameter parsing.
/// </summary>
public class ParameterException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="ParameterException"/>.
    /// </summary>
    /// <param name="message">The error message describing the issue that occurred during parameter parsing.</param>
    public ParameterException(string message) : base(message)
    {
        
    }
}