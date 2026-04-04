using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

using NiveraAPI.IO;
using NiveraAPI.IO.Serialization;

using NiveraAPI.Logs;
using NiveraAPI.Console;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;
using NiveraAPI.IO.Network;
using NiveraAPI.TokenParsing;

namespace NiveraAPI
{
    /// <summary>
    /// Provides functionality to initialize the library by configuring essential components and ensuring proper setup.
    /// This class is intended to be used before accessing any other features
    /// of the library to guarantee correct initialization.
    /// </summary>
    /// <remarks>
    /// The LibraryLoader class serves as the entry point for initialization routines within the library.
    /// It ensures that all required subsystems, such as serialization and threading utilities,
    /// are properly initialized prior to usage. This class is designed as a static utility and
    /// does not support instantiation.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if initialization is attempted more than once. The library can only be initialized once
    /// to maintain a consistent state for its underlying components.
    /// </exception>
    public static class LibraryLoader
    {
        internal static bool cmdDebugToggle;
        
        private static bool loaded = false;
        private static bool console = false;
        private static bool check = false;

        /// <summary>
        /// Gets called when the application is about to exit.
        /// </summary>
        public static event Action? Exiting;

        /// <summary>
        /// Whether or not the application is running in a console window.
        /// </summary>
        public static bool IsConsole
        {
            get
            {
                if (!check)
                {
                    try
                    {
                        console = GetConsoleWindow() != IntPtr.Zero;
                    }
                    catch
                    {
                        console = HasArgument("console");
                    }
                    
                    check = true;
                }

                return console;
            }
        }
        
        /// <summary>
        /// Represents a thread-safe collection of parsed command-line arguments, with
        /// keys as argument names and values as their associated parameters. If an
        /// argument does not have a parameter, its value is set to <c>null</c>.
        /// </summary>
        /// <threadSafety>
        /// This variable is thread-safe and can be accessed concurrently by multiple
        /// threads without additional synchronization.
        /// </threadSafety>
        public static volatile ConcurrentDictionary<string, string?> ParsedArguments = new();
        
        /// <summary>
        /// Retrieves the handle to the current console window.
        /// This method allows interaction with the console window through its handle.
        /// </summary>
        /// <returns>
        /// A pointer to the console window's handle, or <c>IntPtr.Zero</c> if no console window is associated with the process.
        /// </returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();
        
        /// <summary>
        /// Initializes the library by setting up essential components and verifying initialization status.
        /// This method must be called once before any other library features are used.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the library has already been initialized.
        /// </exception>
        public static void Initialize()
        {
            if (loaded)
                throw new InvalidOperationException("Library already initialized");
            
            ParseArguments();
            
            if (IsConsole)
                ConsoleOutput.Initialize();
            
            ProcessArguments();
            
            InitPools();
            
            ExitHandlers.Initialize();
            
            ByteWriter.InitWriters();
            ByteReader.InitReaders();
            
            ThreadHelper.Initialize();
            ReflectionHelper.Initialize();
            
            TokenParser.Initialize();

            if (IsConsole)
                ConsoleCommands.Initialize();
            
            loaded = true;
        }

        /// <summary>
        /// Terminates the current process with the specified exit code.
        /// This method outputs an appropriate message to the console and triggers the <see cref="Exiting"/> event before exiting.
        /// </summary>
        /// <param name="code">
        /// The exit code to be returned to the operating system. A code of 0 typically indicates success,
        /// while non-zero codes indicate an error or specific exit condition.
        /// </param>
        /// <param name="msg">The message to be displayed to the user before exiting.</param>
        public static void Exit(int code = 1, string? msg = null)
        {
            if (IsConsole)
                ConsoleOutput.Write($"Exiting .. ({code}): {msg ?? "No message provided."}", code == 0 ? ConsoleColor.Green : ConsoleColor.Red);

            try
            {
                Exiting?.Invoke();
            }
            catch (Exception ex)
            {
                ConsoleOutput.Write($"Error while exiting:\n{ex}", ConsoleColor.Red);
            }
            
            Environment.Exit(code);
        }

