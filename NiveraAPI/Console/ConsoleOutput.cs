using NiveraAPI.Logs;
using NiveraAPI.Extensions;

namespace NiveraAPI.Console;

/// <summary>
/// Provides functionality to output log messages to the console.
/// </summary>
public static class ConsoleOutput
{
    private static volatile bool windows = false;

    /// <summary>
    /// Writes a message to the console with the specified text color.
    /// </summary>
    /// <param name="message">The message to be written to the console.</param>
    /// <param name="color">The color in which the message should be displayed. Defaults to <see cref="ConsoleColor.White"/>.</param>
    public static void Write(string message, ConsoleColor color = ConsoleColor.White)
    {
        System.Console.ForegroundColor = color;
        System.Console.WriteLine($">>> {message}");
        System.Console.ResetColor();
    }
    
    internal static void Initialize()
    {
        windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        
        if (windows)
            Write("Detected Windows platform.", ConsoleColor.Yellow);
        else
            Write("Detected non-Windows platform.", ConsoleColor.DarkYellow);
        
        LogManager.Log += PrintLog;
        LogManager.UseQueue = true;
        
        new Thread(UpdateQueue).Start();

        Write("Initialized console output.", ConsoleColor.Cyan);
    }

    private static void PrintLog(LogMessage message)
    {
        try
        {
            if (windows && LogManager.TrueColor)
            {
                WindowsPrettyPrint(ref message);
            }
            else if (!windows && LogManager.TrueColor)
            {
                System.Console.WriteLine(message.ToString().FormatTrueColorString());
                System.Console.ResetColor();
            }
            else
            {
                var tagColor = ColorToConsole(LogSink.TagColorForLevel(message.Level));
                var tagTextColor = ColorToConsole(LogSink.TagTextColorForLevel(message.Level));

                System.Console.ResetColor();
                System.Console.WriteLine();
                
                // Time
                System.Console.ForegroundColor = tagColor;
                System.Console.Write("[");
                System.Console.ForegroundColor = tagTextColor;
                System.Console.Write(message.Time.ToString("HH:mm:ss"));
                System.Console.ForegroundColor = tagColor;
                System.Console.Write("] ");
                
                // Level
                System.Console.ForegroundColor = tagColor;
                System.Console.Write("[");
                System.Console.ForegroundColor = tagTextColor;
                System.Console.Write(message.LevelText);
                System.Console.ForegroundColor = tagColor;
                System.Console.Write("] ");
                
                // Source
                System.Console.ForegroundColor = tagColor;
                System.Console.Write("[");
                System.Console.ForegroundColor = tagTextColor;
                System.Console.Write(message.CategoryText);
                System.Console.Write(" / ");
                System.Console.Write(message.SourceText);
                System.Console.ForegroundColor = tagColor;
                System.Console.Write("] ");
                
                // Message
                System.Console.ForegroundColor = message.Level is LogLevel.Error or LogLevel.Fatal ? ConsoleColor.Red : ConsoleColor.White;
                System.Console.Write(message.MessageText);
                System.Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Error while writing log message:");
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(ex);
            System.Console.ResetColor();
        }
    }

    private static void UpdateQueue()
    {
        while (true)
        {
            Thread.Sleep(100);

            try
            {
                LogManager.UpdateQueue();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error while updating log queue:");
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(ex);
                System.Console.ResetColor();
            }
        }
    }

    private static void WindowsPrettyPrint(ref LogMessage message)
    {
        var text = message.ToString();
        
        var tagColor = ColorToConsole(LogSink.TagColorForLevel(message.Level));
        var tagTextColor = ColorToConsole(LogSink.TagTextColorForLevel(message.Level));
        
        // Time
        System.Console.ForegroundColor = tagColor;
        System.Console.Write("[");
        System.Console.ForegroundColor = tagTextColor;
        System.Console.Write(message.Time.ToString("HH:mm:ss"));
        System.Console.ForegroundColor = tagColor;
        System.Console.Write("] ");
        System.Console.ResetColor();

        for (var i = 0; i < text.Length; i++)
        {
            char? next = i < text.Length - 1 ? text[i + 1] : null;
            char? previous = i > 0 ? text[i - 1] : null;
            
            var current = text[i];

            if (previous is '&')
            {
                if (current is 'r')
                {
                    System.Console.ResetColor();
                    continue;
                }

                var color = ColorToConsole(current);

                if (color is not null)
                {
                    System.Console.ForegroundColor = color.Value;
                    continue;
                }
            }
            else if (current is '&' && next.HasValue)
            {
                if (next is 'r')
                    continue;
                
                if (ColorToConsole(next.Value) is not null)
                    continue;
            }

            System.Console.Write(current);
        }
        
        System.Console.WriteLine();
        System.Console.ResetColor();
    }
    
    private static ConsoleColor? ColorToConsole(char color)
    {
        return color switch
        {
            '0' => ConsoleColor.Black,
            '1' => ConsoleColor.Red,
            '2' => ConsoleColor.Green,
            '3' => ConsoleColor.Yellow,
            '4' => ConsoleColor.Blue,
            '5' => ConsoleColor.Magenta,
            '6' => ConsoleColor.Cyan,
            '7' => ConsoleColor.White,

            _ => null
        };
    }

    private static ConsoleColor ColorToConsole(string color)
    {
        return color switch
        {
            "0" => ConsoleColor.Black,
            "1" => ConsoleColor.Red,
            "2" => ConsoleColor.Green,
            "3" => ConsoleColor.Yellow,
            "4" => ConsoleColor.Blue,
            "5" => ConsoleColor.Magenta,
            "6" => ConsoleColor.Cyan,

            _ => ConsoleColor.White,
        };
    }
}