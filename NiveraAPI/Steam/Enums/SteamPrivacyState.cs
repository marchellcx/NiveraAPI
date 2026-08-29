namespace NiveraAPI.Steam;

/// <summary>
/// Represents the privacy state of a Steam profile.
/// </summary>
public enum SteamPrivacyState
{
    /// <summary>
    /// Public account.
    /// </summary>
    Public,
    
    /// <summary>
    /// Only friends can view.
    /// </summary>
    FriendsOnly,
    
    /// <summary>
    /// Private account.
    /// </summary>
    Private
}