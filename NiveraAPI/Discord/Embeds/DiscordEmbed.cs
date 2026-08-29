using System.Drawing;

using Newtonsoft.Json;

namespace NiveraAPI.Discord.Embeds;

/// <summary>
/// Represents a data model for a Discord embed message. Embeds are rich content messages
/// that are commonly used in Discord to send structured, visually enriched data.
/// </summary>
public struct DiscordEmbed
{
    /// <summary>
    /// Gets or sets the title of the Discord embed.
    /// This text appears as the main heading of the embed content.
    /// The title is optional and can include markdown formatting for additional styling.
    /// </summary>
    [JsonProperty("title")] 
    public string Title;

    /// <summary>
    /// Gets or sets the description of the Discord embed.
    /// The description serves as the main body of the embed, providing detailed context or information.
    /// It supports markdown formatting and is optional.
    /// </summary>
    [JsonProperty("description")] 
    public string Description;

    /// <summary>
    /// Gets or sets the URL associated with the Discord embed.
    /// This URL is displayed as a hyperlink when the title of the embed is set.
    /// It provides a way to direct users to an external resource. The URL is optional.
    /// </summary>
    [JsonProperty("url")] 
    public string Url;

    /// <summary>
    /// Gets or sets the color of the Discord embed.
    /// The color is represented as a 24-bit integer in hexadecimal RGB format.
    /// It is used to add a visual accent to the embed, typically displayed as a colored strip or bar.
    /// The value is optional and can be null.
    /// </summary>
    [JsonProperty("color")] 
    public int? Color;

    /// <summary>
    /// Gets or sets the author information for the Discord embed.
    /// The author typically includes a name, an optional URL, and optional icon URLs,
    /// providing additional context or attribution for the embed content.
    /// </summary>
    [JsonProperty("author")] 
    public DiscordEmbedAuthor? Author;

    /// <summary>
    /// Gets or sets the footer of the Discord embed.
    /// The footer typically contains supplemental information displayed at the bottom of the embed,
    /// such as additional context or attribution.
    /// It can include text and optionally a small icon.
    /// </summary>
    [JsonProperty("footer")] 
    public DiscordEmbedFooter? Footer;

    /// <summary>
    /// Gets or sets the image of the Discord embed.
    /// The image is displayed below the description or fields and can be used to provide visual content
    /// associated with the embed. The image is optional and can include a URL to the source image.
    /// </summary>
    [JsonProperty("image")]
    public DiscordEmbedImage? Image;

    /// <summary>
    /// Gets or sets the thumbnail image of the Discord embed.
    /// The thumbnail is an optional small image displayed to the right of the embed content.
    /// </summary>
    [JsonProperty("thumbnail")]
    public DiscordEmbedImage? Thumbnail;

    /// <summary>
    /// Gets or sets the video associated with the Discord embed.
    /// The video typically appears as an embedded element within the embed content.
    /// This property is optional and can include metadata such as the video URL, height, and width.
    /// </summary>
    [JsonProperty("video")]
    public DiscordEmbedImage? Video;

    /// <summary>
    /// Gets or sets the collection of fields within the Discord embed.
    /// Each field contains a title and value pair, used to display
    /// additional structured information in the embed content.
    /// Fields are optional and are typically arranged in a columnar format.
    /// </summary>
    [JsonProperty("fields")] 
    public DiscordEmbedField[]? Fields;

    /// <summary>
    /// Retrieves the color of the embed as a DiscordEmbedColor instance.
    /// </summary>
    /// <return>
    /// A DiscordEmbedColor instance if the color is defined; otherwise, null.
    /// </return>
    public DiscordEmbedColor? GetColor()
    {
        if (Color.HasValue)
        {
            return new DiscordEmbedColor(Color.Value);
        }

        return null;
    }

    /// <summary>
    /// Sets the author of the embed and returns the updated embed instance.
    /// </summary>
    /// <param name="author">
    /// An instance of DiscordEmbedAuthor representing the author to set for the embed.
    /// </param>
    /// <return>
    /// The current instance of DiscordEmbed with the updated author.
    /// </return>
    public DiscordEmbed WithAuthor(DiscordEmbedAuthor? author)
    {
        Author = author;
        return this;
    }

