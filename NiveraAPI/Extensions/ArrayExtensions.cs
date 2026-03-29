using NiveraAPI.Pooling;

namespace NiveraAPI.Extensions
{
    /// <summary>
    /// Extensions targeting arrays.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Concatenates the specified arrays into a single array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the arrays.</typeparam>
        /// <param name="array">The base array to start the concatenation.</param>
        /// <param name="items">One or more arrays to concatenate to the base array.</param>
        /// <returns>A new array containing the elements of the base array followed by the elements of the specified arrays.</returns>
        public static T[] ConcatArray<T>(this T[] array, params T[][] items)
        {
            var list = ListPool<T>.Shared.Rent(array);

            for (var x = 0; x < items.Length; x++)
                list.AddRange(items[x]);
            
            var newArray = list.ToArray();
            
            ListPool<T>.Shared.Return(list);
            return newArray;
        }
        
        /// <summary>
        /// Finds an index of an item in an array.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="array">The target array.</param>
        /// <param name="item">The item to find the index of.</param>
        /// <returns>The item's zero-based index if found, otherwise <see langword="null"/></returns>
        public static int IndexOf<T>(this T[] array, T item)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            return Array.IndexOf(array, item);
        }
    }
}