        /// <summary>
        /// Determines whether a specific argument exists in the parsed arguments dictionary.
        /// </summary>
        /// <param name="toggle">The key of the argument to check for presence in the parsed arguments.</param>
        /// <returns>
        /// <c>true</c> if the specified argument key exists; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasArgument(string toggle)
            => ParsedArguments.ContainsKey(toggle);

        /// <summary>
        /// Checks whether a specific argument exists in the parsed arguments and retrieves its associated value.
        /// </summary>
        /// <param name="toggle">
        /// The key representing the argument to check for in the parsed arguments.
        /// </param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key if the key is found
        /// and the value is not null or whitespace; otherwise, an empty or null string.
        /// </param>
        /// <returns>
        /// A boolean indicating whether the specified argument exists and has a valid non-empty value.
        /// Returns <c>true</c> if the argument exists and has a valid non-empty value; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasArgument(string toggle, out string value)
        {
            if (ParsedArguments.TryGetValue(toggle, out value) && !string.IsNullOrWhiteSpace(value))
                return true;
            
            return false;
        }

        private static void InitPools()
        {
            StaticConstructor<StringBuilder>.Set(() => new());
        }

        private static void ParseArguments()
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Length > 0)
            {
                for (var x = 0; x < args.Length; x++)
                {
                    var arg = args[x];
                    
                    if (arg.StartsWith("--"))
                    {
                        if (x + 1 < args.Length && !args[x + 1].StartsWith("-"))
                        {
                            ParsedArguments.TryAdd(arg.Substring(2), args[x + 1]);
                        }
                        else if (arg.TrySplit('=', 2, true, true, out var parts))
                        {
                            ParsedArguments.TryAdd(parts[0].Substring(2), parts[1]);
                        }
                    }
                    else if (arg.StartsWith("-"))
                    {
                        ParsedArguments.TryAdd(arg.Substring(1), null);
                    }
                }
            }
        }
        
        private static void ProcessArguments()
        {
            LogManager.DisabledLogs = LogLevel.Debug | LogLevel.Verbose;
            
            if (HasArgument("io.writer.buffer.size", out var value)
                && int.TryParse(value, out var bufferSize))
                IOSettings.BYTE_WRITER_BUFFER_INIT_SIZE = bufferSize;
            
            if (HasArgument("io.writer.buffer.resizing", out value)
                && bool.TryParse(value, out var resize))
                IOSettings.BYTE_WRITER_BUFFER_RESIZING = resize;
            
            if (HasArgument("io.writer.buffer.resize_mult", out value)
                && int.TryParse(value, out var mult))
                IOSettings.BYTE_WRITER_BUFFER_RESIZE_MULT = mult;

            if (HasArgument("log.disabled_levels", out value)
                && value.TrySplit(',', null, true, true, out var levels)
                && levels.TryConvertStringArray<LogLevel>(Enum.TryParse, out var logLevels))
                LogManager.DisabledLogs = logLevels.CombineFlags();

            if (HasArgument("log.allow_debug"))
                LogManager.DisabledLogs = LogLevel.Debug;

            if (HasArgument("log.allow_verbose"))
                LogManager.DisabledLogs = null;

            if (HasArgument("log.disable_true_color"))
                LogManager.TrueColor = false;
            
            if (HasArgument("log.unity_rich_text"))
                LogManager.UnityColorTags = true;
            
            if (HasArgument("log.disabled_sources", out value)
                && value.TrySplit(',', null, true, true, out var sources))
                LogManager.DisabledSources.AddRange(sources);

            if (HasArgument("log.enable_all"))
            {
                LogManager.DisabledLogs = null;
                LogManager.DisabledSources.Clear();
            }
            
            if (HasArgument("log.use_queue"))
                LogManager.UseQueue = true;

            if (HasArgument("commands.debug"))
                cmdDebugToggle = true;
            
            if (HasArgument("netio_mtu", out value)
                && int.TryParse(value, out var mtu))
                NetSettings.MTU = mtu;
            
            if (HasArgument("netio_pingint", out value)
                && int.TryParse(value, out var pingInterval))
                NetSettings.PING_INT = pingInterval;
        }
    }
}