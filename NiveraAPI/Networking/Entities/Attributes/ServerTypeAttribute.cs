namespace NiveraAPI.Networking.Entities.Attributes;

/// <summary>
/// Used to override type names on the remote side (server).
/// </summary>
[AttributeUsage(AttributeTargets.Class,
    AllowMultiple = false, 
    Inherited = false)]
public class ServerTypeAttribute : Attribute
{
    /// <summary>
    /// The name of the type on the remote side.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Creates a new instance of the ServerTypeAttribute class.
    /// </summary>
    /// <param name="name">The name of the type on the remote side.</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is null or whitespace.</exception>
    public ServerTypeAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        
        Name = name;
    }
}