using System.Collections.ObjectModel;

using NiveraAPI.Logs;

namespace NiveraAPI.Tests.Core
{
    /// <summary>
    /// The entry point of the application.
    /// </summary>
    public static class Program
    {
        private static volatile LogSink log = LogManager.GetSource("Tests", "Core");
        
        /// <summary>
        /// Global tests dictionary.
        /// </summary>
        public static readonly ReadOnlyDictionary<string, Action> Tests = new(new Dictionary<string, Action>()
        {
            { "cmd", CommandManagerTest.Start },
            { "storage", StorageTests.Start },
            
            { "telepathy_client", NetTests.Client },
            { "telepathy_server", NetTests.Server }
        });
        
        public static async Task Main()
        {
            try
            {
                LibraryLoader.Initialize();
                
                log.Info("Loading ..");

                if (!LibraryLoader.HasArgument("test", out var test))
                {
                    log.Error("No test specified!");
                    return;
                }

                if (!Tests.TryGetValue(test, out var action))
                {
                    log.Error($"Test &1{test}&r not found!");
                    return;
                }
                
                log.Info($"Running test &1{test}&r");

                action();

                while (true)
                {
                    try
                    {
                        LibraryUpdate.Invoke();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Unhandled exception:");
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(ex);
                System.Console.ResetColor();

                await Task.Delay(-1);
            }
        }
    }
}