using System.Collections.Concurrent;

namespace NiveraAPI.Utilities;

/// <summary>
/// Represents a generic event queue designed for storing and processing events of a specific type.
/// </summary>
/// <typeparam name="T">The type of events that this queue will store and process.</typeparam>
public class EventQueue<T>
{
    private volatile ConcurrentQueue<T> queue = new();

    /// <summary>
    /// Adds an event to the queue.
    /// </summary>
    /// <param name="obj">The event to be added to the queue. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided event object is null.</exception>
    public void QueueEvent(T obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        queue.Enqueue(obj);
    }
    
    /// <summary>
    /// Processes events in the queue by executing the specified handler for each event.
    /// Allows processing of a maximum number of events if specified.
    /// </summary>
    /// <param name="handler">The action to execute for each event. Must not be null.</param>
    /// <param name="maxCount">The maximum number of events to process. If set to -1, processes all events in the queue.</param>
    /// <returns>The number of events processed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the handler is null.</exception>
    public int ProcessEvents(Action<T> handler, int maxCount = -1)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var count = 0;

        while (queue.TryDequeue(out var obj))
        {
            count++;
            
            handler(obj);

            if (maxCount > 0 && count >= maxCount)
                break;
        }

        return count;
    }
}