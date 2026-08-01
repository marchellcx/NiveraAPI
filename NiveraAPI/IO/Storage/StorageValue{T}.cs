using Newtonsoft.Json;

using NiveraAPI.IO.Storage.Interfaces;

namespace NiveraAPI.IO.Storage;

/// <summary>
/// Represents a storage value that provides serialized data handling for a specific type <typeparamref name="T"/>.
/// Used to persist and manage data within a storage directory structure.
/// </summary>
/// <typeparam name="T">The type of the value being stored and managed by this instance.</typeparam>
public class StorageValue<T> : IStorageValue
{
    private T value;
    
    private volatile bool isDirty;
    private volatile StorageDirectory directory;
    
    internal volatile string name;
    internal volatile string path;
    
    internal volatile bool armed;

    /// <summary>
    /// The .NET type of the value stored in this storage instance.
    /// </summary>
    public Type Type { get; } = typeof(T);
    
    /// <summary>
    /// Gets the name associated with the storage value.
    /// This name is used as an identifier within the storage system.
    /// </summary>
    public string Name 
    {
        get => name;
        set => name = value;
    }

    /// <summary>
    /// Gets or sets the file system path associated with the storage value.
    /// This path is used to identify the location where the storage value is persisted.
    /// </summary>
    public string Path
    {
        get => path;
        set => path = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the storage value has been modified since it was last persisted.
    /// This property is used to track changes and determine the need for saving the value back to storage.
    /// </summary>
    public bool IsDirty
    {
        get => isDirty;
        set => isDirty = value;
    }

    /// <summary>
    /// Gets or sets the <see cref="StorageDirectory"/> that the current storage value is associated with.
    /// Represents the parent storage directory managing this value, enabling hierarchical
    /// organization and access to related storage values.
    /// </summary>
    public StorageDirectory Directory
    {
        get => directory;
        set => directory = value;
    }

    /// <summary>
    /// Gets or sets the value of type <typeparamref name="T"/> stored in this storage instance.
    /// Assigning a new value marks the instance as dirty, indicating the need for persistence.
    /// </summary>
    public T Value
    {
        get => value;
        set
        {
            this.value = value;

            if (!armed)
                return;
            
            this.isDirty = true;
        }
    }

    /// <summary>
    /// Serializes the current value of type <typeparamref name="T"/> into a JSON string
    /// using the serialization settings configured in the associated <see cref="StorageDirectory"/>.
    /// </summary>
    /// <returns>A JSON string representation of the current value of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is null and cannot be serialized.
    /// </exception>
    public string Serialize()
    {
        if (value == null)
            throw new InvalidOperationException("Value is null.");

        return JsonConvert.SerializeObject(value, Formatting.None);
    }

    /// <summary>
    /// Deserializes the provided JSON string into an object of type <typeparamref name="T"/>
    /// and assigns it to the internal value of this instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize into an object of type <typeparamref name="T"/>.</param>
    /// <exception cref="InvalidCastException">
    /// Thrown when the deserialized object cannot be cast to the expected type <typeparamref name="T"/>.
    /// </exception>
    public void Deserialize(string json)
    {
        var obj = JsonConvert.DeserializeObject<T>(json);
        
        if (obj is not T cast)
            throw new InvalidCastException($"Cannot cast {obj?.GetType()?.ToString() ?? "null"} to {typeof(T)}.");
        
        value = cast;
    }

    /// <summary>
    /// Sets the current value of the storage object to the specified <paramref name="value"/>.
    /// The provided value must be compatible with the expected type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">The new value to assign to the storage object. Must be of type <typeparamref name="T"/>.</param>
    /// <exception cref="InvalidCastException">
    /// Thrown when the provided <paramref name="value"/> is not of the expected type <typeparamref name="T"/>.
    /// </exception>
    public void SetValue(object value)
    {
        if (value is not T cast)
            throw new InvalidCastException($"Cannot cast {value?.GetType()?.ToString() ?? "null"} to {typeof(T)}.");
        
        this.value = cast;
        this.armed = true;
    }
}