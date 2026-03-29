using Newtonsoft.Json;

using NiveraAPI.Logs;
using NiveraAPI.Utilities;

using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Binary;

namespace NiveraAPI.IO.Storage;

/// <summary>
/// Provides functionality for managing persistent file-based storage.
/// </summary>
public class FileStorage : IDisposable
{
    /// <summary>
    /// The current version of the file storage format.
    /// </summary>
    public const byte Version = 1;
    
    internal static LogSink log = LogManager.GetSource("IO", "FileStorage");
    
    private List<FileObject> files = new();
    
    /// <summary>
    /// Whether or not to use type code for serialization.
    /// </summary>
    public bool UseTypeCode { get; set; }
    
    /// <summary>
    /// The directory where files are stored.
    /// </summary>
    public string Directory { get; set; } = System.IO.Directory.GetCurrentDirectory();

    /// <summary>
    /// The format of contained files.
    /// </summary>
    public FileFormat Format { get; set; } = FileFormat.Binary;
    
    /// <summary>
    /// The list of contained files.
    /// </summary>
    public IReadOnlyList<FileObject> Files => files;

    /// <summary>
    /// Removes the file object with the specified name from the storage, with an option to delete any associated files.
    /// </summary>
    /// <param name="name">The name of the file object to be removed from the storage.</param>
    /// <param name="deleteFiles">A boolean value indicating whether to delete files associated with the file object.
    /// If true, all files linked to the object are deleted; otherwise, only the object is removed from storage.</param>
    /// <returns>
    /// A boolean value indicating whether the file object was successfully removed.
    /// Returns true if the operation succeeds; otherwise, false.
    /// </returns>
    public bool Remove(string name, bool deleteFiles = false)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var path = Path.Combine(Directory, name);
        var obj = files.Find(file => file.Directory == path);
        
        if (obj == null)
            return false;
        
