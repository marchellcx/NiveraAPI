using NiveraAPI.Logs;
using Telepathy;

namespace NiveraAPI.Networking.Telepathy;

/// <summary>
/// Implements logging for the Telepathy library.
/// </summary>
public static class TelepathyLog
{
    private static volatile LogSink log;
    
    internal static void Initialize()
    {
        if (LibraryLoader.HasArgument("disable_telepathy_log"))
            return;

        log = LogManager.GetSource("Telepathy", "Internal");
        
        Log.Info = msg => log.Info(msg);
        Log.Warning = msg => log.Warn(msg);
        Log.Error = msg => log.Error(msg);
    }
}