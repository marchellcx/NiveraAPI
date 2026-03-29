using NiveraAPI.Commands;
using NiveraAPI.Commands.Results;
using NiveraAPI.Logs;
using NiveraAPI.Utilities;

namespace NiveraAPI.Console;

/// <summary>
/// Provides functionality to manage and execute console commands within the application.
/// </summary>
/// <remarks>
/// This static class is responsible for initializing and managing commands and associated operations
/// in a console environment. It utilizes a command management system and logging mechanisms to
/// handle commands efficiently.
/// </remarks>
public static class ConsoleCommands
{
    private static volatile bool update = false;
    private static volatile EventQueue<string> input = new();
    
    private static LogSink log;
    
    /// <summary>
    /// Gets the command manager.
    /// </summary>
    public static CommandManager<object> Manager { get; private set; }
    
    internal static void Initialize()
    {
        log = LogManager.GetSource("IO", "Console");

        Manager = new();
        Manager.RegisterCommands(typeof(ConsoleCommands).Assembly);

        update = true;
        
        new Thread(UpdateInput).Start();
        
        LibraryUpdate.Register(UpdateCommands);
        
        log.Info("Initialized console commands.");
    }

    private static void WriteCallback(CommandContext<object> ctx)
    {
        if (ctx.Result != null)
        {
            if (ctx.Result is TextResult textResult
                && !string.IsNullOrEmpty(textResult.Text))
            {
                if (textResult.Success)
                {
                    ConsoleOutput.Write(textResult.Text, ConsoleColor.Green);
                }
                else
                {
                    ConsoleOutput.Write(textResult.Text, ConsoleColor.Red);
                }
            }
            else
            {
                ConsoleOutput.Write(ctx.Result.ToString(), ConsoleColor.Yellow);
            }
        }
        else
        {
            ConsoleOutput.Write("Command executed successfully!", ConsoleColor.Green);
        }
    }

    private static void UpdateCommands()
    {
        input.ProcessEvents(line =>
        {
            try
            {
                var search = Manager.SearchCommand(line);

                if (!search.WasFound || search.Command == null || search.Overload == null)
                {
                    ConsoleOutput.Write("Unknown command!", ConsoleColor.Cyan);
                    return;
                }
                
                Manager.ExecuteSearch(ref search, null, WriteCallback);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        });
    }

    private static void UpdateInput()
    {
        while (update)
        {
            Thread.Sleep(100);

            string cmd = string.Empty;

            try
            {
                cmd = System.Console.ReadLine()!;

                if (string.IsNullOrEmpty(cmd))
                    continue;
                
                input.QueueEvent(cmd);
            }
            catch (Exception ex)
            {
                log.Error("Failed to read input:", ex);
                continue;
            }
        }
    }
}