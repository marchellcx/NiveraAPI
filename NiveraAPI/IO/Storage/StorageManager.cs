using System.Collections.Concurrent;

using NiveraAPI.Logs;

namespace NiveraAPI.IO.Storage;

/// <summary>
/// Provides functionality for managing a collection of <see cref="StorageDirectory"/> objects,
/// including initialization, retrieval, addition, removal, and saving operations.
/// The storage directories are managed based on a specified path.
/// </summary>
public class StorageManager
{
    private volatile bool debugLogs;
    private volatile bool initialized;
    
    private volatile string path;
    private volatile ConcurrentDictionary<string, StorageDirectory> dirs = new();

    /// <summary>
    /// Gets the path to the storage manager.
    /// </summary>
    public string Path => path;
    
    /// <summary>
    /// Gets the number of directories in the storage manager.
    /// </summary>
    public int DirectoryCount => dirs.Count;

    /// <summary>
    /// Gets or sets a value indicating whether debug logs are enabled.
    /// </summary>
    public bool DebugLogs
    {
        get => debugLogs;
        set => debugLogs = value;
    }

    /// <summary>
    /// Gets the directories in the storage manager.
    /// </summary>
    public IReadOnlyDictionary<string, StorageDirectory> Directories => dirs;

    /// <summary>
    /// Creates a new instance of the <see cref="StorageManager"/> class.
    /// </summary>
    public StorageManager(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));
        
        this.path = path;
    }

    /// <summary>
    /// Initializes the <see cref="StorageManager"/> by iterating over all directories in the
    /// configured path and creating corresponding <see cref="StorageDirectory"/> instances.
    /// Each directory is initialized and added to the internal collection for management.
    /// This method must be called before using any other functionality of the <see cref="StorageManager"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the method is called on a <see cref="StorageManager"/> instance that is already initialized.
    /// </exception>
    /// <remarks>
    /// Logs errors encountered while processing individual directories or accessing the configured path,
    /// but does not throw exceptions for these errors, allowing the initialization to proceed for
    /// any remaining valid directories.
    /// </remarks>
    public void Initialize()
    {
        if (initialized)
            throw new InvalidOperationException("StorageManager has already been initialized.");

        try
        {
            Log.Debug($"Initializing storage in &1{path}&r ..", DebugLogs);
            
            foreach (var dir in Directory.GetDirectories(path))
            {
                try
                {
                    Log.Debug($"Loading directory &1{dir}&r ..", DebugLogs);
                    
                    var name = System.IO.Path.GetFileName(dir);
                    var directory = new StorageDirectory(name, dir);

                    directory.Manager = this;
                    directory.Initialize();
                    
                    dirs.TryAdd(name, directory);
                    
                    Log.Debug($"Directory &1{name}&r loaded successfully.", DebugLogs);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed while loading directory &1{dir}&r:\n{ex}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed while initializing storage in &1{Path}&r:\n{ex}");
        }
        
        initialized = true;
    }

    /// <summary>
    /// Saves all storage directories managed by the StorageManager by invoking the
    /// <c>SaveAll</c> method on each <see cref="StorageDirectory"/>. This ensures that all
    /// data associated with every directory is persisted to their respective file paths,
    /// regardless of whether the data has changed since the last save.
    /// </summary>
    /// <exception cref="Exception">
    /// Logs and handles any exceptions thrown during the save operation on a directory
    /// but does not rethrow them.
    /// </exception>
    public void SaveAll()
    {
        Log.Debug($"Saving all files in storage &1{Path}&r ..", DebugLogs);
        
        foreach (var kvp in dirs)
        {
            try
            {
                Log.Debug($"Saving files in directory &1{kvp.Key}&r ..", DebugLogs);
                
                kvp.Value.SaveAll();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed while saving all files in directory &1{kvp.Key}&r:\n{ex}");
            }
        }
    }

    /// <summary>
    /// Saves all "dirty" storage directories managed by the StorageManager by invoking the
    /// <c>SaveDirty</c> method on each <see cref="StorageDirectory"/>. This ensures that any
    /// changes made to storage values within each directory are persisted to their respective
    /// file paths.
    /// </summary>
    /// <exception cref="Exception">
    /// Logs and handles any exceptions thrown during the save operation on a directory but does not
    /// rethrow them.
    /// </exception>
    public void SaveDirty()
    {
        foreach (var kvp in dirs)
        {
            try
            {
                kvp.Value.SaveDirty();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed while saving dirty files in directory &1{kvp.Key}&r:\n{ex}");
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="StorageDirectory"/> from the collection by its name.
    /// </summary>
    /// <param name="name">The name of the storage directory to retrieve.</param>
    /// <param name="dir">
    /// When this method returns, contains the <see cref="StorageDirectory"/> associated with the specified name,
    /// if it exists; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if a <see cref="StorageDirectory"/> with the specified name exists and is successfully retrieved;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> is null or empty.</exception>
    public bool TryGet(string name, out StorageDirectory dir)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        return dirs.TryGetValue(name, out dir);
    }

    /// <summary>
    /// Retrieves the storage directory associated with the specified name.
    /// </summary>
    /// <param name="name">The name of the storage directory to retrieve.</param>
    /// <returns>The <see cref="StorageDirectory"/> corresponding to the specified name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided name is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a storage directory with the specified name does not exist.</exception>
    public StorageDirectory Get(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        return dirs.TryGetValue(name, out var dir)
            ? dir
            : throw new KeyNotFoundException($"Directory with name &1{name}&r does not exist.");
    }

    /// <summary>
    /// Adds a new <see cref="StorageDirectory"/> to the collection, creating it if it does not already exist.
    /// </summary>
    /// <param name="name">The name of the storage directory to add. This cannot be null or empty.</param>
    /// <returns>The <see cref="StorageDirectory"/> instance associated with the specified name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="name"/> is null or empty.</exception>
    public StorageDirectory Add(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (dirs.TryGetValue(name, out var dir))
            return dir;

        Log.Debug($"Creating a new directory: &1{name}&r", DebugLogs);
        
        dir = new StorageDirectory(name, System.IO.Path.Combine(path, name));
        dir.Manager = this;
        
        try
        {
            dir.Initialize();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize directory &1{name}&r:\n{ex}");
        }

        dirs.TryAdd(name, dir);
        return dir;   
    }

    /// <summary>
    /// Removes the specified directory from the collection.
    /// </summary>
    /// <param name="name">
    /// The name of the directory to be removed. This value must not be null or empty.
    /// </param>
    /// <param name="deleteFiles">
    /// A boolean indicating whether to delete the associated files from the filesystem after removing the directory.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the directory was successfully removed. Returns <c>true</c> if the directory was found and removed; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="name"/> parameter is null or empty.
    /// </exception>
    public bool Remove(string name, bool deleteFiles = false)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        Log.Debug($"Removing directory &1{name}&r ..", DebugLogs);
        
        if (!dirs.TryRemove(name, out var dir))
            return false;

        try
        {
            dir.ClearValues(deleteFiles);
        }
        catch (Exception ex)
        {
            Log.Error($"Error while clearing directory &1{dir.Name}&r:\n{ex}");
        }

        return true;
    }

    /// <summary>
    /// Removes all storage directories managed by this <see cref="StorageManager"/> instance.
    /// Optionally deletes the underlying files associated with each directory.
    /// </summary>
    /// <param name="deleteFiles">
    /// If set to <c>true</c>, the underlying files for each directory will also be deleted.
    /// If set to <c>false</c>, only the in-memory directory references will be removed.
    /// </param>
    public void RemoveAll(bool deleteFiles = false)
    {
        Log.Debug($"Removing all directories ..", DebugLogs);
        
        foreach (var kvp in dirs)
        {
            try
            {
                kvp.Value.ClearValues(deleteFiles);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while clearing directory &1{kvp.Key}&r:\n{ex}");
            }
        }
        
        dirs.Clear();
    }
}