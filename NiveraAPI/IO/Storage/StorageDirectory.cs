using System.Collections.Concurrent;

using Newtonsoft.Json;

using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Storage.Interfaces;

using NiveraAPI.Logs;

namespace NiveraAPI.IO.Storage;

/// <summary>
/// Represents a storage directory that manages a collection of storage values
/// and provides methods for initialization and persistence of data.
/// </summary>
public class StorageDirectory
{
    private volatile bool initialized;
    
    private volatile string name;
    private volatile string path;

    private volatile LogSink log;
    
    private volatile StorageManager manager;
    private volatile ConcurrentDictionary<string, IStorageValue> values = new();

    /// <summary>
    /// The name of this directory.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// The path to this directory.
    /// </summary>
    public string Path => path;

    /// <summary>
    /// The number of values in this directory.
    /// </summary>
    public int ValueCount => values.Count;
    
    /// <summary>
    /// The values in this directory.
    /// </summary>
    public IReadOnlyDictionary<string, IStorageValue> Values => values;
    
    /// <summary>
    /// The log sink associated with this directory.
    /// </summary>
    public LogSink Log => log;

    /// <summary>
    /// The storage manager that manages this directory.
    /// </summary>
    public StorageManager Manager
    {
        get => manager;
        set => manager = value;      
    }
    
