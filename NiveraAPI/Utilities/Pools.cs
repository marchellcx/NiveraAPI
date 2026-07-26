using System.Text;
using NiveraAPI.Extensions;
using NiveraAPI.Pooling;

namespace NiveraAPI.Utilities;

/// <summary>
/// Provides utility methods to rent and return commonly used object types such as
/// <see cref="List{T}"/>, <see cref="Dictionary{TKey, TValue}"/>, and <see cref="StringBuilder"/>
/// from shared object pools for optimizing memory usage and reducing garbage collection overhead.
/// </summary>
public static class Pools
{
	/// <summary>
	/// Rents an empty <see cref="List{T}"/> instance from the shared pool.
	/// </summary>
	/// <typeparam name="T">The type of elements contained in the list.</typeparam>
	/// <returns>A <see cref="List{T}"/> instance from the pool. The list is returned in an empty state and ready for use.</returns>
	public static List<T> PoolList<T>()
		=> ListPool<T>.Shared.Rent();

	/// <summary>
	/// Rents a <see cref="List{T}"/> instance from the shared pool and ensures it has at least the specified capacity.
	/// </summary>
	/// <param name="size">The minimum capacity of the list to be rented. Must be a positive integer.</param>
	/// <typeparam name="T">The type of elements contained in the list.</typeparam>
	/// <returns>A <see cref="List{T}"/> instance with a capacity greater than or equal to the specified size. The list may be resized if its current capacity is less than the requested size.</returns>
	public static List<T> PoolList<T>(int size)
		=> ListPool<T>.Shared.Rent(size);

	/// <summary>
	/// Rents a <see cref="List{T}"/> instance from the shared pool and populates it with the specified values.
	/// </summary>
	/// <param name="values">The collection of values to populate the rented list with.</param>
	/// <typeparam name="T">The type of elements contained in the list.</typeparam>
	/// <returns>A <see cref="List{T}"/> instance containing the specified values.</returns>
	public static List<T> PoolList<T>(IEnumerable<T> values)
		=> ListPool<T>.Shared.Rent(values);

	/// <summary>
	/// Returns a previously rented <see cref="List{T}"/> instance to the shared pool.
	/// </summary>
	/// <param name="list">The <see cref="List{T}"/> instance to be returned to the pool.</param>
	/// <typeparam name="T">The type of elements contained in the list.</typeparam>
	public static void ReturnList<T>(this List<T> list)
		=> ListPool<T>.Shared.Return(list);

	/// <summary>
	/// Rents a <see cref="Dictionary{TKey, TValue}"/> instance from the shared pool.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	/// <returns>A <see cref="Dictionary{TKey, TValue}"/> instance rented from the pool.</returns>
	public static Dictionary<TKey, TValue> PoolDictionary<TKey, TValue>()
		=> DictionaryPool<TKey, TValue>.Shared.Rent();

	/// <summary>
	/// Rents a <see cref="Dictionary{TKey, TValue}"/> instance from the shared pool and populates it with the contents of the specified dictionary.
	/// </summary>
	/// <param name="dict">The dictionary whose elements will be copied into the rented dictionary.</param>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	/// <returns>A <see cref="Dictionary{TKey, TValue}"/> instance rented from the pool, containing all elements from the input dictionary.</returns>
	public static Dictionary<TKey, TValue> PoolDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
		=> DictionaryPool<TKey, TValue>.Shared.Rent(dict);

	/// <summary>
	/// Returns a previously rented <see cref="Dictionary{TKey, TValue}"/> instance to the shared pool for reuse.
	/// </summary>
	/// <param name="dict">The <see cref="Dictionary{TKey, TValue}"/> instance to return to the pool.</param>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	public static void ReturnDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dict)
		=> DictionaryPool<TKey, TValue>.Shared.Return(dict);

	/// <summary>
	/// Retrieves a reusable <see cref="StringBuilder"/> instance from the shared pool.
	/// </summary>
	/// <returns>A <see cref="StringBuilder"/> instance for use.</returns>
	public static StringBuilder PoolStringBuilder()
		=> StringBuilderPool.Shared.Rent();

	/// <summary>
	/// Rents and returns a new <see cref="StringBuilder"/> instance from the shared pool for reuse.
	/// </summary>
	/// <param name="size">The initial capacity to set for the <see cref="StringBuilder"/> instance.</param>
	/// <returns>A <see cref="StringBuilder"/> instance with the specified initial capacity.</returns>
	public static StringBuilder PoolStringBuilder(int size)
	{
		var stringBuilder = PoolStringBuilder();
		
		stringBuilder.Capacity = size;
		return stringBuilder;
	}

	/// <summary>
	/// Rents a new <see cref="StringBuilder"/> instance from the shared pool for reuse.
	/// </summary>
	/// <param name="newLine">A boolean value determining whether to append a new line after each initial string.</param>
	/// <param name="initial">An optional set of strings to initialize the <see cref="StringBuilder"/> with.</param>
	/// <returns>A <see cref="StringBuilder"/> instance populated with the given strings based on the <paramref name="newLine"/> setting.</returns>
	public static StringBuilder PoolStringBuilder(bool newLine = true, params string[] initial)
	{
		var builder = PoolStringBuilder();
		
		initial.ForEach(str =>
		{
			if (newLine)
			{
				builder.AppendLine(str);
			}
			else
			{
				builder.Append(str);
			}
		});
		
		return builder;
	}

	/// <summary>
	/// Returns a previously rented <see cref="StringBuilder"/> instance back to the pool for reuse.
	/// </summary>
	/// <param name="stringBuilder">The <see cref="StringBuilder"/> instance to return to the pool.</param>
	/// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="stringBuilder"/> is null.</exception>
	public static void ReturnStringBuilder(this StringBuilder stringBuilder)	
		=> StringBuilderPool.Shared.Return(stringBuilder);

	/// <summary>
	/// Returns the string value of the content within the specified <see cref="StringBuilder"/> instance,
	/// and returns the <see cref="StringBuilder"/> back to the pool for reuse.
	/// </summary>
	/// <param name="stringBuilder">The <see cref="StringBuilder"/> instance to extract the string value from and return to the pool.</param>
	/// <returns>The string representation of the content in the specified <see cref="StringBuilder"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="stringBuilder"/> is null.</exception>
	public static string ReturnStringBuilderValue(this StringBuilder stringBuilder)
		=> StringBuilderPool.Shared.ReturnToString(stringBuilder);
}