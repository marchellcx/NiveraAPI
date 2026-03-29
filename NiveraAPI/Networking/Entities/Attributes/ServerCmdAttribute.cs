namespace NiveraAPI.Networking.Entities.Attributes;

/// <summary>
/// Represents an attribute used to designate a method as a server command.
/// This attribute can only be applied to methods and is not inherited.
/// </summary>
[AttributeUsage(AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = false)]
public class ServerCmdAttribute : Attribute
{
    /// <summary>
    /// Whether or not the server command has a return value.
    /// </summary>
    public bool HasReturnValue { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="ServerCmdAttribute"/> class.
    /// </summary>
    /// <param name="hasReturnValue">Whether the server command has a return value.</param>
    public ServerCmdAttribute(bool hasReturnValue = false)
    {
        HasReturnValue = hasReturnValue;
    }
}