namespace NiveraAPI.IO.Storage;

/// <summary>
/// Represents a strongly-typed property of a file with support for internal state tracking,
/// such as dirty state and serialization/deserialization logic.
/// </summary>
/// <typeparam name="T">The data type of the property's value.</typeparam>
public class FileProperty<T> : FileProperty
{
    /// <summary>
    /// Gets or sets the value of the file property.
    /// Changing the value will mark the property as dirty, indicating that it has been modified
    /// and may require saving or synchronization with its associated file storage.
    /// </summary>
    public T Value
    {
        get => (T)value;
        set
        {
            this.value = value;
            this.dirty = true;
        }
    }
}