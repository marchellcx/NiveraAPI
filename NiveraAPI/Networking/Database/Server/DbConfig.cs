using NiveraAPI.Networking.Database.Enums;
using NiveraAPI.Services;

namespace NiveraAPI.Networking.Database.Server;

/// <summary>
/// Represents the configuration for a database server.
/// </summary>
public class DbConfig : Service
{
    /// <summary>
    /// Represents all permissions for a database user.
    /// </summary>
    public const DbPerms AllPermissions = 
        DbPerms.AccessItem | DbPerms.AddNewItem | DbPerms.UpdateExistingItem | DbPerms.RemoveItem |
        DbPerms.DeleteTable | DbPerms.ClearTable | DbPerms.CreateTable | DbPerms.ClearAllTables;
    
    /// <summary>
    /// Gets or sets a value indicating whether the database is protected.
    /// </summary>
    public bool IsProtected { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the directory of the database.
    /// </summary>
    public string Directory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the password for the database.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of users in the database.
    /// </summary>
    public List<string> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of users with sudo permissions.
    /// </summary>
    public List<string> SudoUsers { get; set; } = new();

    /// <summary>
    /// Gets or sets the default permissions for new users.
    /// </summary>
    public DbPerms DefaultPermissions { get; set; } = DbPerms.None;

    /// <summary>
    /// Gets or sets the permissions for each user.
    /// </summary>
    public Dictionary<string, DbPerms> Permissions { get; set; } = new();

    /// <summary>
    /// Validates if the provided user and password combination is valid for the server configuration.
    /// </summary>
    /// <param name="user">The username to validate.</param>
    /// <param name="password">The password corresponding to the username to validate.</param>
    /// <returns>True if the user is valid or if the server is not protected; otherwise, false.</returns>
    public bool IsValidUser(string user, string password)
    {
        if (!IsProtected)
            return true;

        return Users.Contains(user) && Password == password;
    }

    /// <summary>
    /// Determines whether a specified user has a particular database permission.
    /// </summary>
    /// <param name="user">The username to check permissions for.</param>
    /// <param name="permission">The specific database permission to check.</param>
    /// <returns>True if the user has the specified permission; otherwise, false.</returns>
    public bool HasPermission(string user, DbPerms permission)
    {
        var permissions = GetPermissions(user);
        
        if ((permissions & permission) == permission)
            return true;
        
        return SudoUsers.Contains(user);
    }

    /// <summary>
    /// Retrieves the database permissions associated with a specified user.
    /// </summary>
    /// <param name="user">The username for which to retrieve permissions.</param>
    /// <returns>The database permissions assigned to the user. If the user has no specific permissions, the default permissions are returned.</returns>
    public DbPerms GetPermissions(string user)
    {
        if (SudoUsers.Contains(user))
            return AllPermissions;
        
        return Permissions.TryGetValue(user, out var permissions) ? permissions : DefaultPermissions;
    }
}