namespace NiveraAPI.IO.Serialization
{
    /// <summary>
    /// Implements generic type serialization and deserialization.
    /// </summary>
    /// <typeparam name="T">The type to be handled.</typeparam>
    public static class ByteSerializer<T>
    {
        /// <summary>
        /// The delegate used to write data of the value to serialize to the writer.
        /// </summary>
        public static Action<ByteWriter, T> Serialize;

        /// <summary>
        /// The delegate used to read data of the value to deserialize from the reader.
        /// </summary>
        public static Func<ByteReader, T> Deserialize;
    }
}