using Newtonsoft.Json;

namespace NiveraAPI.Discord.Embeds;

/// <summary>
/// Represents an image that can be embedded in a Discord message, including its URL,
/// proxy URL, and optional dimensions (height and width).
/// </summary>
public struct DiscordEmbedImage
{
    /// <summary>
    /// The direct URL of the image to be embedded in a Discord message.
    /// </summary>
    [JsonProperty("url")] 
    public string Url;

    /// <summary>
    /// The proxied URL of the image to be embedded in a Discord message.
    /// </summary>
    [JsonProperty("proxy_url")]
    public string ProxyUrl;

    /// <summary>
    /// The height of the embedded image in pixels.
    /// </summary>
    [JsonProperty("height")] 
    public int? Height;

    /// <summary>
    /// The width of the embedded image in pixels.
    /// </summary>
    [JsonProperty("width")] 
    public int? Width;

    /// <summary>
    /// Sets the URL and optional proxy URL for the embedded image.
    /// </summary>
    /// <param name="url">
    /// The direct URL of the image to be displayed in a Discord message.
    /// </param>
    /// <param name="proxyUrl">
    /// The optional proxied URL of the image to be displayed in a Discord message.
    /// </param>
    /// <returns>
    /// The updated <see cref="DiscordEmbedImage"/> instance with the specified URL and proxy URL.
    /// </returns>
    public DiscordEmbedImage WithUrl(string url, string proxyUrl = "")
    {
        Url = url;
        
        ProxyUrl = proxyUrl;
        return this;
    }

    /// <summary>
    /// Sets the resolution of the embedded image by specifying its width and height in pixels.
    /// </summary>
    /// <param name="width">
    /// The width of the embedded image in pixels.
    /// </param>
    /// <param name="height">
    /// The height of the embedded image in pixels.
    /// </param>
    /// <returns>
    /// The updated <see cref="DiscordEmbedImage"/> instance with the specified width and height.
    /// </returns>
    public DiscordEmbedImage WithResolution(int width, int height)
    {
        Height = height;
        Width = width;

        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DiscordEmbedImage"/> with the specified URL, proxy URL, height, and width.
    /// </summary>
    /// <param name="url">
    /// The direct URL of the image or video to be embedded.
    /// </param>
    /// <param name="proxy">
    /// The optional proxied URL of the image or video to be embedded.
    /// </param>
    /// <param name="height">
    /// The optional height of the embedded image or video.
    /// </param>
    /// <param name="width">
    /// The optional width of the embedded image or video.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="DiscordEmbedImage"/> configured with the provided properties.
    /// </returns>
    public static DiscordEmbedImage Create(string url, string proxy = "", int? height = null, int? width = null)
    {
        var result = new DiscordEmbedImage();

        result.Url = url;
        result.ProxyUrl = proxy;
        result.Height = height;
        result.Width = width;

        return result;
    }
}