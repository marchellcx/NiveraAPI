using NiveraAPI.Logs;

namespace NiveraAPI.Networking.Telepathy;

/// <summary>
/// Settings for Telepathy.
/// </summary>
public class TelepathySettings
{
    private static volatile LogSink log = LogManager.GetSource("Networking", "TelepathySettings");
    
    /// <summary>
    /// The maximum size, in bytes, allowed for a single message.
    /// </summary>
    public static volatile int MAX_MSG_SIZE = 16384;

    /// <summary>
    /// The interval, in milliseconds, at which the server will send a ping to the client and vice versa.
    /// </summary>
    public static volatile int PING_INTERVAL = 300;

    internal static void ReadSettings()
    {
        if (LibraryLoader.HasArgument("telepathy.max_msg_size", out var value)
            && int.TryParse(value, out var size))
        {
            MAX_MSG_SIZE = size;
            
            log.Debug($"&3MAX_MSG_SIZE&r set to &1{size} bytes&r.");
        }

        if (LibraryLoader.HasArgument("telepathy.ping_interval", out value)
            && int.TryParse(value, out var interval))
        {
            PING_INTERVAL = interval;
            
            log.Debug($"&3PING_INTERVAL&r set to &1{interval} ms&r.");
        }
    }
}