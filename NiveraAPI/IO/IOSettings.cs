namespace NiveraAPI.IO
{
    /// <summary>
    /// Used to modify options for the IO API.
    /// </summary>
    public static class IOSettings
    {
        /// <summary>
        /// The default size of the writer's buffer.
        /// </summary>
        public static int BYTE_WRITER_BUFFER_INIT_SIZE = 1024;

        /// <summary>
        /// Multiplier used when resizing the writer's buffer.
        /// </summary>
        public static int BYTE_WRITER_BUFFER_RESIZE_MULT = 2;

        /// <summary>
        /// Whether or not to allow resizing of the writer buffer, if required.
        /// </summary>
        public static bool BYTE_WRITER_BUFFER_RESIZING = true;
    }
}