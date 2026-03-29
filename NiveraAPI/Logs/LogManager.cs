using System.Collections.Concurrent;

using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

namespace NiveraAPI.Logs
{
    /// <summary>
    /// Manages logging.
    /// </summary>
    public static class LogManager
    {
        /// <summary>
        /// Represents a combination of all available log levels, enabling comprehensive logging across Debug,
        /// Information, Warning, Error, Fatal, and Verbose levels.
        /// </summary>
        public const LogLevel AllLevels = 
            LogLevel.Debug | LogLevel.Information | LogLevel.Warning | LogLevel.Error |  LogLevel.Fatal | LogLevel.Verbose;

        /// <summary>
        /// Gets called once a new message is logged to the console. The string parameter is the message that was logged.
        /// </summary>
        public static event Action<LogMessage>? Log;

        private static List<LogSink> sources = new();
        private static ConcurrentQueue<LogMessage> logs = new();

        /// <summary>
        /// Whether or not to enable true-color log formatting.
        /// </summary>
        public static bool TrueColor { get; set; } = true;
        
        /// <summary>
        /// Whether or not to convert color prefixes to Unity Rich Text color tags.
        /// </summary>
        public static bool UnityColorTags { get; set; }

        /// <summary>
        /// Whether or not to use a queue to process log events. 
        /// If enabled, log messages will be added to a queue and processed in batches during the UpdateQueue method call.
        /// </summary>
        public static bool UseQueue { get; set; } = true;

        /// <summary>
        /// Gets or sets the rate-limit for log updates.
        /// </summary>
        public static RateLimit? RateLimit { get; set; }

        /// <summary>
        /// Gets or sets the log levels that are disabled and will not be recorded by the logger.
        /// </summary>
        public static LogLevel? DisabledLogs { get; set; } = LogLevel.Debug | LogLevel.Verbose;

        /// <summary>
        /// A list of all disabled log sources.
        /// </summary>
        public static List<string> DisabledSources { get; } = new();

        /// <summary>
        /// Gets a list of registered log sources.
        /// </summary>
        public static IReadOnlyList<LogSink> Sources { get; } = sources.AsReadOnly();

        /// <summary>
        /// Retrieves an existing log source with the specified category and name, or creates a new one if none exists.
        /// </summary>
        /// <param name="category">The category of the log source to retrieve or create. Cannot be null, empty, or consist only of white-space
        /// characters.</param>
        /// <param name="name">The name of the log source to retrieve or create. Cannot be null, empty, or consist only of white-space
        /// characters.</param>
        /// <returns>A LogSource instance matching the specified category and name. If no such source exists, a new instance is
        /// created and returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown if category or name is null, empty, or consists only of white-space characters.</exception>
        public static LogSink GetSource(string category, string name)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentNullException("category");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            var curSource = sources.Find(x =>
                x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (curSource != null)
                return curSource;

            curSource = new(category, name);

            if (RateLimit != null)
                curSource.RateLimit = RateLimit.Clone();

            if (DisabledLogs != null)
                curSource.AllowedLogs = AllLevels & ~DisabledLogs.Value;
            else
                curSource.AllowedLogs = AllLevels;

            sources.Add(curSource);
            return curSource;
        }

        /// <summary>
        /// Processes the log queue. The <see cref="Log"/> event will be called for each log message in the queue, 
        /// subject to the rate limit if one is set. This method should be called regularly, such as once per frame, 
        /// to ensure that log messages are processed in a timely manner.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static void UpdateQueue()
        {
            if (!UseQueue)
                throw new InvalidOperationException($"Cannot update the log queue when UseQueue is disabled.");

            while (logs.TryDequeue(out var log) 
                && (RateLimit is null || RateLimit.TryAcquire()))
            {
                try
                {
                    if (!TrueColor)
                    {
                        log.LevelText = log.LevelText.SanitizeTrueColorString();
                        log.SourceText = log.SourceText.SanitizeTrueColorString();
                        log.MessageText = log.MessageText!.SanitizeTrueColorString();
                        log.CategoryText = log.CategoryText.SanitizeTrueColorString();
                    }
                    else if (UnityColorTags)
                    {
                        log.LevelText = log.LevelText.FormatTrueColorString("white", true, false);
                        log.SourceText = log.SourceText.FormatTrueColorString("white", true, false);
                        log.MessageText = log.MessageText!.FormatTrueColorString("white", true, false);
                        log.CategoryText = log.CategoryText.FormatTrueColorString("white", true, false);
                    }
                    
                    Log?.Invoke(log);
                }
                catch
                {
                    // ignored
                }
            }
        }

        internal static void Print(ref LogMessage message)
        {
            // if (DisabledLogs != null && (message.Level & DisabledLogs.Value) != 0)
            //    return;

            if (!UseQueue)
            {
                try
                {
                    Log?.Invoke(message);
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                logs.Enqueue(message);
            }
        }
    }
}