using System.Collections.Concurrent;

using FastGenericNew;

using NiveraAPI.Pooling.Interfaces;
using NiveraAPI.Utilities;

namespace NiveraAPI.Pooling
{
    /// <summary>
    /// Base for an object pool.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    public class PoolBase<T> where T : class
    {
        private static volatile PoolBase<T> shared = new();
        
        /// <summary>
        /// Gets the shared pool instance.
        /// </summary>
        public static PoolBase<T> Shared => shared;

        private volatile ConcurrentQueue<T> queue = new();

        /// <summary>
        /// Gets the number of objects in the pool.
        /// </summary>
        public int Size => queue.Count;

        /// <summary>
        /// Rents an item from the pool.
        /// </summary>
        /// <returns>The rented item.</returns>
        public T Rent(Action<T>? configure = null)
        {
            if (TryRent(out var item))
                return item;

            item = Construct();

            if (item is IPoolResettable poolResettable)
            {
                poolResettable.IsPooled = false;
                poolResettable.OnUnPooled();
            }

            OnUnPooled(item);

            configure?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Attempts to rent an object from the pool.
        /// </summary>
        /// <param name="value">The rented object instance (or null if the pool is empty).</param>
        /// <returns>true if an instance was rented</returns>
        public bool TryRent(out T value)
        {
            value = default!;

            if (!queue.TryDequeue(out var result))
                return false;

            OnUnPooled(result);

            if (result is IPoolResettable poolResettable)
            {
                poolResettable.IsPooled = false;
                poolResettable.OnUnPooled();
            }

            value = result;
            return true;
        }

        /// <summary>
        /// Attempts to rent an object from the pool.
        /// </summary>
        /// <param name="configure">Delegate used to configure the rented item.</param>
        /// <param name="value">The rented object instance (or null if the pool is empty).</param>
        /// <returns>true if an instance was rented</returns>
        public bool TryRent(Action<T> configure, out T value)
        {
            if (configure is null)
                throw new ArgumentNullException(nameof(configure));

            value = default!;

            if (!queue.TryDequeue(out var result))
                return false;

            OnUnPooled(result);

            if (result is IPoolResettable poolResettable)
            {
                poolResettable.IsPooled = false;
                poolResettable.OnUnPooled();
            }

            configure(result);

            value = result;
            return true;
        }

        /// <summary>
        /// Returns a previously rented object to the pool.
        /// </summary>
        /// <param name="value">The rented object.</param>
        /// <exception cref="ArgumentNullException">value is null</exception>
        public void Return(T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (value is IPoolResettable poolResettable)
            {
                poolResettable.OnPooled();
                poolResettable.IsPooled = true;
            }

            OnPooled(value);

            queue.Enqueue(value);
        }

        /// <summary>
        /// Gets called once a value is added to the pool.
        /// </summary>
        /// <param name="value">The added value.</param>
        public virtual void OnPooled(T value) { }

        /// <summary>
        /// Gets called once a value is removed from the pool.
        /// </summary>
        /// <param name="value">The removed value.</param>
        public virtual void OnUnPooled(T value) { }

        /// <summary>
        /// Creates a new instance of the object.
        /// </summary>
        /// <returns>The created instance.</returns>
        public virtual T Construct() => StaticConstructor<T>.Construct();
    }
}