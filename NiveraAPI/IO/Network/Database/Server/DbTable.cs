namespace NiveraAPI.IO.Network.Database.Server;

/// <summary>
/// Represents a server table which handles data storage and operations in a server-side database.
/// Provides functionality for managing server table entries and maintaining the file system directory associated with the table.
/// </summary>
public class DbTable : IDisposable
{
    private readonly Dictionary<string, byte[]> items = new();

    /// <summary>
    /// Gets the name of the server table extracted from its file path.
    /// </summary>
    /// <remarks>
    /// The value of the property is derived from the file name of the provided path, excluding the extension
    /// and a predefined prefix ("DB-") if present. This property is read-only.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets the file system path associated with the server table.
    /// </summary>
    /// <remarks>
    /// This property represents the directory path where the server table's data files are stored.
    /// It is initialized during the creation of the <see cref="DbTable"/> instance and is read-only.
    /// </remarks>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbTable"/> class.
    /// </summary>
    /// <param name="path">The file system path to the server table's data files.</param>
    /// <exception cref="ArgumentNullException">Thrown if the specified path is null or empty.</exception>
    public DbTable(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));
        
        Path = path;
        Name = System.IO.Path.GetFileNameWithoutExtension(path).Substring(4);
    }

    /// <summary>
    /// Clears all entries from the internal dictionary and deletes all associated files from the file system.
    /// Recreates the directory to ensure it remains available for use.
    /// </summary>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs while attempting to delete or recreate the directory.
    /// </exception>
    public void Clear()
    {
        items.Clear();
        
        Directory.Delete(Path, true);
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Retrieves the data associated with the specified item name.
    /// </summary>
    /// <param name="name">The name of the item to retrieve.</param>
    /// <returns>
    /// The byte array containing the data of the specified item if it exists; otherwise, null.
    /// </returns>
    public byte[]? GetItem(string name)
        => items.TryGetValue(name, out var data) ? data : null;

    /// <summary>
    /// Removes an entry from the internal dictionary and deletes its associated file from the file system.
    /// </summary>
    /// <param name="name">The name of the item to remove. This serves as the key in the dictionary and the name of the file to delete.</param>
    /// <returns>A boolean value indicating whether the item was successfully removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the specified name is null or empty.</exception>
    public bool RemoveItem(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!items.Remove(name))
            return false;
        
        File.Delete(System.IO.Path.Combine(Path, $"{name}.db"));
        return true;
    }

    /// <summary>
    /// Updates the internal dictionary by adding or replacing an entry with the specified name and binary data.
    /// </summary>
    /// <param name="name">The name of the item to update or add. This serves as the key in the dictionary.</param>
    /// <param name="data">The binary data associated with the item. This serves as the value in the dictionary.</param>
    /// <exception cref="ArgumentNullException">Thrown if the specified name is null, empty, or if the data is null.</exception>
    public void UpdateItem(string name, byte[] data)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        items[name] = data;
    }

    /// <summary>
    /// Reads all items from the files in the specified directory path, clears any existing data,
    /// and populates the internal dictionary with the file names (without extensions) as keys
    /// and their corresponding binary file data as values.
    /// </summary>
    public void ReadItems()
    {
        items.Clear();
        
        foreach (var file in Directory.GetFiles(Path, "*.db"))
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(file);
            var data = File.ReadAllBytes(file);
            
            items.Add(name, data);
        }
    }

    /// <summary>
    /// Writes all items from the internal dictionary to the file system as binary files.
    /// The file names are derived from the keys of the dictionary with a '.db' extension,
    /// and the file contents are the corresponding binary data.
    /// </summary>
    public void WriteItems()
    {
        foreach (var kvp in items)
        {
            var path = System.IO.Path.Combine(Path, $"{kvp.Key}.db");
            var data = kvp.Value;
            
            File.WriteAllBytes(path, data);
        }
    }
    
    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        items.Clear();
    }
}