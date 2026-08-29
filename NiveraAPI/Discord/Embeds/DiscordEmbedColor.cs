using System.Drawing;
using System.Globalization;

namespace NiveraAPI.Discord.Embeds;

/// <summary>
/// Represents the color of a Discord embed.
/// </summary>
public struct DiscordEmbedColor
{
    /// <summary>
    /// The color of the embed.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Creates a new DiscordEmbedColor instance.
    /// </summary>
    public DiscordEmbedColor(int color)
        => Color = ColorTranslator.FromHtml(color.ToString("X6"));

    /// <summary>
    /// Creates a new DiscordEmbedColor instance.
    /// </summary>
    public DiscordEmbedColor(Color color)
        => Color = color;

    /// <summary>
    /// Converts the RGB components of the color to a hexadecimal integer representation.
    /// </summary>
    /// <returns>
    /// A 24-bit integer representing the RGB color in hexadecimal format.
    /// </returns>
    public int ToHexRgb()
    {
        var s = Color.R.ToString("X2") + Color.G.ToString("X2") + Color.B.ToString("X2");
        return int.Parse(s, NumberStyles.HexNumber, null);
    }
}