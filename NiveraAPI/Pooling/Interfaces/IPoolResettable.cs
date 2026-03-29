namespace NiveraAPI.Pooling.Interfaces
{
    /// <summary>
    /// Describes additional methods for poolable objects.
    /// </summary>
    public interface IPoolResettable
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not the object is currently in a pool.
        /// </summary>
        bool IsPooled { get; set; }

        /// <summary>
        /// Gets called once the object is added to a pool.
        /// </summary>
        void OnPooled();

        /// <summary>
        /// Gets called once the object is removed from a pool.
        /// </summary>
        void OnUnPooled();
    }
}