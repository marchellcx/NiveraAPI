namespace NiveraAPI.IO.Storage;

/// <summary>
/// Represents a file object that stores properties and manages their persistence.
/// </summary>
public class FileObject
{
    private List<FileProperty> properties = new(20);

    /// <summary>
    /// Gets the path to the directory of this object.
    /// </summary>
    public string Directory { get; internal set; }

    /// <summary>
    /// Gets the parent storage of this object.
    /// </summary>
    public FileStorage Storage { get; internal set; }
    
    /// <summary>
    /// Gets a list of all properties registered to the file.
    /// </summary>
    public IReadOnlyList<FileProperty> Properties => properties;

    /// <summary>
    /// Removes a file property by its name from the current FileObject.
    /// </summary>
    /// <remarks>
    /// If a file property with the specified name exists, it removes the property from the internal list.
    /// Optionally, the associated file can also be deleted from the file system.
    /// </remarks>
    /// <param name="name">The name of the property to be removed. Cannot be null or empty.</param>
    /// <param name="deleteFile">
    /// A flag indicating whether the associated file should be deleted when the property is removed.
    /// Defaults to false.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the property was successfully removed.
    /// Returns true if the property was removed; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public bool Remove(string name, bool deleteFile = false)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        var prop = properties.Find(x => x.Name == name);

        if (prop != null && properties.Remove(prop))
        {
            prop.Destroy();
            
            if (deleteFile)
                File.Delete(prop.File);
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Adds a new file property to the current FileObject or retrieves an existing one by name.
    /// </summary>
    /// <remarks>
    /// If a property with the specified name already exists, it returns the existing property.
    /// If no property with the specified name exists, a new property is created, initialized with the provided default value,
    /// and added to the internal list of properties. The property's state is serialized and saved to disk.
    /// </remarks>
    /// <typeparam name="T">The type of the property's value.</typeparam>
    /// <param name="name">The name of the property to be added or retrieved. Cannot be null or empty.</param>
    /// <param name="defaultValue">The default value assigned to the property if it is being created. Optional.</param>
    /// <returns>The added or existing instance of <see cref="FileProperty{T}"/> corresponding to the specified name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public FileProperty<T> Add<T>(string name, T defaultValue = default)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        var prop = properties.Find(x => x.Name == name);

        if (prop != null)
            return (FileProperty<T>)prop;

        prop = new FileProperty<T>();
        prop.value = defaultValue;
        
        prop.File = Path.Combine(Directory, $"{name}.dat");
        prop.Name = name;
        prop.Object = this;

        if (File.Exists(prop.File))
            prop.Read();
        else
            prop.Write();
        
        properties.Add(prop);
        return (FileProperty<T>)prop;   
    }

    /// <summary>
    /// Removes all file properties associated with the current FileObject, with an option to delete linked files.
    /// </summary>
    /// <remarks>
    /// This method clears the internal list of <see cref="FileProperty"/> instances.
    /// If the <paramref name="deleteFiles"/> parameter is set to true, the <see cref="DeleteAll"/> method is invoked to
    /// delete all associated files and recreate the directory specified by the <see cref="Directory"/> property.
    /// If set to false, only the properties are destroyed and removed from the internal list.
    /// </remarks>
    /// <param name="deleteFiles">A boolean value indicating whether to delete the files associated with the properties.
    /// Passing true deletes all files and clears the directory, while false only removes properties.</param>
    public void RemoveAll(bool deleteFiles = true)
    {
        if (deleteFiles)
        {
            DeleteAll();
            return;
        }
        
        properties.ForEach(p => p.Destroy());
        properties.Clear();
    }

    /// <summary>
    /// Deletes all files and properties associated with the current FileObject.
    /// </summary>
    /// <remarks>
    /// This method attempts to delete all <see cref="FileProperty"/> instances stored within the FileObject
    /// and clears the internal list of properties. It also deletes and recreates the directory specified
    /// by the <see cref="Directory"/> property.
    /// If any exception occurs during the deletion process, an error is logged using the
    /// FileStorage logger instance.
    /// </remarks>
    public void DeleteAll()
    {
        try
        {
            properties.ForEach(p => p.Destroy());
            properties.Clear();

            System.IO.Directory.Delete(Directory, true);
        }
        catch (Exception ex)
        {
            FileStorage.log.Error($"Failed to delete file object &3{Directory}&r:\n{ex}");
        }
    }

