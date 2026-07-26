namespace NiveraAPI.Utilities;

/// <summary>
/// A generic implementation of the Singleton pattern that provides a single,
/// globally accessible instance of a specified type.
/// </summary>
/// <typeparam name="T">The class type for which the singleton instance is created. This must be a reference type.</typeparam>
public static class Singleton<T> where T : class
{
    private static T? value;

    /// <summary>
    /// Gets or sets the instance of the singleton type.
    /// This ensures a single shared instance of the specified type is managed.
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown when attempting to get the value without initializing it first.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when attempting to set the value to null.
    /// </exception>
    /// <remarks>
    /// Setting the value is only allowed once, and any subsequent set operations are ignored.
    /// The singleton instance can be cleared by using the <c>Remove</c> method,
    /// allowing for re-initialization if necessary.
    /// </remarks>
    public static T Value
    {
        get
        {
            if (value == null)
                throw new Exception($"Singleton for type {typeof(T)} has not been initialized.");

            return value;
        }
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            
            if (Singleton<T>.value != null)
                return;

            Singleton<T>.value = value;
        }
    }

    /// <summary>
    /// Removes the current instance of the singleton, resetting its value to null.
    /// This allows the singleton to be reinitialized with a new instance.
    /// </summary>
    public static void Remove()
    {
        value = null;
    }
}