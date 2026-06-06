namespace NiveraAPI.Utilities;

/// <summary>
/// Provides utility methods for encoding and decoding URLs by replacing special characters
/// with their percent-encoded equivalents and vice versa.
/// </summary>
public static class UrlEncode
{
    /// <summary>
    /// Provides a read-only dictionary mapping special characters to their URL-encoded equivalents.
    /// This property is used for encoding and decoding URLs by replacing specific characters
    /// with their percent-encoded representations.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Characters { get; } = new Dictionary<string, string>()
    {
        { "!", "%21" },
        { "\"", "%22" },
        { "#", "%23" },
        { "$", "%24" },
        { "%", "%25" },
        { "&", "%26" },
        { "'", "%27" },
        { "(", "%28" },
        { ")", "%29" },
        { "*", "%2A" },
        { "+", "%2B" },
        { ",", "%2C" },
        { "-", "%2D" },
        { ".", "%2E" },
        { "/", "%2F" },
        { ":", "%3A" },
        { ";", "%3B" },
        { "<", "%3C" },
        { "=", "%3D" },
        { ">", "%3E" },
        { "?", "%3F" },
        { "@", "%40" },
        { "[", "%5B" },
        { "\\", "%5C" },
        { "]", "%5D" },
        { "^", "%5E" },
        { "_", "%5F" },
        { "`", "%60" },
        { "{", "%7B" },
        { "|", "%7C" },
        { "}", "%7D" },
        { "~", "%7E" },
        { " ", "%7F" },
        { "€", "%E2%82%AC" },
        { "", "%81" },
        { "‚", "%E2%80%9A" },
    };

    /// <summary>
    /// Encodes a URL string by replacing special characters with their percent-encoded equivalents.
    /// </summary>
    /// <param name="str">The input string to be URL-encoded.</param>
    /// <returns>A URL-encoded version of the input string. If the input string is null or empty, an empty string is returned.</returns>
    public static string EncodeUrl(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;
        
        foreach (var pair in Characters)
            str = str.Replace(pair.Key, pair.Value);

        return str;
    }

    /// <summary>
    /// Decodes a percent-encoded URL string by replacing percent-encoded sequences with their corresponding characters.
    /// </summary>
    /// <param name="str">The input string to be decoded from its percent-encoded form.</param>
    /// <returns>A decoded version of the input string. If the input string is null or empty, an empty string is returned.</returns>
    public static string DecodeUrl(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;
        
        foreach (var pair in Characters)
            str = str.Replace(pair.Value, pair.Key);

        return str;
    }
}