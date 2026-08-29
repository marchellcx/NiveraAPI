namespace NiveraAPI.Steam;

/// <summary>
/// Represents the online state of a user on Steam.
/// </summary>
public enum SteamOnlineState
{
    /// <summary>
    /// The user is online.
    /// </summary>
    Online,
    
    /// <summary>
    /// The user is offline.
    /// </summary>
    Offline,
    
    /// <summary>
    /// The user is in a game.
    /// </summary>
    InGame
}