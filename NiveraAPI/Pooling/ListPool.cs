using NiveraAPI.Utilities;

namespace NiveraAPI.Pooling
{
    /// <summary>
    /// Provides a thread-safe pool for renting and returning lists of type T, optimizing memory usage and performance.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the lists managed by the pool.</typeparam>
    public class ListPool<T> : PoolBase<List<T>>
    {
        static ListPool()
            => StaticConstructor<List<T>>.Set(() => new());

        private static volatile ListPool<T> shared;

        /// <summary>
        /// Gets a shared, thread-safe instance of the list pool for the specified type parameter.
        /// </summary>
        public static new ListPool<T> Shared => shared ??= new();

        /// <summary>
        /// Rents a list with at least the specified capacity from the pool.
        /// </summary>
        /// <remarks>If the rented list's capacity is less than the specified value, its capacity is
        /// increased to meet the requirement. The contents of the list are not guaranteed to be empty; callers should
        /// clear the list if necessary before use.</remarks>
        /// <param name="capacity">The minimum number of elements that the rented list must be able to hold. Must be a positive integer.</param>
        /// <returns>A list of type T with a capacity greater than or equal to the specified value. The list may be resized if
        /// its current capacity is less than the requested capacity.</returns>
        public List<T> Rent(int capacity)
        {
            var list = Rent();

            if (list.Capacity < capacity)
                list.Capacity = capacity;

            return list;
        }

        /// <summary>
        /// Rents a list from the pool and populates it with the elements from the specified collection.
        /// </summary>
        /// <remarks>The returned list is obtained from the pool and should be returned to the pool when
        /// no longer needed. The method adds all elements from the provided collection to the rented list. If the
        /// collection is empty, the returned list will also be empty.</remarks>
        /// <param name="collection">The collection whose elements are added to the rented list. Cannot be null.</param>
        /// <returns>A list containing the elements from the specified collection.</returns>
        public List<T> Rent(IEnumerable<T> collection)
        {
            var list = Rent();

            list.AddRange(collection);
            return list;
        }

        /// <inheritdoc/>
        public override void OnPooled(List<T> value)
        {
            base.OnPooled(value);

            value.Clear();
        }

        /// <summary>
        /// Converts the specified list to an array and returns it to the pool.
        /// </summary>
        /// <param name="list">The list to convert to an array and return to the pool. Must not be null.</param>
        /// <returns>An array containing the elements of the specified list.</returns>
        public static T[] ReturnToArray(List<T> list)
        {
            var array = list.ToArray();

            Shared.Return(list);
            return array;
        }

        /// <summary>
        /// Applies the specified action to each element in the provided list.
        /// </summary>
        /// <remarks>This method uses a pooled list to optimize memory usage during the operation. The
        /// original list is not modified.</remarks>
        /// <param name="target">The list of elements to which the action will be applied. This parameter cannot be null.</param>
        /// <param name="action">The action to perform on each element of the list. The action is invoked once for each element.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
        public static void Modify(List<T> target, Action<int, T> action)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            var pooled = Shared.Rent(target);

            try
            {
                for (var x = 0; x < pooled.Count; x++)
                {
                    action(x, pooled[x]);
                }
            }
            finally
            {
                Shared.Return(pooled);
            }
        }
    }
}