namespace NiveraAPI.Logs
{
    /// <summary>
    /// Describes the severity of a log message.
    /// </summary>
    [Flags]
    public enum LogLevel
    {
        /// <summary>
        /// Most-detailed messages, often a lot of them, and not usually needed unless diagnosing problems.
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Less detailed than Debug, but still more detailed than Information. Useful for diagnosing problems, but not usually needed in normal operation.
        /// </summary>
        Verbose = 1,

        /// <summary>
        /// Informational messages that highlight the progress of the application at a coarse-grained level.
        /// </summary>
        Information = 2,

        /// <summary>
        /// Potentially harmful situations that indicate a possible problem.
        /// </summary>
        Warning = 4,

        /// <summary>
        /// Error events that might still allow the application to continue running.
        /// </summary>
        Error = 8,

        /// <summary>
        /// Fatal errors that will presumably lead the application to abort.
        /// </summary>
        Fatal = 16
    }
}