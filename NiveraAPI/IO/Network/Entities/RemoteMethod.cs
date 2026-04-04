using System.Reflection;

namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// Represents a remote method.
/// </summary>
public class RemoteMethod
{
    /// <summary>
    /// The original method to invoke.
    /// </summary>
    public MethodInfo? Target;

    /// <summary>
    /// Whether the method has a return value.
    /// </summary>
    /// <remarks>This avoids unnecessary de-pooling of a ByteWriter instance.</remarks>
    public bool HasReturnValue;

    /// <summary>
    /// The local index of the method.
    /// </summary>
    public ushort Index;

    /// <summary>
    /// Whether the method is a remote method.
    /// </summary>
    public bool IsRemote;

    /// <summary>
    /// The name of the remote method.
    /// </summary>
    public string? RemoteName;
}