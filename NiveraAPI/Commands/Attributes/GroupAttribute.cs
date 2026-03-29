namespace NiveraAPI.Commands.Attributes;

/// <summary>
/// Represents an attribute used to define a command group for classes.
/// This attribute is used to specify metadata such as the name prefix
/// and permissions required for the commands in a class.
/// </summary>
[AttributeUsage(AttributeTargets.Class,
    AllowMultiple = false, 
    Inherited = false)]
public class GroupAttribute : Attribute
{
    /// <summary>
    /// The name of the command group. All commands in this class will be prefixed by this.
    /// </summary>
    public string[] Name { get; } = Array.Empty<string>();
    
    /// <summary>
    /// The permissions required to use any overload in this class.
    /// </summary>
    public string[] Permissions { get; } = Array.Empty<string>();
    
    /// <summary>
    /// The flags inherited by each overload in this class.
    /// </summary>
    public virtual object[] Flags { get; } = Array.Empty<object>();
    
    /// <summary>
    /// Whether or not to require all permissions in this class.
    /// </summary>
    public bool AllPermissions { get; }

    /// <summary>
    /// Creates a new command group attribute.
    /// </summary>
    /// <param name="name">The name of the command group. All commands in this class will be prefixed by this.</param>
    /// <param name="permissions">The permissions required to use any overload in this class.</param>
    /// <exception cref="ArgumentNullException">Thrown if name is null or empty.</exception>
    public GroupAttribute(string name, string? permissions = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        Name = name.Split(' ');

        if (permissions != null)
        {
            Permissions = permissions.Split(',');
            
            if (Permissions.Length > 0 && Permissions[0].Length > 0 && Permissions[0][0] == '*')
                AllPermissions = true;
        }
    }
}