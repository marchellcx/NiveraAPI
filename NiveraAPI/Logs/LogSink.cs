using System.Reflection;
using NiveraAPI.Utilities;

namespace NiveraAPI.Logs
{
    /// <summary>
    /// Represents a source of log messages, providing methods to write log entries at various severity levels and
    /// associate them with a specific category and name.
    /// </summary>
    public class LogSink
    {
        private volatile string name = string.Empty;
        private volatile string category = string.Empty;

        private volatile RateLimit? rateLimit;

        private volatile LogLevel allowedLogs;

        /// <summary>
        /// Gets the name of the log source.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Gets the category of the log source.
        /// </summary>
        public string Category => category;

        /// <summary>
        /// Gets or sets the rate-limit for this sink.
        /// </summary>
        public RateLimit? RateLimit
        {
            get => rateLimit;
            set => rateLimit = value;
        }

        /// <summary>
        /// Sets the allowed log levels for this sink.
        /// </summary>
        public LogLevel AllowedLogs
        {
            get => allowedLogs;
            set => allowedLogs = value;
        }

        private LogSink() { }

        internal LogSink(string category, string name)
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentNullException(nameof(category));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            this.name = name;
            this.category = category.ToUpperInvariant();
        }

        /// <summary>
        /// Writes a debug-level log message using the specified object as the message content.
        /// </summary>
        /// <remarks>Use this method to record diagnostic information that is useful for debugging.
        /// Debug-level messages are typically only visible when the logging system is configured for verbose
        /// output.</remarks>
        /// <param name="msg">The object to log. If the object is not a string, its string representation is used.</param>
        public void Debug(object msg)
            => Print(LogLevel.Debug, null, ObjectToString(msg));

        /// <summary>
        /// Writes a debug-level log entry with the specified method name and message.
        /// </summary>
        /// <param name="method">The name of the method or operation associated with the log entry. This value is used to identify the source
        /// of the log message.</param>
        /// <param name="msg">The message to log. If the object is not a string, its string representation is used.</param>
        public void Debug(string method, object msg)
            => Print(LogLevel.Debug, method, ObjectToString(msg));

        /// <summary>
        /// Writes a log entry at the Verbose level using the specified message object.
        /// </summary>
        /// <remarks>Use this method to record detailed diagnostic information that is useful for
        /// debugging. Verbose logs are typically disabled in production environments due to their high
        /// volume.</remarks>
        /// <param name="msg">The message object to log. If the object is not a string, its string representation is used. Can be null.</param>
        public void Verbose(object msg)
            => Print(LogLevel.Verbose, null, ObjectToString(msg));

        /// <summary>
        /// Logs a message at the verbose level, associating it with the specified method name.
        /// </summary>
        /// <param name="method">The name of the method or operation to associate with the log entry. Cannot be null.</param>
        /// <param name="msg">The message object to log. The object's string representation will be included in the log entry. Can be
        /// null.</param>
        public void Verbose(string method, object msg)
            => Print(LogLevel.Verbose, method, ObjectToString(msg));

        /// <summary>
        /// Writes an informational log entry with the specified message.
        /// </summary>
        /// <param name="msg">The message object to log. If the object is not a string, its string representation is used.</param>
        public void Info(object msg)
            => Print(LogLevel.Information, null, ObjectToString(msg));

        /// <summary>
        /// Writes an informational log entry with the specified method name and message.
        /// </summary>
        /// <param name="method">The name of the method or operation associated with the log entry. This value is included to help identify
        /// the source of the log message.</param>
        /// <param name="msg">The message object to log. The object's string representation is written to the log. Can be null.</param>
        public void Info(string method, object msg)
            => Print(LogLevel.Information, method, ObjectToString(msg));

        /// <summary>
        /// Logs a warning message with the specified content.
        /// </summary>
        /// <param name="msg">The message object to log. If the object is not a string, its string representation is used. Can be null.</param>
        public void Warn(object msg)
            => Print(LogLevel.Warning, null, ObjectToString(msg));

        /// <summary>
        /// Logs a warning message associated with the specified method name.
        /// </summary>
        /// <param name="method">The name of the method or operation related to the warning message. Cannot be null or empty.</param>
        /// <param name="msg">The message object to log. The object's string representation will be included in the log entry. Can be
        /// null.</param>
        public void Warn(string method, object msg)
            => Print(LogLevel.Warning, method, ObjectToString(msg));

        /// <summary>
        /// Logs an error message with the specified content.
        /// </summary>
        /// <param name="msg">The message object to log. If the object is not a string, its string representation is used. Can be null.</param>
        public void Error(object msg)
            => Print(LogLevel.Error, null, ObjectToString(msg));

