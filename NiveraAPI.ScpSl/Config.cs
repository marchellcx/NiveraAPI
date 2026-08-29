using System.ComponentModel;

namespace NiveraAPI.ScpSl;

/// <summary>
/// Represents the configuration settings.
/// </summary>
public class Config
{
    /// <summary>
    /// Determines whether logs from the NiveraAPI library are enabled or disabled.
    /// When set to true, logging is enabled, and logs from the library will be displayed.
    /// When set to false, logging is disabled, and no logs will be shown.
    /// </summary>
    [Description("Whether to show logs from the NiveraAPI library.")]
    public bool EnableLogs { get; set; } = true;

    /// <summary>
    /// Specifies whether debug logs from the NiveraAPI library are enabled or disabled.
    /// When set to true, debug-level logging is enabled, providing detailed diagnostic information.
    /// When set to false, debug logging is disabled, and only higher-level logs, if any, will be shown.
    /// </summary>
    [Description("Whether to show debug logs from the NiveraAPI library.")]
    public bool EnableDebugLogs { get; set; } = false;

    /// <summary>
    /// Specifies whether to use LabAPI's YAML serializer and deserializer for handling configurations.
    /// Setting this property to true enables the usage of LabAPI's YAML functionalities.
    /// Setting it to false may disable YAML-based serialization in favor of an alternative approach.
    /// </summary>
    [Description("Whether to use LabAPI's YAML serializer and deserializer.")]
    public bool UseLabApiYaml { get; set; } = true;

    /// <summary>
    /// Represents a collection of custom paths to plugin configuration files.
    /// The dictionary key represents the plugin name, and the value represents the associated configuration file path.
    /// </summary>
    [Description("Custom paths to plugin config files.")]
    public Dictionary<string, string> ConfigPaths { get; set; } = new();

    /// <summary>
    /// Specifies a list of plugins that utilize global configuration paths.
    /// This property contains the names of plugins which will share or depend on global
    /// configuration settings provided in the NiveraAPI library.
    /// </summary>
    [Description("List of plugins which will use global config paths.")]
    public List<string> GlobalConfigs { get; set; } = new();
}