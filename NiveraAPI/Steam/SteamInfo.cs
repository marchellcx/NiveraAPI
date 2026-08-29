namespace NiveraAPI.Steam;

/// <summary>
/// Represents the public profile information of a Steam user.
/// </summary>
public class SteamInfo
{
    /// <summary>
    /// Represents the unique 64-bit identifier for a Steam user, also known as SteamID64.
    /// This identifier is used to uniquely identify a player's Steam account
    /// across the Steam platform and associated services.
    /// </summary>
    public ulong SteamId = 0;

    /// <summary>
    /// Represents the total number of hours a Steam user has played games in the past two weeks.
    /// This value provides insight into recent gaming activity for the user.
    /// </summary>
    public double HoursPlayedTwoWeeks = 0.0;

    /// <summary>
    /// Represents the current online status of a Steam user.
    /// </summary>
    public volatile SteamOnlineState OnlineState = SteamOnlineState.Offline;
    
    /// <summary>
    /// Represents the current privacy state of a Steam user's profile.
    /// </summary>
    public volatile SteamPrivacyState PrivacyState = SteamPrivacyState.Public;

    /// <summary>
    /// Represents the current trade ban state of a Steam user account.
    /// </summary>
    public volatile SteamTradeBanState TradeBanState = SteamTradeBanState.None;

    /// <summary>
    /// Represents the display name or username of a Steam user.
    /// This name is chosen by the user and may be changed at any time.
    /// It is used for identifying the user within the Steam community and services.
    /// </summary>
    public volatile string Name = string.Empty;

    /// <summary>
    /// Represents the geographical location or region associated with a Steam user's profile.
    /// This information is typically specified by the user and may include their country, city, or region.
    /// </summary>
    public volatile string Location = string.Empty;

    /// <summary>
    /// Represents the URL of a Steam user's avatar image.
    /// This property provides the location of the user's avatar, usually hosted on Steam's servers.
    /// </summary>
    public volatile string AvatarUrl = string.Empty;

    /// <summary>
    /// Represents the full URL of a Steam user's avatar image.
    /// This URL points to the highest resolution version of the avatar
    /// available for the user's profile.
    /// </summary>
    public volatile string AvatarFullUrl = string.Empty;

    /// <summary>
    /// Represents the URL of the medium-sized avatar image for a Steam user.
    /// This URL provides access to the user's profile picture at a medium resolution,
    /// typically used in various UI elements where a balance between detail and performance is needed.
    /// </summary>
    public volatile string AvatarMediumUrl = string.Empty;

    /// <summary>
    /// Represents the name or identifier of the game that the user is currently playing.
    /// This information is typically displayed on the user's profile or status
    /// to indicate their current in-game activity.
    /// </summary>
    public volatile string CurrentGame = string.Empty;
    
    /// <summary>
    /// Represents the status of a Steam user's VAC (Valve Anti-Cheat) ban.
    /// </summary>
    public volatile bool IsVacBanned;
    
    /// <summary>
    /// Represents the status of a Steam user's account.
    /// </summary>
    public volatile bool IsLimitedAccount;
}