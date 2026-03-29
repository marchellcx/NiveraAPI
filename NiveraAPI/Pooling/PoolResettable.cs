using NiveraAPI.Pooling.Interfaces;

namespace NiveraAPI.Pooling
{
    /// <summary>
    /// A base implementation of <see cref="IPoolResettable"/>
    /// </summary>
    public abstract class PoolResettable : IPoolResettable, IDisposable
    {
        /// <inheritdoc/>
        public bool IsPooled { get; set; }

        /// <inheritdoc/>
        public virtual void OnPooled() { }

        /// <inheritdoc/>
        public virtual void OnUnPooled() { }

        /// <summary>
        /// Places the object back into the pool for reuse by resetting its state and performing any necessary cleanup.
        /// This method must be implemented by derived classes to define specific reset behavior.
        /// </summary>
        /// <remarks>
        /// This method is intended to be called when the object is no longer in use and should be returned to a reusable state.
        /// Implementations should ensure that the object is properly prepared for its next usage and does not retain any stale references or data.
        /// </remarks>
        public abstract void ReturnToPool();

        /// <summary>
        /// Releases resources and returns the object to the pool if it is not already pooled.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the object is already pooled when attempting to dispose it.
        /// </exception>
        public void Dispose()
        {
            if (IsPooled)
                throw new ObjectDisposedException(GetType().Name);
            
            ReturnToPool();
        }
    }
}