using System.Text;

namespace NiveraAPI.Pooling
{
    /// <summary>
    /// Provides a centralized pool for managing StringBuilder instances, allowing for efficient reuse and reducing
    /// memory allocations.
    /// </summary>
    /// <remarks>This class is designed to improve performance in scenarios where multiple StringBuilder
    /// instances are frequently created and discarded.</remarks>
    public class StringBuilderPool : PoolBase<StringBuilder>
    {
        private static volatile StringBuilderPool shared = new();

        /// <summary>
        /// Gets the shared instance of the StringBuilderPool, providing a centralized pool for managing StringBuilder
        /// instances.
        /// </summary>
        /// <remarks>This property allows for efficient reuse of StringBuilder objects, reducing memory
        /// allocations and improving performance in scenarios where multiple StringBuilder instances are frequently
        /// created and discarded.</remarks>
        public static new StringBuilderPool Shared => shared;

        /// <summary>
        /// Returns the string representation of the specified StringBuilder and returns the builder to the pool.
        /// </summary>
        /// <remarks>After calling this method, the provided StringBuilder instance should not be used, as
        /// it is returned to the pool and may be reused elsewhere.</remarks>
        /// <param name="builder">The StringBuilder instance whose contents are to be converted to a string and then returned to the pool.
        /// This parameter cannot be null.</param>
        /// <returns>A string containing the contents of the specified StringBuilder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the builder parameter is null.</exception>
        public string ReturnToString(StringBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var str = builder.ToString();

            Return(builder);
            return str;
        }

        /// <inheritdoc/>
        public override void OnPooled(StringBuilder value)
        {
            base.OnPooled(value);

            value.Clear();
        }

        /// <summary>
        /// Builds and returns a string by applying the specified action to a pooled StringBuilder instance.
        /// </summary>
        /// <remarks>This method uses a pooled StringBuilder instance for improved memory efficiency. The
        /// StringBuilder is automatically returned to the pool after use.</remarks>
        /// <param name="buildAction">An action that receives a StringBuilder to construct the string. This parameter cannot be null.</param>
        /// <returns>A string containing the result of the operations performed by the specified action.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="buildAction"/> is null.</exception>
        public static string BuildString(Action<StringBuilder> buildAction)
        {
            if (buildAction == null)
                throw new ArgumentNullException(nameof(buildAction));

            var builder = Shared.Rent();

            try
            {
                buildAction(builder);
                return builder.ToString();
            }
            finally
            {
                Shared.Return(builder);
            }
        }
    }
}