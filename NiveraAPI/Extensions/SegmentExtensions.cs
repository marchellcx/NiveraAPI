namespace NiveraAPI.Extensions
{
    /// <summary>
    /// Extensions targeting the <see cref="ArraySegment{T}"/> struct.
    /// </summary>
    public static class SegmentExtensions
    {
        /// <summary>
        /// Gets a value at a specific index in an array segmnet.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="segment">The target array segment.</param>
        /// <param name="index">The target index.</param>
        /// <returns>The value at the specified index.</returns>
        public static T At<T>(this ArraySegment<T> segment, int index)
            => segment.Array[segment.Offset + index];

        /// <summary>
        /// Gets a new segment from an existing array.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="array">The source array.</param>
        /// <param name="offset">The start offset index.</param>
        /// <param name="count">The size of the segment.</param>
        /// <returns>The new segment.</returns>
        public static ArraySegment<T> ToSegment<T>(this T[] array, int offset, int count)
            => new(array, offset, count);

        /// <summary>
        /// Gets a new segment from an existing one.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="segment">The source array segment.</param>
        /// <param name="offset">The start offset index.</param>
        /// <param name="count">The size of the segment.</param>
        /// <returns>The new segment.</returns>
        public static ArraySegment<T> ToSegment<T>(this ArraySegment<T> segment, int offset, int count)
            => new ArraySegment<T>(segment.Array, segment.Offset + offset, count);
    }
}