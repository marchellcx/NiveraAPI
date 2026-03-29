using System.Reflection;

namespace NiveraAPI.Networking.Entities;

/// <summary>
/// Represents a remote sync variable.
/// </summary>
public class RemoteSyncVar
{
    /// <summary>
    /// The target sync variable.
    /// </summary>
    public FieldInfo Field;

    /// <summary>
    /// The method called when the sync variable is written.
    /// </summary>
    public MethodInfo? Hook;
    
    /// <summary>
    /// The method used to read the sync variable.
    /// </summary>
    public MethodInfo Reader;

    /// <summary>
    /// The index of the sync variable.
    /// </summary>
    public ushort Index;

    /// <summary>
    /// The target of the reader method.
    /// </summary>
    public object ReaderTarget;
}