using NiveraAPI.Utilities;

namespace NiveraAPI;

/// <summary>
/// Provides methods for managing and invoking a unified update operation, with support for
/// registering and unregistering callback actions. Ensures that invocations occur on the main thread.
/// </summary>
public static class LibraryUpdate
{
    private static volatile Action? update;

    private static long lastUtc = 0;

    private static long updateTicks = 0;
    private static float updateDelta = 0f;
    
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
    }

    /// <summary>
    /// Invokes all registered actions as part of the unified update operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this method is called on a thread other than the main thread.</exception>
    public static void Invoke()
    {
        if (!ThreadHelper.IsMainThread)
            throw new InvalidOperationException("This method must be called on the main thread.");

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
}