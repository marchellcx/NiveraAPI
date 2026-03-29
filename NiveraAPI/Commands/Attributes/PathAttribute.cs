namespace NiveraAPI.Commands.Attributes;

/// <summary>
/// Represents an attribute used to specify a property path for parsing.
/// </summary>
public class PathAttribute : ParameterSubAttribute
{
    /// <summary>
    /// The type of parser (or parsed object) used.
    /// </summary>
    public Type Parser { get; }
    
    /// <summary>
    /// The property path.
    /// </summary>
    public string[] Path { get; }

    /// <summary>
    /// Creates a new path attribute.
    /// </summary>
    /// <param name="parser">The type of the parser to use for parsing the path.</param>
    /// <param name="path">The path segments to be used for parsing.</param>
    /// <exception cref="ArgumentNullException">Thrown if parser or path is null.</exception>
    /// <exception cref="ArgumentException">Thrown if path is empty.</exception>
    public PathAttribute(Type parser, params string[] path)
    {
        Parser = parser ?? throw new ArgumentNullException(nameof(parser));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        
        if (path.Length == 0)
            throw new ArgumentException("Path cannot be empty", nameof(path));
    }
}