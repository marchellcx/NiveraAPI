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
    public volatile MethodInfo? Target;

    /// <summary>
    /// The method used to write the return value.
    /// </summary>
    public volatile MethodInfo? ReturnWriter;
    
    /// <summary>
    /// An array of MethodInfo objects used to extract or read parameters for the remote method.
    /// </summary>
    public volatile MethodInfo[]? ParameterReaders;

    /// <summary>
    /// Whether the method is a basic method.
    /// </summary>
    public volatile bool IsBasic;

    /// <summary>
    /// Whether the method has a return value.
    /// </summary>
    /// <remarks>This avoids unnecessary de-pooling of a ByteWriter instance.</remarks>
    public volatile bool HasReturnValue;

    /// <summary>
    /// The local index of the method.
    /// </summary>
    public volatile ushort Index;

    /// <summary>
    /// Whether the method is a remote method.
    /// </summary>
    public volatile bool IsRemote;

    /// <summary>
    /// The name of the remote method.
    /// </summary>
    public volatile string? RemoteName;
}