    /// <summary>
    /// Saves all modified file properties associated with the current FileObject to their corresponding storage locations.
    /// </summary>
    /// <remarks>
    /// This method iterates through the list of <see cref="FileProperty"/> instances and processes only those marked as dirty.
    /// During the first save operation, it ensures that the directory specified by the <see cref="Directory"/> property exists,
    /// creating it if necessary. Once a dirty property is saved, its dirty flag is reset to indicate it has been persisted.
    /// </remarks>
    public void SaveDirty()
    {
        var dirChecked = false;

        for (var x = 0; x < properties.Count; x++)
        {
            var property = properties[x];
            
            if (!property.dirty)
                continue;

            if (!dirChecked)
            {
                if (!System.IO.Directory.Exists(Directory))
                    System.IO.Directory.CreateDirectory(Directory);
                
                dirChecked = true;
            }
            
            property.dirty = false;
            property.Write();
        }
    }

    /// <summary>
    /// Saves all file properties associated with the current FileObject to disk, creating the directory if it does not exist.
    /// </summary>
    /// <remarks>
    /// This method iterates through all <see cref="FileProperty"/> instances in the FileObject and writes them to disk
    /// using their respective <see cref="FileProperty.Write"/> method. The <c>dirty</c> flag of each file property
    /// is also reset to false after writing. If the directory specified by <see cref="Directory"/> does not exist,
    /// it is created before saving the properties.
    /// </remarks>
    public void SaveAll()
    {
        if (!System.IO.Directory.Exists(Directory))
            System.IO.Directory.CreateDirectory(Directory);
        
        for (var x = 0; x < properties.Count; x++)
        {
            var property = properties[x];

            property.dirty = false;
            property.Write();
        }
    }

    /// <summary>
    /// Reads and loads all properties from the directory associated with the current FileObject.
    /// </summary>
    /// <remarks>
    /// This method processes all files with a ".dat" extension within the directory specified by the
    /// <see cref="Directory"/> property. It attempts to read and deserialize each file into a corresponding
    /// <see cref="FileProperty"/> instance. Valid properties are then added to the internal list of properties.
    /// Empty or invalid files are deleted, and deserialization errors are logged using the FileStorage logger.
    /// If the directory does not exist, it is created and the method exits without processing any files.
    /// </remarks>
    public void ReadAll()
    {
        properties.ForEach(p => p.Destroy());
        properties.Clear();
        
        if (!System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.CreateDirectory(Directory);
            return;
        }
        
        foreach (var file in System.IO.Directory.GetFiles(Directory, ".dat"))
        {
            try
            {
                var data = File.ReadAllBytes(file);
                var name = Path.GetFileNameWithoutExtension(file);
                
                if (data.Length < 1)
                {
                    FileStorage.log.Warn($"File &3{file}&r is empty, deleting ..");
                    File.Delete(file);
                    continue;
                }
                
                var obj = FileStorage.Deserialize(data);

                if (obj == null)
                {
                    FileStorage.log.Error($"Could not deserialize file &3{file}&r, deleting ..");
                    File.Delete(file);
                    continue;
                }
                
                var propertyType = typeof(FileProperty<>).MakeGenericType(obj.GetType());
                var property = Activator.CreateInstance(propertyType, obj, name) as FileProperty;

                if (property == null)
                {
                    FileStorage.log.Error($"Could not create property for file &3{file}&r!");
                    continue;
                }
                
                property.SetValue(obj);
                
                property.Raw = data;
                property.Name = name;
                property.File = file;
                property.Object = this;
                
                properties.Add(property);
                
                FileStorage.log.Info($"Loaded file &3{file}&r");
            }
            catch (Exception ex)
            {
                FileStorage.log.Error($"Could not load file &3{file}&r:\n{ex}");
            }
        }
    }
}