        /// <summary>
        /// Logs an error message associated with the specified method name.
        /// </summary>
        /// <param name="method">The name of the method where the error occurred. This value is used to identify the source of the error in
        /// the log output.</param>
        /// <param name="msg">The error message or object to log. If an object is provided, its string representation is used.</param>
        public void Error(string method, object msg)
            => Print(LogLevel.Error, method, ObjectToString(msg));

        /// <summary>
        /// Writes a formatted log message with the specified log level, source method, and message content.
        /// </summary>
        /// <remarks>The log message is formatted with color tags and includes the log category and
        /// source. Messages with a log level that is currently disabled are not output. This method is intended for
        /// internal logging infrastructure and may not be thread-safe.</remarks>
        /// <param name="level">The severity level of the log message. Determines the formatting and whether the message is output based on
        /// the current log configuration.</param>
        /// <param name="method">The name of the method or operation generating the log message. Can be null or empty if not applicable.</param>
        /// <param name="message">The content of the log message to write. If null or empty, the method does not output a log entry.</param>
        public bool Print(LogLevel level, string? method, string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            // if ((AllowedLogs & level) != level)
            //    return false;

            if (RateLimit != null && !RateLimit.TryAcquire())
                return false;

            var source = Name;

            if (!string.IsNullOrWhiteSpace(method))
                source = string.Concat(source, " / ", method);

            var tagColor = TagColorForLevel(level);

            var tagText = TagTextForLevel(level);
            var tagTextColor = TagTextColorForLevel(level);

            var categoryTxt = string.Concat("&3[&r&7", Category, "&r&3]&r");
            var sourceTxt = string.Concat("&3[&r&7", source, "&r&3]&r");
            var tagTxt = string.Concat("&", tagColor, "[&r&", tagTextColor, tagText, "&r&", tagColor, "]&r");

            var msgTxt = string.Concat("&", tagTextColor, message, "&r");
            var msg = new LogMessage(this, DateTime.Now, level, categoryTxt, sourceTxt, tagTxt, msgTxt);

            LogManager.Print(ref msg);
            return true;
        }

        /// <summary>
        /// Returns the standardized text tag corresponding to the specified log level.
        /// </summary>
        /// <param name="level">The log level for which to retrieve the text tag.</param>
        /// <returns>A string representing the text tag for the specified log level. Returns "UNKNOWN" if the log level is not
        /// recognized.</returns>
        public static string TagTextForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => "ERROR",
                LogLevel.Fatal => "FATAL",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARNING",
                LogLevel.Debug => "DEBUG",
                LogLevel.Verbose => "VERBOSE",

                _ => "UNKNOWN",
            };
        }

        /// <summary>
        /// Returns the tag text color code associated with the specified log level.
        /// </summary>
        /// <param name="level">The log level for which to retrieve the tag text color code.</param>
        /// <returns>A string representing the color code for the specified log level. Returns "1" for critical log levels;
        /// otherwise, returns "7".</returns>
        public static string TagTextColorForLevel(LogLevel level)
        {
            if (level != LogLevel.Fatal)
                return "7";

            return "1";
        }

        /// <summary>
        /// Returns the tag color code associated with the specified log level.
        /// </summary>
        /// <param name="level">The log level for which to retrieve the corresponding tag color code.</param>
        /// <returns>A string representing the tag color code for the specified log level. Returns "4" for Error or Critical, "2"
        /// for Information, "3" for Warning, "6" for Debug, "5" for Verbose, and "7" for all other values.</returns>
        public static string TagColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error or LogLevel.Fatal => "1",
                LogLevel.Information => "2",
                LogLevel.Warning => "3",
                LogLevel.Debug => "6",
                LogLevel.Verbose => "5",

                _ => "7",
            };
        }

        /// <summary>
        /// Converts the specified object to its string representation, providing detailed information for exceptions.
        /// </summary>
        /// <remarks>If the object is a ReflectionTypeLoadException, the returned string includes the
        /// exception message, stack trace, and messages from all loader exceptions. For other exceptions, the full
        /// exception string is returned. For non-exception objects, the method returns the result of ToString(), or the
        /// string itself if the object is already a string.</remarks>
        /// <param name="obj">The object to convert to a string. Can be any type, including exceptions.</param>
        /// <returns>A string representation of the specified object. If the object is an exception, returns detailed exception
        /// information. Returns an empty string if the object is null.</returns>
        public static string ObjectToString(object obj)
        {
            if (obj == null)
                return string.Empty;

            if (obj is Exception exception)
            {
                if (exception is ReflectionTypeLoadException rtlEx)
                {
                    return string.Concat(
                        exception.Message, Environment.NewLine,
                        exception.StackTrace, Environment.NewLine,

                        string.Join(Environment.NewLine, rtlEx.LoaderExceptions.Select(t => t.Message)));
                }

                return exception.ToString();
            }

            if (obj is string str)
                return str;

            return obj.ToString() ?? string.Empty;
        }
    }
}