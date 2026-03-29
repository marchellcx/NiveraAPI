using NiveraAPI.Services;

namespace NiveraAPI.Networking.Database.Client;

/// <summary>
/// Represents the configuration for a database client.
/// </summary>
public class DbConfig : Service
{
    /// <summary>
    /// The name of the database user
    /// </summary>
    public string User { get; set; } = string.Empty;
    
    /// <summary>
    /// The password of the database user
    /// </summary>
    public string Password { get; set; } = string.Empty;
}