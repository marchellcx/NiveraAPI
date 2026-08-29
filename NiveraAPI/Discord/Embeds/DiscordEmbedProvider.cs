using Newtonsoft.Json;

namespace NiveraAPI.Discord.Embeds;

/// <summary>
/// Represents an embed provider in a Discord message, which typically contains information
/// about the source or service of the embed content and metadata.
/// </summary>
public struct DiscordEmbedProvider
{
    /// <summary>
    /// The name of the embed provider, typically representing the source or service
    /// responsible for the embed content.
    /// </summary>
    [JsonProperty("name")] 
    public string Name;

    /// <summary>
    /// The URL of the embed provider, pointing to the source or service
    /// responsible for the embed content.
    /// </summary>
    [JsonProperty("url")] 
    public string Url;

    /// <summary>
    /// Creates a new instance of <see cref="DiscordEmbedProvider"/> with the specified name and URL.
    /// </summary>
    /// <param name="name">The name of the embed provider, typically indicating the source or service responsible for the embed content.</param>
    /// <param name="url">The URL of the embed provider, pointing to the source or service responsible for the embed content.</param>
    /// <returns>A new <see cref="DiscordEmbedProvider"/> instance initialized with the specified name and URL.</returns>
    public static DiscordEmbedProvider Create(string name, string url)
    {
        var result = new DiscordEmbedProvider();

        result.Name = name;
        result.Url = url;

        return result;
    }
}