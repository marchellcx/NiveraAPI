using NiveraAPI.Logs;
using NiveraAPI.Rest.Steam;

namespace NiveraAPI.Tests;

/// <summary>
/// Contains a set of tests and utility methods for managing and verifying
/// Steam authentication processes. These tests ensure that the Steam authentication
/// server operates as expected and logs relevant information for debugging and monitoring.
/// </summary>
public static class SteamAuthTests
{
    /// <summary>
    /// A static instance of <see cref="LogSink"/> used for logging activities
    /// related to Steam authentication tests. It is initialized with a specific
    /// category ("Tests") and name ("SteamAuth") to organize and distinguish log messages.
    /// </summary>
    public static LogSink Log = LogManager.GetSource("Tests", "SteamAuth");

    /// <summary>
    /// Initiates the Steam authentication server and logs the session details.
    /// This method starts an authentication server instance for Steam,
    /// monitors the session's state, and logs key information such as the session URL and result details.
    /// </summary>
    public static void Start()
    {
        Log.Info("Starting SteamAuth server ..");

        var session = SteamAuthManager.AuthOnce("127.0.0.1", s =>
        {
            Log.Info($"Result: &6[{s.Id}]&r &1{s.Error}&r = &3{s.SteamId}&r");
        });
        
        Log.Info("Started");
        Log.Info(session.Url);
    }
}