namespace NiveraAPI.Rest.Steam;

/// <summary>
/// Represents potential errors that can occur during Steam authentication.
/// </summary>
public enum SteamAuthError
{
    /// <summary>
    /// No errors have occured.
    /// </summary>
    None,
    
    /// <summary>
    /// The request timed out.
    /// </summary>
    TimedOut,
    
    /// <summary>
    /// The request was cancelled.
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Server has been closed.
    /// </summary>
    ServerClosed
}