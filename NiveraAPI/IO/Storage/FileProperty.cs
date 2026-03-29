namespace NiveraAPI.IO.Storage;

/// <summary>
/// Represents a property of a file object.
/// </summary>
public class FileProperty
{
    internal bool dirty;
    internal object value;

    /// <summary>
    /// Gets the raw data of the property.
    /// </summary>
    public byte[] Raw { get; internal set; }
    
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string Name { get; internal set; }
    
    /// <summary>
    /// Path to the file of the property.
    /// </summary>
    public string File { get; internal set; }
    
    /// <summary>
    /// The parent object.
    /// </summary>
    public FileObject Object { get; internal set; }

    /// <summary>
    /// The parent storage.
    /// </summary>
    public FileStorage Storage => Object.Storage;

    /// <summary>
    /// Gets the current value of the property.
    /// </summary>
    /// <returns>The current value assigned to the property.</returns>
    public object GetValue()
        => value;

    /// <summary>
    /// Sets the value of the property. Marks the property as dirty if the value has changed and is not guarded.
    /// </summary>
    /// <param name="value">The new value to set for the property.</param>
    public void SetValue(object value)
        => this.value = value;

    /// <summary>
    /// Clears the current state of the property, including its value, name, associated file, and object reference.
    /// Resets the dirty and guard flags. This method effectively removes all associations of the property
    /// and prepares it for disposal or reuse.
    /// </summary>
    public void Destroy()
    {
        dirty = false;
        value = null!;

        Raw = null!;
        File = null!;
        Name = null!;
        Object = null!;
    }
    
    internal void Read()
        => value = FileStorage.Deserialize(Raw = System.IO.File.ReadAllBytes(File));

    internal void Write()
        => System.IO.File.WriteAllBytes(File, Raw = FileStorage.Serialize(value, Storage.UseTypeCode, Storage.Format));
}