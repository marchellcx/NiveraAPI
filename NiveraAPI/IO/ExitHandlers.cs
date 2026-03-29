using System.Diagnostics;

namespace NiveraAPI.IO;

/// <summary>
/// Provides functionality to handle application exit events.
/// </summary>
public static class ExitHandlers
{
    private static bool exitByKey = true;

    private static void OnProcessQuit(object _, EventArgs args)
    {
        LibraryLoader.Exit(0, "Process was terminated.");
    }
    
    private static void OnCancelKeyPress(object _, ConsoleCancelEventArgs args)
    {
        args.Cancel = true;

        if (!exitByKey)
            return;
        
        LibraryLoader.Exit(0, "Cancel key was pressed.");
    }
    
    internal static void Initialize()
    {
        if (LibraryLoader.IsConsole && !LibraryLoader.HasArgument("exithandlers.console_cancel_key_event_disabled"))
        {
            exitByKey = !LibraryLoader.HasArgument("console.cancel_key_disabled");
            
            System.Console.CancelKeyPress += OnCancelKeyPress;
        }

        if (!LibraryLoader.HasArgument("exithandlers.process_exit_event_disabled"))
        {
            var process = Process.GetCurrentProcess();

            if (process != null)
                process.Exited += OnProcessQuit;
        }
    }
}