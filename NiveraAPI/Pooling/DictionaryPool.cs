using NiveraAPI.Extensions;
using NiveraAPI.Utilities;

namespace NiveraAPI.Pooling;

/// <summary>
/// Represents a pool specifically designed to manage reusable dictionaries of specified key and value types,
/// leveraging object pooling for optimized memory management.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionaries managed by the pool.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionaries managed by the pool.</typeparam>
public class DictionaryPool<TKey, TValue> : PoolBase<Dictionary<TKey, TValue>>
{
    static DictionaryPool()
        => StaticConstructor<Dictionary<TKey, TValue>>.Set(() => new());

    private static volatile DictionaryPool<TKey, TValue> shared;

    /// <summary>
    /// Provides a singleton instance of the <see cref="DictionaryPool{TKey, TValue}"/> class,
    /// ensuring thread-safe access to a shared dictionary pool for managing dictionaries of the specified types.
    /// </summary>
    /// <remarks>
    /// The shared instance allows for optimized memory utilization by reusing dictionary objects
    /// across multiple operations, reducing the need for frequent memory allocations and collections.
    /// </remarks>
    /// <value>
    /// A thread-safe, lazily-initialized instance of <see cref="DictionaryPool{TKey, TValue}"/>.
    /// </value>
    public static new DictionaryPool<TKey, TValue> Shared => shared ??= new();

    /// <summary>
    /// Rents a dictionary from the pool and populates it with the contents of the specified dictionary.
    /// </summary>
    /// <param name="dict">The dictionary whose elements will be copied into the rented dictionary.</param>
    /// <returns>A dictionary rented from the pool with all elements from the input dictionary.</returns>
    public Dictionary<TKey, TValue> Rent(IDictionary<TKey, TValue> dict)
    {
        var newDict = Rent();

        newDict.AddRange(dict);
        return newDict;
    }

    /// <inheritdoc/>
    public override void OnPooled(Dictionary<TKey, TValue> value)
    {
        base.OnPooled(value);

        value.Clear();
    }

    /// <summary>
    /// Applies the specified action to each element in the provided list.
    /// </summary>
    /// <remarks>This method uses a pooled list to optimize memory usage during the operation. The
    /// original list is not modified.</remarks>
    /// <param name="target">The list of elements to which the action will be applied. This parameter cannot be null.</param>
    /// <param name="action">The action to perform on each element of the list. The action is invoked once for each element.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
    public static void Modify(Dictionary<TKey, TValue> target, Action<int, KeyValuePair<TKey, TValue>> action)
    {
        if (target == null)
            throw new ArgumentNullException("target");

        var pooled = Shared.Rent(target);

        try
        {
            var index = 0;

            foreach (var kvp in pooled)
            {
                action(index++, kvp);
            }
        }
        finally
        {
            Shared.Return(pooled);
        }
    }
}