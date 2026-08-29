using Newtonsoft.Json;

namespace NiveraAPI.Discord.Embeds;

/// <summary>
/// Represents the author section of a Discord embed. This class includes information
/// such as the author's name, URL, icon URL, and proxied icon URL.
/// </summary>
public struct DiscordEmbedAuthor
{
    /// <summary>
    /// Gets or sets the name of the author displayed in the embed.
    /// </summary>
    [JsonProperty("name")] 
    public string Name;

    /// <summary>
    /// Gets or sets the URL associated with the author displayed in the embed.
    /// </summary>
    [JsonProperty("url")] 
    public string Url;

    /// <summary>
    /// Gets or sets the URL of the icon associated with the author displayed in the embed.
    /// </summary>
    [JsonProperty("icon_url")] 
    public string IconUrl;

    /// <summary>
    /// Gets or sets the proxied URL of the icon associated with the author displayed in the embed.
    /// </summary>
    [JsonProperty("proxy_icon_url")] 
    public string ProxyIconUrl;

    /// <summary>
    /// Sets the name of the author displayed in the embed and returns the updated <see cref="DiscordEmbedAuthor"/> instance.
    /// </summary>
    /// <param name="name">The name to be displayed as the author in the embed.</param>
    /// <returns>The updated <see cref="DiscordEmbedAuthor"/> instance.</returns>
    public DiscordEmbedAuthor WithName(string name)
    {
        Name = name;
        return this;
    }

    /// <summary>
    /// Sets the URL associated with the author displayed in the embed and returns the updated <see cref="DiscordEmbedAuthor"/> instance.
    /// </summary>
    /// <param name="url">The URL to be associated with the author in the embed.</param>
    /// <returns>The updated <see cref="DiscordEmbedAuthor"/> instance.</returns>
    public DiscordEmbedAuthor WithUrl(string url)
    {
        Url = url;
        return this;
    }

    /// <summary>
    /// Sets the URL of the icon associated with the author displayed in the embed,
    /// optionally setting a proxied URL for the icon, and returns the updated <see cref="DiscordEmbedAuthor"/> instance.
    /// </summary>
    /// <param name="iconUrl">The URL of the icon to be associated with the author in the embed.</param>
    /// <param name="proxyIconUrl">The proxied URL of the icon to be associated with the author in the embed. This parameter is optional.</param>
    /// <returns>The updated <see cref="DiscordEmbedAuthor"/> instance.</returns>
    public DiscordEmbedAuthor WithIcon(string iconUrl, string proxyIconUrl = "")
    {
        IconUrl = iconUrl;
        ProxyIconUrl = proxyIconUrl;
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DiscordEmbedAuthor"/> with the specified properties.
    /// </summary>
    /// <param name="name">The name to be displayed as the author in the embed.</param>
    /// <param name="url">The URL associated with the author in the embed. Optional.</param>
    /// <param name="iconUrl">The URL of the icon associated with the author in the embed. Optional.</param>
    /// <param name="proxyUrl">The proxied URL of the icon associated with the author in the embed. Optional.</param>
    /// <returns>A new instance of <see cref="DiscordEmbedAuthor"/> with the specified properties.</returns>
    public static DiscordEmbedAuthor Create(string name, string url = "", string iconUrl = "", string proxyUrl = "")
    {
        var result = new DiscordEmbedAuthor();

        result.Name = name;
        result.Url = url;

        result.IconUrl = iconUrl;
        result.ProxyIconUrl = proxyUrl;

        return result;
    }
}