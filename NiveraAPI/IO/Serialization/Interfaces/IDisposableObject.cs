namespace NiveraAPI.IO.Serialization.Interfaces;

/// <summary>
/// Interface for objects that can be disposed.
/// </summary>
public interface IDisposableObject
{
    /// <summary>
    /// Whether or not the object should be disposed.
    /// </summary>
    /// <returns>true if the object should be disposed, false otherwise.</returns>
    bool ShouldDispose();
}