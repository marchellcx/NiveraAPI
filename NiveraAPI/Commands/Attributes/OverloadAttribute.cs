namespace NiveraAPI.Commands.Attributes;

/// <summary>
/// Represents an attribute used to mark a command overload.
/// </summary>
[AttributeUsage(AttributeTargets.Method, 
    AllowMultiple = false, 
    Inherited = false)]
public class OverloadAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the overload.
    /// </summary>
    public string[] Name { get; } = Array.Empty<string>();

    /// <summary>
    /// Gets the description of the overload.
    /// </summary>
    public string Description { get; } = string.Empty;

    /// <summary>
    /// Gets the permissions required to use this overload.
    /// </summary>
    public string[] Permissions { get; } = Array.Empty<string>();

    /// <summary>
    /// Whether or not this overload requires all provided permissions.
    /// </summary>
    public bool AllPermissions { get; } = false;
    
    /// <summary>
    /// Gets the flags associated with this overload.
    /// </summary>
    public virtual object[] Flags { get; } = Array.Empty<object>();
    
    /// <summary>
    /// Creates a new overload attribute.
    /// </summary>
    public OverloadAttribute() { }
    
    /// <summary>
    /// Creates a new overload attribute with the given name.
    /// </summary>
    /// <param name="name">The name of the overload.</param>
    /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
    public OverloadAttribute(string name) 
        => Name = name?.Split(' ') ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Creates a new overload attribute with the given name and description.
    /// </summary>
    /// <param name="name">The name of the overload.</param>
    /// <param name="description">The description of the overload.</param>
    /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
    public OverloadAttribute(string name, string? description)
    {
        Name = name?.Split(' ') ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
    }
    
    /// <summary>
    /// Creates a new overload attribute with the given name, description, and permissions.
    /// </summary>
    /// <param name="name">The name of the overload.</param>
    /// <param name="description">The description of the overload.</param>
    /// <param name="permissions">The permissions required to use the overload, split by comma. Prefix with * to require all permissions.</param>
    /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
    public OverloadAttribute(string name, string? description, string? permissions)
    {
        Name = name?.Split(' ') ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        
        Permissions = permissions?.Split(',') ?? Array.Empty<string>();

        if (Permissions.Length > 0 && Permissions[0].Length > 0 && Permissions[0][0] == '*')
            AllPermissions = true;
    }
}