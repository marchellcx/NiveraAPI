namespace NiveraAPI.Utilities;

/// <summary>
/// Utilities used for thread management.
/// </summary>
public static class ThreadHelper
{
    private static volatile Thread mainThread;
    private static volatile TaskScheduler mainScheduler;
    
    /// <summary>
    /// Gets the task scheduler of the main thread.
    /// </summary>
    public static TaskScheduler MainTaskScheduler => mainScheduler;

    /// <summary>
    /// Determines whether the current thread is the main thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the ThreadHelper is not initialized properly by calling ThreadHelper.Initialize().
    /// </exception>
    public static bool IsMainThread
    {
        get
        {
            if (mainThread == null)
                throw new InvalidOperationException("ThreadHelper.Initialize() has not been called.");
            
            return Thread.CurrentThread == mainThread;
        }
    }

    /// <summary>
    /// Ensures that the current thread is the main thread.
    /// Throws an exception if the operation is not executed on the main thread.
    /// </summary>
    /// <param name="msg">The message to include in the exception if the check fails.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the operation is not performed on the main thread or if the ThreadHelper has not been initialized.
    /// </exception>
    public static void EnsureMainThread(string msg = "")
    {
        if (mainThread == null)
            throw new InvalidOperationException("ThreadHelper.Initialize() has not been called.");
        
        if (Thread.CurrentThread != mainThread)
            throw new InvalidOperationException($"Operation must be performed on the main thread: {msg}");
    }
    
    /// <summary>
    /// Queues a task continuation to run on the main thread.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="continuation">The action to continue with.</param>
    public static void ContinueWithOnMain(this Task task, Action<Task> continuation)
        => task.ContinueWith(continuation, MainTaskScheduler);

    /// <summary>
    /// Queues a task continuation to run on the main thread.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="continuation">The action to continue with.</param>
    public static void ContinueWithOnMain<T>(this Task<T> task, Action<Task<T>> continuation)
        => task.ContinueWith(continuation, MainTaskScheduler);

    /// <summary>
    /// Starts a task on the main game thread.
    /// </summary>
    /// <param name="action">The delegate to run in a task.</param>
    /// <returns>The started task.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Task RunOnMainThread(this Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));
        
        var task = new Task(action);
        
        task.Start(MainTaskScheduler);
        return task;
    }

    /// <summary>
    /// Starts a task on the main game thread.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="func">The delegate to run in a task.</param>
    /// <returns>The started task.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Task<T> RunOnMainThread<T>(this Func<T> func)
    {
        if (func is null)
            throw new ArgumentNullException(nameof(func));
        
        var task = new Task<T>(func);
        
        task.Start(MainTaskScheduler);
        return task;
    }

    internal static void Initialize()
    {
        mainThread = Thread.CurrentThread;
        
        if (SynchronizationContext.Current != null)
            mainScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        else
            mainScheduler = TaskScheduler.Current;
    }
}