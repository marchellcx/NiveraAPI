using NiveraAPI.Extensions;
using NiveraAPI.Logs;

namespace NiveraAPI.Networking.Database.Server;

/// <summary>
/// Represents a file within the database system, managing the reading and saving
/// of database manifests and their related data.
/// </summary>
public class DbFile : IDisposable
{
    private readonly LogSink log = LogManager.GetSource("Database", "DbFile");
    private readonly Dictionary<string, DbTable> tables = new();
    
    /// <summary>
    /// The directory of the database file.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// The tables contained within the database file.
    /// </summary>
    public IReadOnlyDictionary<string, DbTable> Tables => tables;

    /// <summary>
    /// Creates a new instance of the <see cref="DbFile"/> class.
    /// </summary>
    /// <param name="directory">The directory of the database file.</param>
    /// <exception cref="ArgumentNullException">Thrown if the directory is null or empty.</exception>
    public DbFile(string directory)
    {
        if (string.IsNullOrEmpty(directory))
            throw new ArgumentNullException(nameof(directory));
        
        Directory = directory;
    }

    /// <summary>
    /// Removes all tables from the database, disposes their resources, deletes the current database directory,
    /// and recreates a clean directory structure for the database.
    /// </summary>
    /// <exception cref="IOException">Thrown if an error occurs while deleting or creating the database directory.</exception>
    public void RemoveTables()
    {
        tables.ForEach(kvp => kvp.Value.Dispose());
        tables.Clear();
        
        System.IO.Directory.Delete(Directory, true);
        System.IO.Directory.CreateDirectory(Directory);
    }

    /// <summary>
    /// Removes the specified table from the database file.
    /// </summary>
    /// <param name="name">The name of the table to be removed.</param>
    /// <returns>
    /// Returns <c>true</c> if the table was successfully removed; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the name is null or empty.</exception>
    public bool RemoveTable(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!tables.TryGetValue(name, out var table))
            return false;
        
        table.Dispose();
        
        tables.Remove(name);
        
        System.IO.Directory.Delete(table.Path, true);
        return true;
    }

    /// <summary>
    /// Adds a new table to the database file or retrieves an existing one.
    /// </summary>
    /// <param name="name">The name of the table to be added or retrieved.</param>
    /// <returns>A <see cref="DbTable"/> instance representing the added or existing table.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="name"/> is null or empty.</exception>
    public DbTable? AddTable(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (tables.TryGetValue(name, out var table))
            return table;
        
        var path = Path.Combine(Directory, $"DB-{name}");
        
        if (System.IO.Directory.Exists(path))
            System.IO.Directory.Delete(path, true);
        
        System.IO.Directory.CreateDirectory(path);
        
        table = new DbTable(path);
        
        tables.Add(name, table);
        return table;
    }

    /// <summary>
    /// Retrieves the table with the specified name from the database file.
    /// </summary>
    /// <param name="name">The name of the table to retrieve.</param>
    /// <returns>
    /// The <see cref="DbTable"/> instance if the table exists; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the name is null or empty.</exception>
    public DbTable? GetTable(string name)
        => tables.TryGetValue(name, out var table) ? table : null;

    /// <summary>
    /// Reads all tables in the database file.
    /// </summary>
    public void ReadAll()
    {
        log.Debug($"Reading all tables in {Directory}");
        
        if (!System.IO.Directory.Exists(Directory))
        {
            log.Debug($"Directory {Directory} does not exist, creating ...");
            
            System.IO.Directory.CreateDirectory(Directory);
            return;
        }
        
        log.Debug($"Clearing existing tables ...");
        
        tables.ForEach(kvp => kvp.Value.Dispose());
        tables.Clear();
        
        log.Debug($"Reading tables ...");

        foreach (var directory in System.IO.Directory.GetDirectories(Directory, "DB-*"))
        {
            log.Debug($"Reading table {directory}");
            
            var table = new DbTable(directory);
            
            log.Debug($"Table {table.Name} read");

            try
            {
                table.ReadItems();
                
                tables.Add(table.Name, table);
            }
            catch (Exception ex)
            {
                log.Error($"Could not read table {table.Name}:\n{ex}");
            }
        }
    }

    /// <summary>
    /// Saves all tables in the database file.
    /// </summary>
    public void SaveAll()
    {
        log.Debug($"Saving all tables in {Directory}");
        
        if (!System.IO.Directory.Exists(Directory))
            System.IO.Directory.CreateDirectory(Directory);

        tables.ForEach(kvp => kvp.Value.WriteItems());
        
        log.Debug($"All tables saved");
    }
    
    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        tables.ForEach(kvp => kvp.Value.Dispose());
        tables.Clear();
    }
}