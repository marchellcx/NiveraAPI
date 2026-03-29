namespace NiveraAPI.IO.Serialization.Interfaces;

/// <summary>
/// Interface for a serializer that can dispose objects.
/// </summary>
public interface IDisposingSerializer
{
    /// <summary>
    /// Disposes the given object.
    /// </summary>
    /// <param name="obj">The object to dispose.</param>
    void DisposeObject(ISerializableObject obj);
}