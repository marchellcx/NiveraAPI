namespace NiveraAPI.Discord;

/// <summary>
/// Represents the ID and token extracted from a Discord webhook URL.
/// </summary>
public struct WebhookCredentials
{
    /// <summary>
    /// The ID of the webhook.
    /// </summary>
    public ulong Id;

    /// <summary>
    /// The URL of the webhook.
    /// </summary>
    public string Url;

    /// <summary>
    /// The token of the webhook.
    /// </summary>
    public string Token;

    /// <summary>
    /// Creates a new instance of the WebhookCredentials struct.
    /// </summary>
    public WebhookCredentials(ulong id, string url, string token)
    {
        Id = id;
        Url = url;
        Token = token;
    }
}