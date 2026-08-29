using NiveraAPI.Extensions;

namespace NiveraAPI.Utilities;

/// <summary>
/// Provides utility methods for asynchronous value monitoring and waiting for specified conditions to be met.
/// </summary>
public static class AsyncHelper
{
    /// <summary>
    /// Waits asynchronously until the specified predicate evaluates to true for the value
    /// returned by the provided getter function. Periodically checks the value with a specified interval.
    /// </summary>
    /// <typeparam name="T">The type of the value returned by the getter function.</typeparam>
    /// <param name="getter">A function that retrieves the value to be checked.</param>
    /// <param name="predicate">A predicate that determines whether the awaited condition is met.</param>
    /// <param name="interval">The interval, in milliseconds, at which the value is checked. Default is 10 milliseconds.</param>
    /// <returns>A task that represents the asynchronous operation. The task is completed when the predicate evaluates to true.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the getter or predicate is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the interval is less than or equal to zero.</exception>
    public static async Task AwaitValueChangeAsync<T>(Func<T> getter, Predicate<T> predicate, int interval = 10)
    {
        if (getter == null)
            throw new ArgumentNullException(nameof(getter));

        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        if (interval <= 0)
            throw new ArgumentOutOfRangeException(nameof(interval));

        while (true)
        {
            var value = getter();

            if (predicate(value))
                break;
            
            await Task.Delay(interval);
        }
    }

    /// <summary>
    /// Waits asynchronously until the value obtained from the getter function is equal to the specified required value.
    /// The comparison is performed using the custom equality logic provided by the `IsEqualTo` extension method.
    /// Periodically checks the value with a specified interval.
    /// </summary>
    /// <typeparam name="T">The type of the value returned by the getter function and the required value.</typeparam>
    /// <param name="getter">A function that retrieves the current value to be checked.</param>
    /// <param name="required">The value that the retrieved value is expected to match.</param>
    /// <param name="interval">The interval, in milliseconds, at which the value is checked. Default is 10 milliseconds.</param>
    /// <returns>A task that represents the asynchronous operation. The task completes when the retrieved value matches the required value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the getter is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the interval is less than or equal to zero.</exception>
    public static async Task AwaitValueChangeAsync<T>(Func<T> getter, T required, int interval = 10)
    {
        if (getter == null)
            throw new ArgumentNullException(nameof(getter));

        if (interval <= 0)
            throw new ArgumentOutOfRangeException(nameof(interval));

        while (true)
        {
            var value = getter();
            
            if (value.IsEqualTo(required))
                break;
            
            await Task.Delay(interval);
        }
    }
}