    /// <summary>
    /// Sets the author information for the DiscordEmbed instance.
    /// </summary>
    /// <param name="name">The name of the author to display.</param>
    /// <param name="url">The URL associated with the author. This is optional.</param>
    /// <param name="iconUrl">The URL of the author's icon. This is optional.</param>
    /// <param name="proxyIconUrl">The proxied URL of the author's icon. This is optional.</param>
    /// <return>
    /// The current DiscordEmbed instance with the specified author information applied.
    /// </return>
    public DiscordEmbed WithAuthor(string name, string url = "", string iconUrl = "", string proxyIconUrl = "")
    {
        Author = DiscordEmbedAuthor.Create(name, url, iconUrl, proxyIconUrl);
        return this;
    }

    /// <summary>
    /// Sets the title of the embed and optionally assigns a URL associated with the title.
    /// </summary>
    /// <param name="title">
    /// The title to be displayed on the embed.
    /// </param>
    /// <param name="url">
    /// An optional URL to be linked with the title. Pass null if no URL is required.
    /// </param>
    /// <return>
    /// The current instance of <see cref="DiscordEmbed"/> with the title set.
    /// </return>
    public DiscordEmbed WithTitle(string title, string url = "")
    {
        Title = title;

        if (url != null)
            Url = url;

        return this;
    }

    /// <summary>
    /// Sets the color of the embed using the specified color.
    /// </summary>
    /// <param name="color">The color to set for the embed as a System.Drawing.Color instance.</param>
    /// <return>
    /// The current DiscordEmbed instance with the updated color.
    /// </return>
    public DiscordEmbed WithColor(Color color)
    {
        Color = new DiscordEmbedColor(color).ToHexRgb();
        return this;
    }

    /// <summary>
    /// Sets the description of the embed to the specified string value.
    /// </summary>
    /// <param name="description">The new description to set for the embed.</param>
    /// <return>
    /// The current instance of the <c>DiscordEmbed</c> with the updated description.
    /// </return>
    public DiscordEmbed WithDescription(string description)
    {
        Description = description;
        return this;
    }

    
    /// <summary>
    /// Sets the footer for the embed and returns the updated DiscordEmbed instance.
    /// </summary>
    /// <param name="footer">The footer to be associated with the embed. Can be null to clear the footer.</param>
    /// <return>
    /// The updated DiscordEmbed instance with the specified footer set.
    /// </return>
    public DiscordEmbed WithFooter(DiscordEmbedFooter? footer)
    {
        Footer = footer;
        return this;
    }

    /// <summary>
    /// Sets the footer for the embed with the specified text, icon URL, and proxy icon URL.
    /// </summary>
    /// <param name="text">The text to display in the footer.</param>
    /// <param name="iconUrl">The URL of the footer's icon. This parameter is optional.</param>
    /// <param name="proxyIconUrl">The URL of the proxy icon for the footer. This parameter is optional.</param>
    /// <return>
    /// The current instance of <see cref="DiscordEmbed"/> with the footer set.
    /// </return>
    public DiscordEmbed WithFooter(string text, string iconUrl = "", string proxyIconUrl = "")
    {
        Footer = DiscordEmbedFooter.Create(text, iconUrl, proxyIconUrl);
        return this;
    }

    /// <summary>
    /// Sets the image of the embed.
    /// </summary>
    /// <param name="image">
    /// A DiscordEmbedImage instance representing the image to be set.
    /// </param>
    /// <return>
    /// The current DiscordEmbed instance with the image set.
    /// </return>
    public DiscordEmbed WithImage(DiscordEmbedImage? image)
    {
        Image = image;
        return this;
    }

    /// <summary>
    /// Sets the image of the embed using the specified parameters.
    /// </summary>
    /// <param name="imageUrl">The URL of the image to display in the embed.</param>
    /// <param name="proxyImageUrl">The proxied URL of the image, if applicable. Optional.</param>
    /// <param name="height">The height of the image in pixels. Optional.</param>
    /// <param name="width">The width of the image in pixels. Optional.</param>
    /// <returns>
    /// The current DiscordEmbed instance with the image set.
    /// </returns>
    public DiscordEmbed WithImage(string imageUrl, string proxyImageUrl = "", int? height = null, int? width = null)
    {
        Image = DiscordEmbedImage.Create(imageUrl, proxyImageUrl, height, width);
        return this;
    }

