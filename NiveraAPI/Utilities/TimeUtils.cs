namespace NiveraAPI.Utilities;

/// <summary>
/// Utilities for working with time.
/// </summary>
public static class TimeUtils
{
    /// <summary>
    /// Gets the current number of ticks that have elapsed since 12:00:00 midnight, January 1, 0001,
    /// according to the local time zone.
    /// </summary>
    /// <remarks>
    /// Ticks represent the smallest unit of time in .NET, equal to 100 nanoseconds.
    /// This property retrieves the value based on the local system time.
    /// </remarks>
    public static long CurTicks => DateTime.Now.Ticks;

    /// <summary>
    /// Gets the current number of ticks that have elapsed since 12:00:00 midnight, January 1, 0001,
    /// in Coordinated Universal Time (UTC).
    /// </summary>
    public static long CurUtcTicks => DateTime.UtcNow.Ticks;

    /// <summary>
    /// Gets the current number of ticks that have elapsed since 12:00:00 midnight, January 1, 0001,
    /// adjusted to the local time zone.
    /// </summary>
    public static long CurLocalTicks => DateTime.Now.ToLocalTime().Ticks;

    /// <summary>
    /// Gets the current Unix timestamp representing the number of seconds
    /// that have elapsed since January 1, 1970 (00:00:00 UTC).
    /// </summary>
    public static long CurUnixTimeStamp => DateTimeOffset.Now.ToUnixTimeSeconds();

    /// <summary>
    /// Gets the current Unix timestamp representing the number of seconds that have elapsed since
    /// January 1, 1970 (midnight UTC/GMT), based on the current Coordinated Universal Time (UTC).
    /// </summary>
    public static long CurUtcUnixTimeStamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// Calculates the difference between two tick values in seconds.
    /// </summary>
    /// <param name="ticks1">The first tick value.</param>
    /// <param name="ticks2">The second tick value.</param>
    /// <returns>The difference between the two tick values in seconds.</returns>
    public static long TicksDiffSeconds(long ticks1, long ticks2)
        => (ticks1 - ticks2) / TimeSpan.TicksPerSecond;

    /// <summary>
    /// Calculates the difference between two tick values in milliseconds.
    /// </summary>
    /// <param name="ticks1">The first tick value.</param>
    /// <param name="ticks2">The second tick value.</param>
    /// <returns>The difference between the two tick values in milliseconds.</returns>
    public static long TicksDiffMilliseconds(long ticks1, long ticks2)
        => (ticks1 - ticks2) / TimeSpan.TicksPerMillisecond;
}