    /// <summary>
    /// Creates a new storage directory.
    /// </summary>
    public StorageDirectory(string name, string path)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));
        
        this.name = name;
        this.path = path;

        this.log = LogManager.GetSource("StorageDirectory", name);
    }

    /// <summary>
    /// Attempts to retrieve a value from the storage directory by its name and convert it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="name">The name of the value to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified name, if the key is found
    /// and can be converted to type <typeparamref name="T"/>; otherwise, the default value of <typeparamref name="T"/>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// true if the value associated with the specified name is found and successfully converted to type <typeparamref name="T"/>;
    /// otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided name is null or empty.</exception>
    public bool TryGetValue<T>(string name, out T value)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!values.TryGetValue(name, out var storageValue))
        {
            value = default;
            return false;
        }
        
        value = (storageValue as StorageValue<T>).Value;
        return true;      
    }

    /// <summary>
    /// Attempts to retrieve a storage value of the specified type associated with the given name.
    /// </summary>
    /// <typeparam name="T">The expected type of the storage value.</typeparam>
    /// <param name="name">The name of the storage value to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the storage value of type <typeparamref name="T"/>
    /// if the key is found and the type is compatible; otherwise, the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <c>true</c> if a storage value with the specified name exists and can be cast
    /// to the specified type <typeparamref name="T"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is <c>null</c> or an empty string.
    /// </exception>
    public bool TryGetStorageValue<T>(string name, out StorageValue<T> value)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!values.TryGetValue(name, out var storageValue))
        {
            value = default;
            return false;
        }

        value = storageValue as StorageValue<T>;
        return true;      
    }

    /// <summary>
    /// Retrieves a storage value of the specified type <typeparamref name="T"/>
    /// from the storage directory by the given name.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="name">The name of the value to retrieve from the storage directory.</param>
    /// <returns>The value of type <typeparamref name="T"/> associated with the specified name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no value with the specified <paramref name="name"/> exists in the storage directory.
    /// </exception>
    public T GetValue<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!values.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Value with name &1{name}&r does not exist in directory &1{name}&r.");

        return (value as StorageValue<T>).Value;
    }

    /// <summary>
    /// Retrieves a strongly typed storage value by its name from the storage directory.
    /// </summary>
    /// <typeparam name="T">The type of the storage value to retrieve.</typeparam>
    /// <param name="name">The name of the storage value to retrieve.</param>
    /// <returns>
    /// The <see cref="StorageValue{T}"/> instance associated with the specified name.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="name"/> is null or empty.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when a value with the specified <paramref name="name"/> is not found in the storage directory.
    /// </exception>
    public StorageValue<T> GetStorageValue<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!values.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Value with name &1{name}&r does not exist in directory &1{name}&r.");

        return value as StorageValue<T>;
    }

    /// <summary>
    /// Removes a value from the storage directory.
    /// </summary>
    /// <param name="name">
    /// The name of the value to remove.
    /// </param>
    /// <param name="deleteFile">
    /// A boolean indicating whether to delete the associated file from the filesystem.
    /// </param>
    /// <returns>
    /// A boolean indicating whether the value was successfully removed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided name is null or empty.
    /// </exception>
    public bool RemoveStorageValue(string name, bool deleteFile = false)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!values.TryRemove(name, out var value))
            return false;

        if (deleteFile)
        {
            try
            {
                File.Delete(value.Path);
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to delete file for value &1{name}&r:\n{ex}");
            }
        }

        return true;
    }

    /// <summary>
    /// Adds a new storage value to the directory or retrieves the existing value if one already exists with the given name.
    /// </summary>
    /// <typeparam name="T">The type of the value being added or retrieved.</typeparam>
    /// <param name="name">The name of the storage value to be added or retrieved.</param>
    /// <param name="valueFactory">A factory method to generate the initial value if the storage value needs to be created.</param>
    /// <returns>A <see cref="StorageValue{T}"/> instance representing the added or existing storage value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty, or <paramref name="valueFactory"/> is null.</exception>
    public StorageValue<T> AddStorageValue<T>(string name, Func<T> valueFactory)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (valueFactory == null)
            throw new ArgumentNullException(nameof(valueFactory));

        if (values.TryGetValue(name, out var value))
            return (StorageValue<T>)value;
        
        Log.Debug($"Adding new storage value &1{name}&r ..");

        var newValue = new StorageValue<T>() { armed = true };
        
        newValue.name = name;
        newValue.path = System.IO.Path.Combine(path, $"{name}.dat");

        newValue.Directory = this;
        newValue.Value = valueFactory();
        
        values.TryAdd(name, newValue);
        
        Log.Debug($"Added new storage value &1{name}&r!");
        return newValue;       
    }

    /// <summary>
    /// Clears all values stored in the directory.
    /// </summary>
    /// <param name="deleteFiles">
    /// A boolean indicating whether to delete the associated files from the filesystem before clearing the values.
    /// </param>
    public void ClearValues(bool deleteFiles = false)
    {
        if (deleteFiles)
        {
            foreach (var kvp in values)
            {
                try
                {
                    File.Delete(kvp.Value.Path);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to delete file for value &1{name}&r:\n{ex}");
                }
            }
        }
        
        values.Clear();      
    }

    /// <summary>
    /// Initializes the storage directory by loading storage values from .dat files
    /// in the specified directory path. If the directory has been previously
    /// initialized, an exception is thrown.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the storage directory has already been initialized.
    /// </exception>
    public void Initialize()
    {
        if (initialized)
            throw new InvalidOperationException("Storage directory has already been initialized.");

        if (!Manager.DebugLogs)
            Log.AllowedLogs &= ~LogLevel.Debug;
        
        Log.Debug($"Initializing storage directory &1{name}&r ..");
        
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);       
        
        foreach (var file in Directory.GetFiles(path, "*.dat"))
        {
            try
            {
                Log.Debug($"Loading storage value from file &1{file}&r ..", true);
                
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                var data = File.ReadAllLines(file);

                if (data.Length != 2)
                {
                    Log.Error($"Invalid storage value file &1{file}&r: expected 2 lines, found &1{data.Length}&r.");
                    
                    File.Delete(file);
                    continue;
                }
                
                var type = Type.GetType(data[0], true);
                
                Log.Debug($"Loaded storage value type: &1{type!.FullName}&r");
                Log.Debug($"Value JSON: &1{data[1]}&r");
                
                var value = JsonConvert.DeserializeObject(data[1], type);

                if (value == null)
                {
                    Log.Error($"Failed to deserialize storage value from file &1{file}&r.");
                    continue;
                }
                
                var valueType = typeof(StorageValue<>).MakeGenericType(value.GetType());
                
                Log.Debug($"Storage value type: &1{valueType}&r");
                
                var valueInstance = Activator.CreateInstance(valueType) as IStorageValue;

                if (valueInstance == null)
                {
                    Log.Error($"Failed to create storage value for &1{file}&r: &1{valueType.FullName}&r is not a valid storage value.");
                    continue;
                }
                
                Log.Debug($"Loaded storage value &1{name}&r from &3{file}&r ..");

                valueInstance.Name = name;
                valueInstance.Path = file;
                
                valueInstance.Directory = this;
                valueInstance.SetValue(value);
                
                values.TryAdd(name, valueInstance);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed while loading storage value from file &1{file}&r:\n{ex}");
            }
        }
        
        Log.Debug($"Initialized storage directory &1{name}&r!");      
        
        initialized = true;       
    }

    /// <summary>
    /// Writes all dirty storage values in the directory to their respective file paths
    /// and marks them as not dirty. Logs any errors encountered during the process.
    /// </summary>
    public void SaveDirty()
    {
        foreach (var kvp in values)
        {
            try
            {
                if (!kvp.Value.IsDirty)
                    continue;

                Log.Debug($"Saving dirty value &1{kvp.Key}&r ..");

                var type = kvp.Value.Type;
                var json = kvp.Value.Serialize();
                
                File.WriteAllLines(kvp.Value.Path, 
                [
                    type.AssemblyQualifiedName,
                    json
                ]);

                kvp.Value.IsDirty = false;
                
                Log.Debug($"Saved dirty value &1{kvp.Key}&r to &3{kvp.Value.Path}&r!");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while saving value &1{kvp.Key}&r:\n{ex}");
            }
        }
    }

    /// <summary>
    /// Saves all values in the storage directory to their respective file paths.
    /// </summary>
    /// <remarks>
    /// This method iterates through all stored values in the directory, ensures their dirty flag is reset, serializes
    /// the values using a <c>ByteWriter</c>, and writes the data to the corresponding file paths.
    /// In case of an error during the save operation, it logs the exception details for each value that fails to save.
    /// </remarks>
    public void SaveAll()
    {
        Log.Debug($"Saving all values in directory &1{path}&r ..");      
        
        foreach (var kvp in values)
        {
            try
            {
                kvp.Value.IsDirty = false;
                
                var type = kvp.Value.Type;
                var json = kvp.Value.Serialize();
                
                File.WriteAllLines(kvp.Value.Path, 
                [
                    type.AssemblyQualifiedName,
                    json
                ]);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while saving value &1{kvp.Key}&r:\n{ex}");
            }
        }
    }
}