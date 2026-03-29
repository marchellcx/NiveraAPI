namespace NiveraAPI.TypeParsing;

/// <summary>
/// Represents a named parameter in a command.
/// </summary>
public struct NamedParameter
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// The value of the parameter.
    /// </summary>
    public readonly object Value;
    
    /// <summary>
    /// Creates a new instance of the NamedParameter struct.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    public NamedParameter(string name, object value)
    {
        Name = name;
        Value = value;
    }
}