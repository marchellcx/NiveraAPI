using NiveraAPI.Extensions;
using NiveraAPI.Logs;

namespace NiveraAPI.Utilities;

/// <summary>
/// Provides a mechanism for creating instances of types using a static cached constructor strategy.
/// </summary>
public static class StaticNonGenericConstructor
{
    private static LogSink log = LogManager.GetSource("Utils", "StaticNonGenericConstructor");
    
    internal static Dictionary<Type, Func<object>> constructors = new();

    /// <summary>
    /// Provides a read-only dictionary containing constructors associated with their respective types.
    /// Each entry maps a <see cref="Type"/> to a <see cref="Func{TResult}"/> that creates an instance of that type.
    /// </summary>
    public static IReadOnlyDictionary<Type, Func<object>> Constructors => constructors;

    /// <summary>
    /// Retrieves the constructor delegate for the specified type from a static cached mechanism.
    /// </summary>
    /// <param name="type">The type for which the constructor delegate is to be retrieved. Must not be null.</param>
    /// <returns>A delegate that can create an instance of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="type"/> is null.</exception>
    public static Func<object> GetConstructor(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "Type cannot be null.");

        if (constructors.TryGetValue(type, out var constructor))
            return constructor;
        
        // This will call StaticConstructor.CacheNonGeneric() which will add the current Constructor field
        // to the constructors dictionary.
        
        var constructorType = typeof(StaticConstructor<>).MakeGenericType(type);
        var constructorObj = constructorType.FindMethod("CacheNonGeneric").Invoke(null, null) as Func<object>;

        return constructorObj;
    }
    
    /// <summary>
    /// Constructs an instance of the specified type using a static cached constructor mechanism.
    /// </summary>
    /// <param name="type">The type of the object to be constructed. Must not be null.</param>
    /// <returns>An instance of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided type is null.</exception>
    public static object Construct(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "Type cannot be null.");

        if (constructors.TryGetValue(type, out var constructor))
            return constructor();
        
        // This will call StaticConstructor.CacheNonGeneric() which will add the current Constructor field
        // to the constructors dictionary.
        
        var constructorType = typeof(StaticConstructor<>).MakeGenericType(type);
        var constructorObj = constructorType.FindMethod("CacheNonGeneric").Invoke(null, null) as Func<object>;

        return constructorObj();
    }

    /// <summary>
    /// Attempts to construct an instance of the specified type using a static cached constructor mechanism.
    /// </summary>
    /// <param name="type">The type of the object to be constructed. Must not be null.</param>
    /// <param name="obj">When this method returns, contains the constructed instance of the specified type if the operation was successful; otherwise, null.</param>
    /// <returns>True if the instance was successfully constructed; otherwise, false.</returns>
    public static bool TryConstruct(Type type, out object obj)
    {
        try
        {
            obj = Construct(type);
            return obj != null;
        }
        catch (Exception ex)
        {
            log.Error(ex);

            obj = null!;
            return false;
        }
    }
}