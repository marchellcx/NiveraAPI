using System.Collections.Concurrent;

namespace NiveraAPI.Utilities;

/// <summary>
/// Represents a thread-safe action queue that allows scheduling of actions for later execution.
/// </summary>
public class ActionQueue
{
    /// <summary>
    /// The global action queue instance.
    /// </summary>
    public static ActionQueue Global { get; }

    static ActionQueue()
    {
        Global = new();
        
        LibraryUpdate.Register(() => Global.UpdateQueue());
    }
    
    private volatile ConcurrentQueue<Action> queue = new();
    
    /// <summary>
    /// The number of actions in the queue.
    /// </summary>
    public int Size => queue.Count;

    /// <summary>
    /// Removes all actions currently stored in the internal queue.
    /// </summary>
    public void ClearQueue()
    {
        while (queue.TryDequeue(out _))
        {
            
        }
    }

    /// <summary>
    /// Adds a new action to the internal queue for later execution.
    /// </summary>
    /// <param name="action">The action to be added to the queue. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided action is null.</exception>
    public void AddToQueue(Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));
        
        queue.Enqueue(action);
    }
    
    /// <summary>
    /// Processes and executes actions from the internal queue.
    /// If a maximum count is specified, limits the number of actions executed to the given maximum.
    /// </summary>
    /// <param name="maxCount">The maximum number of actions to execute. Set to -1 to process all actions in the queue.</param>
    /// <returns>The number of actions that were executed.</returns>
    public int UpdateQueue(int maxCount = -1)
    {
        var count = 0;

        while (queue.TryDequeue(out var action))
        {
            if (action != null)
            {
                action();

                count++;

                if (maxCount > 0 && count >= maxCount)
                    break;
            }
        }

        return count;
    }
}