        return Remove(obj, deleteFiles);
    }

    /// <summary>
    /// Removes the specified file object from the storage, with an option to delete any associated files.
    /// </summary>
    /// <param name="file">The <see cref="FileObject"/> to be removed from the storage.</param>
    /// <param name="deleteFiles">A boolean value indicating whether to delete files associated with the file object.
    /// If true, all files linked to the object are deleted; otherwise, only the object is removed from storage.</param>
    /// <returns>
    /// A boolean value indicating whether the file object was successfully removed.
    /// Returns true if the operation succeeds, otherwise false.
    /// </returns>
    public bool Remove(FileObject file, bool deleteFiles = false)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        
        if (!files.Remove(file))
            return false;
        
        file.RemoveAll(deleteFiles);
        return true;
    }

    /// <summary>
    /// Creates a new file object with the specified name, or retrieves an existing one if it already exists in the directory.
    /// </summary>
    /// <param name="name">The name of the file object to create or retrieve.</param>
    /// <returns>
    /// The newly created file object, or an existing file object if one with the specified name already exists in the directory.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="name"/> is null or empty.
    /// </exception>
    public FileObject Create(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        var path = Path.Combine(Directory, name);
        var active = files.Find(obj => obj.Directory == path);
        
        if (active != null)
            return active;

        active = new();

        active.Storage = this;
        active.Directory = path;
        
        active.ReadAll();
        
        files.Add(active);
        return active;
    }

    /// <summary>
    /// Saves file objects in the internal file list that are marked as dirty.
    /// Only the file objects requiring updates are processed, optimizing
    /// the save operation for efficiency.
    /// </summary>
    /// <remarks>
    /// If an error occurs while saving a specific dirty file object, the error
    /// is logged, and the method proceeds to the remaining dirty file objects.
    /// </remarks>
    public void SaveDirty()
    {
        for (var x = 0; x < files.Count; x++)
        {
            try
            {
                files[x].SaveDirty();
            }
            catch (Exception ex)
            {
                log.Error($"Could not update file &1{files[x].Directory}&r:\n{ex}");
            }
        }
    }
    
    /// <summary>
    /// Saves all file objects contained in the internal file list.
    /// Each file object invokes its own save operation, ensuring
    /// the persistence of associated data to the storage system.
    /// </summary>
    /// <remarks>
    /// If an error occurs while saving an individual file object,
    /// the error is logged, and the process continues for the
    /// remaining file objects.
    /// </remarks>
    public void SaveAll()
    {
        for (var x = 0; x < files.Count; x++)
        {
            try
            {
                files[x].SaveAll();
            }
            catch (Exception ex)
            {
                log.Error($"Could not save directory &1{files[x].Directory}&r:\n{ex}");
            }
        }
    }

    /// <summary>
    /// Reads all file objects from the directories within the specified storage directory.
    /// Populates the internal file list with discovered file objects and invokes their
    /// individual read operations.
    /// </summary>
    /// <remarks>
    /// If the storage directory does not exist, it is created and the process terminates without
    /// attempting to read file objects. Errors encountered while processing individual directories
    /// are logged, and the operation continues for the remaining directories.
    /// </remarks>
    public void ReadAll()
    {
        RemoveAll(false);
        
        if (!System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.CreateDirectory(Directory);
            return;
        }
        
        var directories = System.IO.Directory.GetDirectories(Directory);

        for (var x = 0; x < directories.Length; x++)
        {
            try
            {
                var directory = directories[x];
                var obj = new FileObject();

                obj.Storage = this;
                obj.Directory = directory;

                obj.ReadAll();
                
                files.Add(obj);
            }
            catch (Exception ex)
            {
                log.Error($"Could not read directory &1{directories[x]}&r:\n{ex}");
            }
        }
    }
    
    /// <summary>
    /// Deletes all files from the storage. Ensures that the internal file list
    /// is cleared and attempts to remove each file in the process.
    /// </summary>
    /// <remarks>
    /// Any errors encountered during the deletion of individual files are logged,
    /// but the process continues for other files in the list.
    /// </remarks>
    public void DeleteAll()
    {
        for (var x = 0; x < files.Count; x++)
        {
            try
            {
                files[x].DeleteAll();
            }
            catch (Exception ex)
            {
                log.Error($"Could not remove file &1{files[x].Directory}&r:\n{ex}");
            }
        }
        
        files.Clear();
    }

    /// <summary>
    /// Removes all files from the storage, with an option to delete the physical files from the file system.
    /// </summary>
    /// <param name="deleteFiles">If true, the physical files associated with each file object will also be deleted from the file system.
    /// efaults to false.</param>
    public void RemoveAll(bool deleteFiles = false)
    {
        for (var x = 0; x < files.Count; x++)
        {
            try
            {
                files[x].RemoveAll(deleteFiles);
            }
            catch (Exception ex)
            {
                log.Error($"Could not remove file &1{files[x].Directory}&r:\n{ex}");
            }
        }
        
        files.Clear();
    }
    
    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        RemoveAll(false);
        
        files.Clear();
        
        log.Info("File storage disposed.");
    }

    /// <summary>
    /// Serializes the given object into a byte array using the specified file format.
    /// </summary>
    /// <param name="obj">The object to serialize. Cannot be null.</param>
    /// <param name="useTypeCode">Whether to use type code for serialization. Defaults to false.</param>
    /// <param name="format">The file format to use for serialization. Defaults to <see cref="FileFormat.Binary"/>.</param>
    /// <returns>A byte array representing the serialized object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided object is null.</exception>
    public static byte[] Serialize(object obj, bool useTypeCode = false, FileFormat format = FileFormat.Binary)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        using (var writer = ByteWriter.Get())
        {
            writer.WriteByte(Version);
            
            if (useTypeCode)
            {
                writer.WriteByte(0);
                writer.WriteByte((byte)format);
                writer.WriteInt32(obj.GetType().GenerateLibraryCode());
            }
            else
            {
                writer.WriteByte(1);
                writer.WriteByte((byte)format);
                writer.WriteString(obj.GetType().AssemblyQualifiedName!);
            }
            
            switch (format)
            {
                case FileFormat.JsonIndented:
                    writer.WriteString(JsonConvert.SerializeObject(obj, Formatting.Indented));
                    break;
                
                case FileFormat.JsonNotIndented:
                    writer.WriteString(JsonConvert.SerializeObject(obj, Formatting.None));
                    break;
                
                default:
                    BinarySerializer.Serialize(obj, writer);
                    break;
            }

            return writer.ToArray();
        }
    }

    /// <summary>
    /// Deserializes the given byte array into an object using the specified file format.
    /// </summary>
    /// <param name="data">The byte array containing the serialized object data. Cannot be null.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided byte array is null.</exception>
    public static object? Deserialize(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.Length < 7) // header + min type size
            return null;
        
        using (var reader = ByteReader.Get(data, 0, data.Length))
        {
            try
            {
                var versionHeader = reader.ReadByte();

                if (versionHeader != Version)
                {
                    log.Error($"Unsupported version header: &1{versionHeader}&r");
                    return null;
                }
                
                var codeHeader = reader.ReadByte();
                var formatHeader = (FileFormat)reader.ReadByte();
                
                var type = default(Type);

                switch (codeHeader)
                {
                    case 0: // Short Type Code, less reliable
                    {
                        type = TypeCodeLibrary.GetLibraryType(reader.ReadInt32());

                        if (type == null)
                            log.Error($"Could not resolve type by code &1{reader.ReadInt32()}&r");

                        break;
                    }

                    case 1:
                    {
                        var typeName = reader.ReadString();

                        type = Type.GetType(typeName, false);

                        if (type == null && !ReflectionHelper.TryFindType(typeName, false, out type))
                            log.Error($"Could not resolve type by name &1{typeName}&r");

                        break;
                    }

                    default:
                        log.Error($"Invalid type header: &1{codeHeader}&r");
                        return null;
                }

                if (type == null)
                {
                    log.Error($"Could not find type for header &1{codeHeader}&r");
                    return null;
                }

                switch (formatHeader)
                {
                    case FileFormat.Binary:
                        return BinarySerializer.Deserialize(type, reader);
                    
                    case FileFormat.JsonIndented or 
                         FileFormat.JsonNotIndented:
                        return JsonConvert.DeserializeObject(reader.ReadString(), type);
                    
                    default:
                        log.Error($"Invalid format header: &1{formatHeader}&r");
                        return null;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Could not deserialize object:\n&1{ex}&r");
                return null;
            }
        }
    }
}