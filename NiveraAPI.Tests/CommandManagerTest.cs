using NiveraAPI.Commands;
using NiveraAPI.Commands.Attributes;

using NiveraAPI.Logs;
using NiveraAPI.Console;
using NiveraAPI.Pooling;

namespace NiveraAPI.Tests;

/// <summary>
/// Tests for the <see cref="CommandManager{TSender}"/> class.
/// </summary>
public static class CommandManagerTest
{
    /// <summary>
    /// Global log sink for command-related tests.
    /// </summary>
    public static volatile LogSink Log = LogManager.GetSource("Tests", "Commands");
    
    /// <summary>
    /// Starts the tests.
    /// </summary>
    public static void Start()
    {
        Log.Info("Registering commands ..");
        
        var dict = ConsoleCommands.Manager.RegisterCommands(typeof(CommandManagerTest).Assembly);
        
        Log.Info($"Done: &1{dict.Count}&r commands registered.");
    }

    [Overload("hello", "Say hello to the world.")]
    [Parameter("Argument", "The argument to say..")]
    private static void HelloCommand(ref CommandContext<object> ctx, string[] arguments)
    {
        if (arguments == null)
        {
            ctx.SetFailText("No argument!");
            return;
        }
        
        ctx.SetOkText(StringBuilderPool.BuildString(x =>
        {
            x.Append("Hello, world!");
            x.Append(" Arguments: ");
            x.Append(arguments.Length);
            x.AppendLine();

            for (var y = 0; y < arguments.Length; y++)
                x.Append('[')
                    .Append(y)
                    .Append("]: ")
                    .Append(arguments[y])
                    .AppendLine();
        }));
    }
}