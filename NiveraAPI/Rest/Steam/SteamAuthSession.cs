namespace NiveraAPI.Rest.Steam;

/// <summary>
/// Represents a Steam authentication session, including session details,
/// expiration time, and callback functions.
/// </summary>
public class SteamAuthSession
{
    /// <summary>
    /// The unique identifier of the session.
    /// </summary>
    public int Id { get; internal set; } = 0;
    
    /// <summary>
    /// The URL to which the user will be redirected to for authentication.
    /// </summary>
    public string Url { get; internal set; }
    
    /// <summary>
    /// The Steam ID of the user associated with the session.
    /// </summary>
    public string SteamId { get; internal set; } = "";
    
    /// <summary>
    /// The message to display to the user.
    /// </summary>
    public string? Message { get; internal set; }

    /// <summary>
    /// Represents the error status of the Steam authentication session.
    /// </summary>
    public SteamAuthError Error { get; internal set; } = SteamAuthError.None;

    /// <summary>
    /// The time at which the session was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; } = DateTime.Now;

    /// <summary>
    /// The time at which the session will expire.
    /// </summary>
    public DateTime? ExpiresAtUtc { get; internal set; }
    
    /// <summary>
    /// The callback function to be executed when the session expires.
    /// </summary>
    public Action<SteamAuthSession> Callback { get; internal set; }
    
    /// <summary>
    /// Additional data associated with the session.
    /// </summary>
    public object? Data { get; internal set; } = null;
    
    internal bool ShouldRemove()
    {
        if (!ExpiresAtUtc.HasValue)
            return false;

        if (Error != SteamAuthError.None)
            return true;

        if (DateTime.UtcNow >= ExpiresAtUtc.Value)
        {
            Invalidate(SteamAuthError.TimedOut);
            return true;
        }

        return false;
    }

    internal void Validate(string id)
    {
        if (Error != SteamAuthError.None)
            return;
        
        SteamId = id;

        try
        {
            Callback(this);
        }
        catch
        {
            // ignored
        }
    }
    
    internal void Invalidate(SteamAuthError error)
    {
        if (Error != SteamAuthError.None)
            return;
        
        SteamId = string.Empty;

        Error = error;
        
        try
        {
            Callback(this);
        }
        catch
        {
            // ignored
        }
    }
}