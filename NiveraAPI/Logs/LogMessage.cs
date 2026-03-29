namespace NiveraAPI.Logs
{
    /// <summary>
    /// Represents a log message.
    /// </summary>
    public struct LogMessage
    {
        /// <summary>
        /// The sink this message originated from.
        /// </summary>
        public LogSink Sink;

        /// <summary>
        /// The severity level of the log.
        /// </summary>
        public LogLevel Level;

        /// <summary>
        /// The UTC-time of the log being created.
        /// </summary>
        public DateTime Time;

        /// <summary>
        /// The text of the category tag.
        /// </summary>
        public string CategoryText;

        /// <summary>
        /// The text of the level tag.
        /// </summary>
        public string LevelText;

        /// <summary>
        /// The text of the source tag.
        /// </summary>
        public string SourceText;

        /// <summary>
        /// The text of the message.
        /// </summary>
        public string MessageText;

        /// <summary>
        /// Initializes a new instance of the LogMessage class with the specified log sink, timestamp, log level,
        /// category, source, level text, and message text.
        /// </summary>
        /// <param name="sink">The LogSink instance that will receive the log message. This parameter cannot be null.</param>
        /// <param name="time">The date and time when the log message was created.</param>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="categoryText">A string that specifies the category associated with the log message.</param>
        /// <param name="sourceText">A string that identifies the source of the log message.</param>
        /// <param name="levelText">A string representation of the log level.</param>
        /// <param name="messageText">The main content of the log message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the sink parameter is null.</exception>
        public LogMessage(LogSink sink, DateTime time, LogLevel level, string categoryText, string sourceText, 
            string levelText, string messageText)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            Sink = sink;

            Time = time;
            Level = level;

            CategoryText = categoryText;
            SourceText = sourceText;
            LevelText = levelText;
            MessageText = messageText;
        }

        /// <summary>
        /// Returns a string that represents the current log message, combining the level, category, source, and message
        /// text values. Does NOT include time!
        /// </summary>
        /// <returns>A string containing the log level, category, source, and message text, separated by spaces.</returns>
        public override string ToString()
        {
            return string.Concat(
                LevelText, " ",
                CategoryText, " ",
                SourceText, " ",
                MessageText);
        }
    }
}