using NiveraAPI.IO.Storage.Interfaces;

namespace NiveraAPI.IO.Storage;

/// <summary>
/// Represents an abstract class that provides functionality for serializing and deserializing storage values.
/// </summary>
public abstract class StorageSerializer
{
    /// <summary>
    /// Serializes the provided storage value into a string representation.
    /// </summary>
    /// <param name="value">The storage value to be serialized. Represents the object implementing the <see cref="IStorageValue"/> interface.</param>
    /// <returns>A string representation of the serialized storage value.</returns>
    public abstract string Serialize(IStorageValue value);

    /// <summary>
    /// Deserializes the provided data and populates the specified storage value instance with the deserialized content.
    /// </summary>
    /// <param name="data">The string representation of the serialized data to be deserialized.</param>
    /// <param name="value">The storage value instance to populate with the deserialized content. Represents an object implementing the <see cref="IStorageValue"/> interface.</param>
    public abstract void Deserialize(string data, IStorageValue value);
}