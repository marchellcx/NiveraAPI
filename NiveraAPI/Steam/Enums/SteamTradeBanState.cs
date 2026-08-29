namespace NiveraAPI.Steam;

/// <summary>
/// Represents the state of a trade ban.
/// </summary>
public enum SteamTradeBanState
{
    /// <summary>
    /// No trade ban.
    /// </summary>
    None,

    /// <summary>
    /// Temporary trade restriction / probationary period.
    /// </summary>
    Probation,
    
    /// <summary>
    /// Permanent (or long-term) trade ban.
    /// </summary>
    Banned
}