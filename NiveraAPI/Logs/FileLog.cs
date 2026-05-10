using System.Collections.Concurrent;

using NiveraAPI.Console;
using NiveraAPI.Extensions;

namespace NiveraAPI.Logs;

/// <summary>
/// Provides functionality for writing and managing log messages to a file.
/// </summary>
public static class FileLog
{
    private static volatile bool running;
    
    private static volatile StreamWriter writer;
    private static volatile ConcurrentQueue<string> logs = new();

    /// <summary>
    /// Writes a log message to the file log if logging is currently active.
    /// </summary>
    /// <param name="msg">The log message to be written. Must be passed by reference.</param>
    /// <returns>
    /// Returns <c>true</c> if the log message was successfully added to the log queue;
    /// otherwise, <c>false</c> if logging is not active.
    /// </returns>
    public static bool Write(ref LogMessage msg)
    {
        if (!running)
            return false;
        
        var time = msg.Time.ToString("G");
        var source = msg.CategoryText;

        if (msg.SourceText?.Length > 0)
            source += $" / {msg.SourceText}";
        
        logs.Enqueue($"{msg.Level} :: {source} @ {time} -> {msg.MessageText}");
        return true;
    }

    private static void Update()
    {
        running = true;
        
        while (running)
        {
            var flush = false;
            
            while (logs.TryDequeue(out var message))
            {
                message = message.SanitizeTrueColorString();

                try
                {
                    writer.WriteLine(message);

                    flush = true;
                }
                catch (Exception ex)
                {
                    ConsoleOutput.Write($"File log stopped due to an error:\n{ex}", ConsoleColor.Red);

                    running = false;
                    break;
                }
            }
            
            if (flush)
                writer.Flush();
        }
        
        while (logs.TryDequeue(out _)) { }
    }

    internal static void Initialize()
    {
        if (LibraryLoader.HasArgument("FileLogDisabled"))
            return;

        try
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "logs");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (LibraryLoader.HasArgument("FileLogDirectory", out var dir))
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        path = dir;
                    }
                    else
                    {
                        Directory.CreateDirectory(dir);

                        path = dir;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleOutput.Write($"Could not use custom file log directory:\n{ex}", ConsoleColor.Red);
                }
            }

            var time = DateTime.UtcNow;
            var file = $"Log_Y{time.Year}M{time.Month}D{time.Day}H{time.Hour}m{time.Minute}S{time.Second}.txt";

            path = Path.Combine(path, file);

            if (File.Exists(path))
                File.Delete(path);

            writer = File.CreateText(path);
            writer.AutoFlush = false;

            new Thread(Update).Start();
            
            ConsoleOutput.Write($"Enabled file log: {path}", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            ConsoleOutput.Write($"Could not start file log:\n{ex}", ConsoleColor.Red);
        }
    }
}