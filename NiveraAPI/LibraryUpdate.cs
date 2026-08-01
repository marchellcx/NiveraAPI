using NiveraAPI.Logs;

namespace NiveraAPI;

/// <summary>
/// Provides methods for managing and invoking a unified update operation, with support for
/// registering and unregistering callback actions. Ensures that invocations occur on the main thread.
/// </summary>
public static class LibraryUpdate
{
    private static readonly LogSink log = LogManager.GetSource("IO", "Update");
    private static volatile Action? update;

    private static long lastUtc = 0;

    private static long updateTicks = 0;
    private static float updateDelta = 0f;
    
    /// <summary>
    /// Whether or not debug logs should be enabled.
    /// </summary>
    public static bool DebugLogs { get; set; }
    
    /// <summary>
    /// Gets the time elapsed since the last update, in seconds.
    /// </summary>
    public static float DeltaTime => updateDelta;
    
    /// <summary>
    /// Gets the time elapsed since the last update, in ticks.
    /// </summary>
    public static long DeltaTicks => updateTicks;

    /// <summary>
    /// Registers an action to be invoked as part of a unified update operation.
    /// </summary>
    /// <param name="target">The action to be registered. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="target"/> is null.</exception>
    public static void Register(Action target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (update == null)
        {
            update = target;
            return;
        }

        var curUpdate = update;
        var newUpdate = Delegate.Combine(update, target) as Action;

        Interlocked.CompareExchange(ref update, newUpdate, curUpdate);
        
        log.DebugIf("Register", $"Added handler: &1{target.Method}&r", DebugLogs);
    }

    /// <summary>
    /// Unregisters a previously registered action from the unified update operation.
    /// </summary>
    /// <param name="target">The action to be unregistered. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="target"/> is null.</exception>
    public static void Unregister(Action target)
    {
        if (update == null)
            return;

        var obj = Delegate.Remove(update, target);

        if (obj is Action newUpdate)
        {
            var curUpdate = update;

            Interlocked.CompareExchange(ref update, newUpdate, curUpdate);
        }
        else
        {
            var curUpdate = update;
            
            Interlocked.CompareExchange(ref update, null, curUpdate);
        }

        log.DebugIf("Unregister", $"Removed handler: &1{target.Method}&r", DebugLogs);
    }

    /// <summary>
    /// Invokes all registered actions as part of the unified update operation.
    /// </summary>
    /// <remarks>Make sure to invoke this method from the same thread you invoked <see cref="LibraryLoader.Initialize"/> from!</remarks>
    public static void Invoke()
    {
        try
        {
            if (lastUtc != 0)
            {
                var curUtc = DateTime.UtcNow.Ticks;
                var delta = (curUtc - lastUtc) / TimeSpan.TicksPerMillisecond;

                updateDelta = delta / 1000f;
                updateTicks = curUtc - lastUtc;

                lastUtc = curUtc;
            }
            else
            {
                lastUtc = DateTime.UtcNow.Ticks;
            }

            update?.Invoke();
        }
        catch (Exception ex)
        {
            log.Error("Invoke", ex);
        }
    }
}