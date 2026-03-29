using NiveraAPI.Utilities;

namespace NiveraAPI.Pooling
{
    /// <summary>
    /// Provides a thread-safe pool for renting and returning simple arrays of type T, optimizing memory usage and performance.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the lists managed by the pool.</typeparam>
    public class SimpleArrayPool<T> : PoolBase<SimpleArray<T>>
    {
        static SimpleArrayPool()
            => StaticConstructor<SimpleArray<T>>.Constructor = () => new SimpleArray<T>();

        private static volatile SimpleArrayPool<T> shared;

        /// <summary>
        /// Gets a shared, thread-safe instance of the list pool for the specified type parameter.
        /// </summary>
        public new static SimpleArrayPool<T> Shared => shared ??= new();

        /// <summary>
        /// Rents a list with at least the specified capacity from the pool.
        /// </summary>
        /// <remarks>If the rented list's capacity is less than the specified value, its capacity is
        /// increased to meet the requirement. The contents of the list are not guaranteed to be empty; callers should
        /// clear the list if necessary before use.</remarks>
        /// <param name="capacity">The minimum number of elements that the rented list must be able to hold. Must be a positive integer.</param>
        /// <returns>A list of type T with a capacity greater than or equal to the specified value. The list may be resized if
        /// its current capacity is less than the requested capacity.</returns>
        public SimpleArray<T> Rent(int capacity)
        {
            var list = Rent();

            if (list.Capacity < capacity)
                list.Capacity = capacity;

            return list;
        }

        /// <inheritdoc/>
        public override void OnPooled(SimpleArray<T> value)
        {
            base.OnPooled(value);

            value.Clear();
        }
    }
}