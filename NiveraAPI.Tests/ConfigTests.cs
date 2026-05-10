using NiveraAPI.Logs;
using NiveraAPI.IO.Configs;

namespace NiveraAPI.Tests;

/// <summary>
/// Provides test configurations and logging for the NiveraAPI test framework.
/// This class includes predefined configuration elements and a logging sink
/// for use in testing scenarios. It facilitates the management and loading
/// of configurations from an INI file through the <see cref="ConfigHandler"/>.
/// </summary>
public static class ConfigTests
{
    /// <summary>
    /// Represents a logging sink used for managing and categorizing log entries
    /// within the "Tests" configuration context. The log source is created using
    /// the <see cref="LogManager.GetSource(string, string)"/> method with
    /// category "Tests" and name "Config".
    /// </summary>
    public static LogSink Log = LogManager.GetSource("Tests", "Config");

    /// <summary>
    /// Represents a configuration list containing string elements.
    /// This list is configured using a <see cref="ConfigAttribute"/> with a
    /// null section and key, and includes a comment "A string list!".
    /// </summary>
    [Config(null, null, "A string list!")]
    public static List<string> StringList = new()
    {
        "Test",
        "One"
    };

    /// <summary>
    /// Represents a configuration dictionary containing string key-value pairs.
    /// This dictionary is configured using a <see cref="ConfigAttribute"/> with
    /// a specified section ("Testing") and key ("StringDict").
    /// </summary>
    [Config(null, null, "A string dictionary!")]
    public static Dictionary<string, string> StringDict = new()
    {
        { "Test", "One" },
        { "Two", "Two" }
    };

    /// <summary>
    /// Initializes and sets up configuration handling for the application.
    /// This method establishes the file path for the configuration file,
    /// registers all configuration values in the specified configuration type,
    /// and loads existing configuration data from the file.
    /// </summary>
    public static void Start()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "config.ini");
        var handler = new ConfigHandler();

        handler.FilePath = path;
        handler.Register(typeof(ConfigTests));
        handler.Load();
    }
}