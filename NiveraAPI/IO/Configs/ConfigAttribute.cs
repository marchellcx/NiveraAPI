namespace NiveraAPI.IO.Configs;

/// <summary>
/// Represents an attribute used to annotate configuration-related properties or fields within a class.
/// This attribute allows specifying metadata such as configuration section, key, and an optional comment
/// for the associated member.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ConfigAttribute : Attribute
{
    /// <summary>
    /// The key of the configuration value.
    /// </summary>
    public string? Key { get; }
    
    /// <summary>
    /// The section of the configuration value.
    /// </summary>
    public string? Section { get; }
    
    /// <summary>
    /// The comment associated with the configuration value.
    /// </summary>
    public string? Comment { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="ConfigAttribute"/> class.
    /// </summary>
    public ConfigAttribute()
    {
        
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ConfigAttribute"/> class.
    /// </summary>
    /// <param name="key">The key of the configuration value.</param>
    public ConfigAttribute(string? key)
    {
        Key = key;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ConfigAttribute"/> class.
    /// </summary>
    /// <param name="section">The section of the configuration value.</param>
    /// <param name="key">The key of the configuration value.</param>
    public ConfigAttribute(string? section, string? key)
    {
        Key = key;
        Section = section;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ConfigAttribute"/> class.
    /// </summary>
    /// <param name="section">The section of the configuration value.</param>
    /// <param name="key">The key of the configuration value.</param>
    /// <param name="comment">The comment of the configuration value.</param>
    public ConfigAttribute(string? section, string? key, string? comment)
    {
        Key = key;
        Section = section;
        Comment = comment;
    }
}