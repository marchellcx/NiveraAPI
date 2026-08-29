using Newtonsoft.Json;

namespace NiveraAPI.Discord.Embeds;

/// <summary>
/// Represents a field in a Discord embed. Each field contains a name (title), a value (content),
/// and an indication of whether the field should be displayed inline with other fields.
/// </summary>
public struct DiscordEmbedField
{
    /// <summary>
    /// Gets or sets the name of the embed field. This is typically displayed as the title or header of the field in a Discord embed.
    /// </summary>
    [JsonProperty("name")] 
    public volatile string Name;

    /// <summary>
    /// Gets or sets the content of the embed field. This is typically displayed as the text or body of the field in a Discord embed.
    /// </summary>
    [JsonProperty("value")] 
    public volatile string Value;

    /// <summary>
    /// Gets or sets a value indicating whether the embed field is displayed inline with other fields.
    /// When set to true, the field appears alongside other inline fields in a Discord embed layout.
    /// Otherwise, it is displayed on its own row.
    /// </summary>
    [JsonProperty("inline")] 
    public volatile bool IsInline;

    /// <summary>
    /// Sets the name of the embed field and returns the current instance of <see cref="DiscordEmbedField"/>.
    /// </summary>
    /// <param name="name">The name to set for the embed field. This is typically displayed as the header or title of the field in a Discord embed.</param>
    /// <returns>The current instance of <see cref="DiscordEmbedField"/> with the updated name.</returns>
    public DiscordEmbedField WithName(string name)
    {
        Name = name;
        return this;
    }

    /// <summary>
    /// Sets the value of the embed field, optionally specifying whether the field should be displayed inline, and returns the current instance of <see cref="DiscordEmbedField"/>.
    /// </summary>
    /// <param name="value">The content to set for the embed field. This is typically displayed as the text or body of the field in a Discord embed.</param>
    /// <param name="inline">A boolean value indicating whether the embed field should be displayed inline with other fields. Defaults to true.</param>
    /// <returns>The current instance of <see cref="DiscordEmbedField"/> with the updated value and inline property.</returns>
    public DiscordEmbedField WithValue(object value, bool inline = true)
    {
        Value = value.ToString();
        IsInline = inline;
        
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DiscordEmbedField"/> with the specified name, value, and inline configuration.
    /// </summary>
    /// <param name="name">The name of the embed field. This is typically displayed as the title or header of the field in a Discord embed.</param>
    /// <param name="value">The value or content of the embed field. This is typically displayed as the body or text of the field in a Discord embed.</param>
    /// <param name="isInline">A value indicating whether the field should be displayed inline with other fields. If true, the field appears alongside other inline fields. Otherwise, it is displayed on its own row.</param>
    /// <returns>A new instance of <see cref="DiscordEmbedField"/> populated with the specified parameters.</returns>
    public static DiscordEmbedField Create(string name, object value, bool isInline = false)
    {
        var result = new DiscordEmbedField();

        result.Name = name;
        result.Value = value.ToString();
        result.IsInline = isInline;

        return result;
    }
}