    /// <summary>
    /// Sets the thumbnail image of the embed.
    /// </summary>
    /// <param name="thumbnail">The thumbnail image to set, represented as a DiscordEmbedImage instance.</param>
    /// <returns>
    /// The current instance of DiscordEmbed with the thumbnail image set.
    /// </returns>
    public DiscordEmbed WithThumbnail(DiscordEmbedImage? thumbnail)
    {
        Thumbnail = thumbnail;
        return this;
    }

    /// <summary>
    /// Sets the thumbnail for the embed using the specified image URL, optional proxy URL, and dimensions.
    /// </summary>
    /// <param name="imageUrl">The direct URL to the thumbnail image.</param>
    /// <param name="proxyImageUrl">An optional proxy URL for the thumbnail image.</param>
    /// <param name="height">The optional height of the thumbnail image in pixels.</param>
    /// <param name="width">The optional width of the thumbnail image in pixels.</param>
    /// <return>
    /// The current instance of <see cref="DiscordEmbed"/> with the updated thumbnail.
    /// </return>
    public DiscordEmbed WithThumbnail(string imageUrl, string proxyImageUrl = "", int? height = null, int? width = null)
    {
        Thumbnail = DiscordEmbedImage.Create(imageUrl, proxyImageUrl, height, width);
        return this;
    }

    /// <summary>
    /// Sets the video of the embed using the specified DiscordEmbedImage instance.
    /// </summary>
    /// <param name="video">The DiscordEmbedImage instance representing the video to associate with the embed.</param>
    /// <return>
    /// The current instance of <see cref="DiscordEmbed"/> with the video property updated.
    /// </return>
    public DiscordEmbed WithVideo(DiscordEmbedImage? video)
    {
        Video = video;
        return this;
    }

    /// <summary>
    /// Configures this embed with a video using the specified parameters.
    /// </summary>
    /// <param name="videoUrl">The URL of the video to include in the embed.</param>
    /// <param name="proxyVideoUrl">The proxy URL of the video, if applicable. Optional.</param>
    /// <param name="height">The height of the video in pixels. Optional.</param>
    /// <param name="width">The width of the video in pixels. Optional.</param>
    /// <return>
    /// The current instance of the <see cref="DiscordEmbed"/> with the video configured.
    /// </return>
    public DiscordEmbed WithVideo(string videoUrl, string proxyVideoUrl = "", int? height = null, int? width = null)
    {
        Video = DiscordEmbedImage.Create(videoUrl, proxyVideoUrl, height, width);
        return this;
    }

    /// <summary>
    /// Adds the specified fields to the current Discord embed.
    /// </summary>
    /// <param name="fields">
    /// An array of DiscordEmbedField instances to add to the embed.
    /// </param>
    /// <return>
    /// The current DiscordEmbed instance with the added fields.
    /// </return>
    public DiscordEmbed WithFields(params DiscordEmbedField[] fields)
    {
        if (Fields != null)
        {
            var list = new List<DiscordEmbedField>(Fields);

            list.AddRange(fields);

            Fields = list.ToArray();
            return this;
        }

        Fields = fields;
        return this;
    }

    /// <summary>
    /// Adds a single field to the embed using the specified name, value, and inline configuration.
    /// </summary>
    /// <param name="name">The name of the field to be added. This typically appears as the field's title.</param>
    /// <param name="value">The value of the field, which provides the content or description of the field.</param>
    /// <param name="inline">A boolean indicating whether the field should be displayed inline with other fields. Defaults to true.</param>
    /// <return>
    /// The updated <see cref="DiscordEmbed"/> instance with the newly added field.
    /// </return>
    public DiscordEmbed WithField(string name, object value, bool inline = true)
    {
        return WithFields(DiscordEmbedField.Create(name, value, inline));
    }

    /// <summary>
    /// Resets all the fields in the embed to null.
    /// </summary>
    /// <return>
    /// The current instance of the DiscordEmbed object after the fields have been reset.
    /// </return>
    public DiscordEmbed ResetFields()
    {
        Fields = null;
        return this;
    }
}