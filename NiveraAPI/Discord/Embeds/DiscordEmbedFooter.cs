using Newtonsoft.Json;

namespace NiveraAPI.Discord.Embeds;

/// <summary>
/// Represents the footer section of a Discord embed, which includes text content
/// and optionally an icon URL for visual enhancement.
/// </summary>
public struct DiscordEmbedFooter
{
    /// <summary>
    /// The text content of the embed footer. This is the main displayed information
    /// within the footer of a Discord embed.
    /// </summary>
    [JsonProperty("text")]
    public string Text;

    /// <summary>
    /// The URL of the icon displayed in the embed footer. This can be used to visually
    /// enhance the footer with a small image or icon.
    /// </summary>
    [JsonProperty("icon_url")]
    public string IconUrl;

    /// <summary>
    /// The proxied URL of the icon displayed in the embed footer. This is typically
    /// used when the icon image is hosted on a platform that generates a proxy URL
    /// for accessing the file securely or more efficiently.
    /// </summary>
    [JsonProperty("proxy_icon_url")]
    public string ProxyIconUrl;

    /// <summary>
    /// Sets the text content of the embed footer and returns the updated instance.
    /// </summary>
    /// <param name="text">The text to set as the content of the embed footer.</param>
    /// <returns>The current <see cref="DiscordEmbedFooter"/> instance with the updated text.</returns>
    public DiscordEmbedFooter WithText(string text)
    {
        Text = text;
        return this;
    }

    /// <summary>
    /// Sets the icon URL and optional proxied icon URL for the embed footer,
    /// and returns the updated instance.
    /// </summary>
    /// <param name="iconUrl">The URL of the icon to display in the embed footer.</param>
    /// <param name="proxyIconUrl">The proxied URL of the icon, if any, to display in the embed footer.</param>
    /// <returns>The current <see cref="DiscordEmbedFooter"/> instance with the updated icon URLs.</returns>
    public DiscordEmbedFooter WithIcon(string iconUrl, string proxyIconUrl = "")
    {
        IconUrl = iconUrl;
        ProxyIconUrl = proxyIconUrl;
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DiscordEmbedFooter"/> with the specified text and optional icon URLs.
    /// </summary>
    /// <param name="text">The text content to set for the embed footer.</param>
    /// <param name="icon">The URL of the icon to display in the embed footer. Optional parameter.</param>
    /// <param name="proxyIcon">The proxied URL of the icon to display in the embed footer. Optional parameter.</param>
    /// <returns>A new <see cref="DiscordEmbedFooter"/> instance initialized with the given parameters.</returns>
    public static DiscordEmbedFooter Create(string text, string icon = "", string proxyIcon = "")
    {
        var result = new DiscordEmbedFooter();

        result.Text = text;
        result.IconUrl = icon;
        result.ProxyIconUrl = proxyIcon;

        return result;
    }
}