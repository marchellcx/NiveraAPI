using FastGenericNew;

using NiveraAPI.Logs;

namespace NiveraAPI.Utilities;

/// <summary>
/// Provides a mechanism for initializing static constructors of generic types.
/// </summary>
/// <typeparam name="T">The type for which the static constructor is initialized.</typeparam>
public class StaticConstructor<T>
{
    private static volatile LogSink log = LogManager.GetSource("Utils", "StaticConstructor");
    
    /// <summary>
    /// A static variable that provides a mechanism for creating instances of the generic type <typeparamref name="T"/>.
    /// This delegate is initialized to use the FastNew library for efficient instance creation.
    /// </summary>
    public static volatile Func<T> Constructor = FastNew.CreateInstance<T>;

    /// <summary>
    /// Sets the constructor delegate for creating instances of the generic type.
    /// </summary>
    /// <param name="constructor">
    /// A delegate of type <see cref="Func{T}"/> that defines how instances of the generic type <typeparamref name="T"/> should be created.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="constructor"/> is null.
    /// </exception>
    public static void Set(Func<T> constructor)
    {
        if (constructor == null)
            throw new ArgumentNullException(nameof(constructor));
        
        Constructor = constructor;
        
        CacheNonGeneric();
    }

    /// <summary>
    /// Creates and returns an instance of the generic type <typeparamref name="T"/> by invoking the assigned constructor delegate.
    /// </summary>
    /// <returns>
    /// An instance of the generic type <typeparamref name="T"/> created using the current constructor delegate.
    /// </returns>
    public static T Construct()
        => Constructor();

    /// <summary>
    /// Attempts to construct an instance of the generic type <typeparamref name="T"/> using the specified constructor delegate.
    /// </summary>
    /// <param name="instance">
    /// When this method returns, contains the constructed instance of type <typeparamref name="T"/> if the operation was successful;
    /// otherwise, it contains the default value for the type <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the instance was successfully constructed; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryConstruct(out T instance)
    {
        try
        {
            instance = Construct();
            return instance != null;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to construct instance of &1{typeof(T)}&r:\n{ex}");
            
            instance = default!;
            return false;
        }
    }

    private static Func<object> CacheNonGeneric()
    {
        return StaticNonGenericConstructor.constructors[typeof(T)] = () => Constructor()!;
    }
}