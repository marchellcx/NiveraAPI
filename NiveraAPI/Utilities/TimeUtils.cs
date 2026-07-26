using NiveraAPI.Extensions;
using NiveraAPI.Results;

namespace NiveraAPI.Utilities;

/// <summary>
/// Utilities for working with time.
/// </summary>
public static class TimeUtils
{
    /// <summary>
    /// Gets the current date and time as a <see cref="DateTime"/> object, reflecting the system's local time.
    /// </summary>
    public static DateTime Time => DateTime.Now;

    /// <summary>
    /// Gets the current date and time as a <see cref="DateTime"/> object in Coordinated Universal Time (UTC).
    /// </summary>
    public static DateTime UtcTime => DateTime.UtcNow;
    
    /// <summary>
    /// Gets the current local date and time as a <see cref="DateTime"/> object,
    /// representing the system's local time zone.
    /// </summary>
    public static DateTime LocalTime => DateTime.Now.ToLocalTime();

    /// <summary>
    /// Gets the universal coordinated time (UTC) as a <see cref="DateTime"/> object.
    /// </summary>
    public static DateTime UniversalTime => DateTime.Now.ToUniversalTime();

    /// <summary>
    /// Gets the current date and time as a formatted string representation using the "F" format specifier,
    /// reflecting the system's local time.
    /// </summary>
    public static string StringFull => Time.ToString("F");

    /// <summary>
    /// Gets the current UTC date and time as a full string representation, formatted using the "F" standard date and time format specifier.
    /// </summary>
    public static string UtcStringFull => UtcTime.ToString("F");

    /// <summary>
    /// Gets the local date and time as a formatted string in the "F" format, representing the full date and time pattern of the system's current culture.
    /// </summary>
    public static string LocalStringFull => LocalTime.ToString("F");

    /// <summary>
    /// Gets the universal time as a formatted string representing a full date and time pattern.
    /// </summary>
    public static string UniversalStringFull => UniversalTime.ToString("F");
    
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

    /// <summary>
    /// Converts a TimeSpan into a human-readable string representation.
    /// </summary>
    /// <param name="span">The TimeSpan to convert.</param>
    /// <param name="maxDays">A flag indicating whether to include only days and smaller units or to distribute days into hours. Default is false.</param>
    /// <returns>A human-readable string representing the TimeSpan, using appropriate time units.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the TimeSpan contains negative seconds.</exception>
    public static string UserFriendlySpan(this TimeSpan span, bool maxDays = false)
    {
        if (span.Seconds < 0)
            throw new ArgumentOutOfRangeException("span");

        if (span.TotalSeconds == 0.0)
            return "0 sec";
        
        var parts = new int[6]
        {
            span.Days / 365,
            span.Days % 365 / 31,
            span.Days % 365 % 31,
            span.Hours,
            span.Minutes,
            span.Seconds
        };
        
        var units = new string[6] { " year", " month", " day", " hour", " minute", " second" };
        
        if (maxDays)
        {
            parts[0] = 0;
            parts[1] = 0;
            parts[2] = 0;
            parts[3] += span.Days * 24;
        }
        
        return string.Join(", ", from index in Enumerable.Range(0, units.Length)
            where parts[index] > 0
            select parts[index] + ((parts[index] == 1) ? units[index] : (units[index] + "s")));
    }

    /// <summary>
    /// Attempts to parse a time string into a TimeSpan object.
    /// </summary>
    /// <param name="time">The time string to parse.</param>
    /// <param name="result">
    /// When the method returns, contains the TimeSpan object if the parsing is successful,
    /// or <c>default</c> if the parsing fails.
    /// </param>
    /// <returns><c>true</c> if the parsing is successful; otherwise, <c>false</c>.</returns>
    public static bool TryParseTime(string time, out TimeSpan result)
        => ParseTime(time).TryReadResult(true, out result);

    /// <summary>
    /// Parses a time string into an <see cref="IResult"/> representing a TimeSpan object.
    /// </summary>
    /// <param name="time">The input time string to be parsed.</param>
    /// <returns>An <see cref="IResult"/> containing the resulting TimeSpan if the parsing succeeds; otherwise, an error result.</returns>
    public static IResult ParseTime(string time)
    {
        long? totalSecs = null;

        if (time.TrySplit(' ', true, null, out var parts))
        {
            parts.ForEach(p =>
            {
                var result = ParseSeconds(p);
                
                if (result.IsSuccess)
                {
                    if (!totalSecs.HasValue)
                        totalSecs = 0L;
                    
                    if (result.TryReadResult<long>(true, out var value2))
                        totalSecs += value2;
                }
            });
        }
        else
        {
            var result = ParseSeconds(time);
            
            if (result.IsSuccess)
            {
                if (!totalSecs.HasValue)
                    totalSecs = 0L;
                
                if (result.TryReadResult<long>(true, out var value))
                    totalSecs += value;
            }
        }
        
        if (!totalSecs.HasValue)
            return Result.Error();
        
        return Result.Success(TimeSpan.FromSeconds(totalSecs.Value));
    }

    // just so you know this method is decompiled from my very first library
    private static IResult ParseSeconds(string t)
    {
        if (long.TryParse(t, out var result))
            return Result.Success(result);
        
        if (t.Length < 2)
            return Result.Error();

        if (!long.TryParse(t.Substring(0, t.Length - 1), out result))
            return Result.Error();
        
        var c = t[t.Length - 1];
        
        if (c == 'S' || c == 's')
            return Result.Success(result);
        
        switch (c)
        {
            case 'm':
                return Result.Success(result * 60);
            
            default:
                if (c != 'h')
                {
                    if (c == 'D' || c == 'd')
                        return Result.Success(result * 86400);
                    
                    if (c == 'W' || c == 'w')
                        return Result.Success(result * 604800);
                    
                    switch (c)
                    {
                        case 'M':
                            return Result.Success(result * 2629743);
                        
                        default:
                            if (c != 'y')
                                return Result.Error();
                            
                            goto case 'Y';
                        case 'Y':
                            return Result.Success(result * 31556926);
                    }
                }
                
                goto case 'H';
                
            case 'H':
                return Result.Success(result * 3600);
        }
    }
}