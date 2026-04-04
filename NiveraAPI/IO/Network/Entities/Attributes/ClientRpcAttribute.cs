namespace NiveraAPI.IO.Network.Entities.Attributes;

/// <summary>
/// Represents an attribute used to mark a method for Remote Procedure Call (RPC) purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = false)]
public class ClientRpcAttribute : Attribute
{
    /// <summary>
    /// Whether or not the RPC method has a return value.
    /// </summary>
    public bool HasReturnValue { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="ClientRpcAttribute"/> class.
    /// </summary>
    /// <param name="hasReturnValue">Whether the RPC method has a return value.</param>
    public ClientRpcAttribute(bool hasReturnValue = false)
    {
        HasReturnValue = hasReturnValue;
    }
}