using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Storage.Interfaces;

/// <summary>
/// Represents a storage value interface that defines properties and methods for managing storage data.
/// </summary>
public interface IStorageValue
{
    /// <summary>
    /// The type of the value.
    /// </summary>
    Type Type { get; }
    
    /// <summary>
    /// The name of the value.
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// The path to the value.
    /// </summary>
    string Path { get; set; }
    
    /// <summary>
    /// Whether or not the value is dirty.
    /// </summary>
    bool IsDirty { get; set; }
    
    /// <summary>
    /// The directory that contains the value.
    /// </summary>
    StorageDirectory Directory { get; set; }

    /// <summary>
    /// Serializes the current storage value to a JSON string representation.
    /// </summary>
    /// <returns>A JSON string representing the serialized storage value.</returns>
    string Serialize();

    /// <summary>
    /// Deserializes the provided JSON string and populates the storage value with the data.
    /// </summary>
    /// <param name="json">The JSON string containing the data to be deserialized.</param>
    void Deserialize(string json);
    
    /// <summary>
    /// Sets the value of the storage value.
    /// </summary>
    /// <param name="value">The value to be set.</param>
    void SetValue(object value);
}