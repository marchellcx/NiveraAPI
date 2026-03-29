using NiveraAPI.Logs;

namespace NiveraAPI.Utilities;

/// <summary>
/// Represents a queue that manages delayed and repeated execution of actions, with optional conditions and scheduling capabilities.
/// </summary>
public class CallQueue
{
    /// <summary>
    /// The global call queue instance.
    /// </summary>
    public static CallQueue Global { get; }
    
    static CallQueue()
    {
        Global = new();
        
        LibraryUpdate.Register(Global.Update);
    }
    
    /// <summary>
    /// Represents a call that is queued.
    /// </summary>
    public struct QueuedCall
    {
        /// <summary>
        /// The target of the call.
        /// </summary>
        public readonly Action Target;
        
        /// <summary>
        /// The condition that removes this call from the queue.
        /// </summary>
        public readonly Func<bool>? Condition;

        /// <summary>
        /// The time the call is scheduled to be executed.
        /// </summary>
        public long? UtcNext;

        /// <summary>
        /// The delay between executions.
        /// </summary>
        public int Delay;

        /// <summary>
        /// The remaining executions.
        /// </summary>
        public int Remaining;

        /// <summary>
        /// The amount of calls to execute.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Whether the call should be removed from the queue.
        /// </summary>
        public bool Remove;

        /// <summary>
        /// Whether this is a repeat-while function.
        /// </summary>
        public bool RepeatWhile;
        
        /// <summary>
        /// Creates a new instance of the <see cref="QueuedCall"/> struct.
        /// </summary>
        /// <param name="target">The action to execute.</param>
        /// <param name="remaining">The remaining executions.</param>
        /// <param name="delay">The delay between executions.</param>
        /// <param name="amount">The number of calls to execute.</param>
        /// <param name="condition">The condition to check before executing the call.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
        public QueuedCall(Action target, int remaining, int delay, int amount, bool repeatWhile,
            Func<bool>? condition = null)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Delay = delay;
            Condition = condition;
            Remaining = remaining;
            Amount = amount;
            RepeatWhile = repeatWhile;
            UtcNext = Delay > 0 ? DateTime.UtcNow.Ticks + (Delay * TimeSpan.TicksPerMillisecond) : null;
        }
    }

    private List<QueuedCall> calls = new();
    private LogSink log = LogManager.GetSource("IO", "CallQueue");

    /// <summary>
    /// Schedules a target action to be executed after a specified delay in milliseconds, with an optional condition to check before execution.
    /// </summary>
    /// <param name="milliseconds">The amount of time, in milliseconds, to wait before executing the action.</param>
    /// <param name="target">The action to execute after the delay.</param>
    /// <param name="condition">An optional condition that must evaluate to true before executing the action. If null, the action is always executed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="milliseconds"/> is less than 0.</exception>
    public void WaitSeconds(int milliseconds, Action target, Func<bool>? condition = null)
        => Schedule(target, condition, 1, milliseconds);

    /// <summary>
    /// Schedules the specified action to execute after a delay in milliseconds,
    /// optionally conditioned on a predicate.
    /// </summary>
    /// <param name="milliseconds">The delay in milliseconds before invoking the action.</param>
    /// <param name="invokeAmount">The number of times the action should be invoked after the delay expires.</param>
    /// <param name="target">The action to execute after the delay.</param>
    /// <param name="condition">
    /// An optional condition that determines whether the action is executed.
    /// The action will only execute if the condition evaluates to <c>true</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="target"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="milliseconds"/> is less than 0 or
    /// <paramref name="invokeAmount"/> is less than 1.
    /// </exception>
    public void WaitSeconds(int milliseconds, int invokeAmount, Action target, Func<bool>? condition = null)
        => Schedule(target, condition, 1, milliseconds, invokeAmount);

    /// <summary>
    /// Schedules an action to be executed repeatedly after a specified delay, with an optional condition to determine execution eligibility.
    /// </summary>
    /// <param name="milliseconds">The delay, in milliseconds, between each execution of the action.</param>
    /// <param name="amount">The number of times the action will be executed.</param>
    /// <param name="target">The action to execute repeatedly.</param>
    /// <param name="condition">An optional condition that determines whether the action can be executed. If null, the action will execute unconditionally.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="milliseconds"/> is negative or <paramref name="amount"/> is less than 1.</exception>
    public void WaitSecondsRepeat(int milliseconds, int amount, Action target, Func<bool>? condition = null)
        => Schedule(target, condition, 1, milliseconds, amount);

    /// <summary>
    /// Schedules a repeated action to be executed after a specified delay, for a defined number of times.
    /// </summary>
    /// <param name="milliseconds">The delay, in milliseconds, between each execution of the action.</param>
    /// <param name="amount">The total number of times the action should be executed.</param>
    /// <param name="remaining">The number of remaining executions to be scheduled.</param>
    /// <param name="target">The action to execute repeatedly after the specified delay.</param>
    /// <param name="condition">An optional condition that determines whether the action should execute. If null, the action executes unconditionally.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="milliseconds"/> is less than 0,
    /// <paramref name="amount"/> is less than 1,
    /// or <paramref name="remaining"/> is less than 1.
    /// </exception>
    public void WaitSecondsRepeat(int milliseconds, int amount, int remaining, Action target,
        Func<bool>? condition = null)
        => Schedule(target, condition, remaining, milliseconds);

    /// <summary>
    /// Schedules a repeating call to the specified target action while the provided condition evaluates to true.
    /// </summary>
    /// <param name="delayMilliseconds">The delay, in milliseconds, between each invocation of the target action.</param>
    /// <param name="invokeCount">The maximum number of times the target action will be invoked.</param>
    /// <param name="target">The action to invoke repeatedly.</param>
    /// <param name="condition">The condition that must evaluate to true for the action to continue being invoked.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="delayMilliseconds"/> is less than 0 or if <paramref name="invokeCount"/> is less than 1.
    /// </exception>
    public void RepeatWhile(int delayMilliseconds, int invokeCount, Action target, Func<bool> condition)
        => Schedule(target, condition, 1, delayMilliseconds, invokeCount, true);

    /// <summary>
    /// Schedules a new action to be executed with optional conditions, delay, and repeat settings.
    /// </summary>
    /// <param name="target">The action that will be scheduled for execution.</param>
    /// <param name="condition">An optional condition that determines when the action can be executed. If null, the action will execute unconditionally.</param>
    /// <param name="remainingExecutions">The number of times the action is allowed to execute before being removed from the queue.</param>
    /// <param name="delayMilliseconds">The delay, in milliseconds, between each execution of the action.</param>
    /// <param name="invokeCount">The number of times the action is executed during each call until the delay applies.</param>
    /// <param name="isRepeatWhile">Whether the action should repeat while the condition is true.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="remainingExecutions"/> is less than 1, <paramref name="delayMilliseconds"/> is negative,
    /// or <paramref name="invokeCount"/> is less than 1.
    /// </exception>
    public void Schedule(Action target, Func<bool>? condition = null, int remainingExecutions = 1,
        int delayMilliseconds = 0,
        int invokeCount = 1,
        bool isRepeatWhile = false)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (remainingExecutions < 1)
            throw new ArgumentOutOfRangeException(nameof(remainingExecutions));

        if (delayMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(delayMilliseconds));
        
        if (invokeCount < 1)
            throw new ArgumentOutOfRangeException(nameof(invokeCount));
        
        calls.Add(new QueuedCall(target, remainingExecutions, delayMilliseconds, invokeCount, isRepeatWhile, condition));
    }

    /// <summary>
    /// Updates the internal queue of calls and processes any eligible queued calls based on their
    /// execution conditions and timing. This method iterates through the queued calls, executes
    /// those that meet their conditions, and removes those that have completed their execution
    /// or no longer meet their criteria.
    /// </summary>
    /// <remarks>
    /// Calls are evaluated based on their scheduled execution time, remaining executions,
    /// and optional condition delegate. Calls that are due or meet their conditions are executed.
    /// Once a call has completed or is flagged for removal, it is removed from the queue.
    /// </remarks>
    /// <exception cref="Exception">
    /// Exceptions thrown during the execution of a call's <c>Target</c> action will result
    /// in the call being marked for removal from the queue.
    /// </exception>
    public void Update()
    {
        var remove = false;

        for (var i = 0; i < calls.Count; i++)
        {
            var call = calls[i];

            if (!call.UtcNext.HasValue || DateTime.UtcNow.Ticks >= call.UtcNext.Value)
            {
                try
                {
                    if (call is { RepeatWhile: true, Condition: not null, Amount: > 0 })
                    {
                        if (!call.Condition())
                            continue;

                        for (var x = 0; x < call.Amount; x++)
                            call.Target();
                    }
                    else if (call is { Amount: > 0, Remaining: > 0 }
                             && (call.Condition == null || call.Condition()))
                    {
                        for (var x = 0; x < call.Amount; x++)
                            call.Target();

                        call.Remaining--;

                        if (call.Remaining > 0)
                        {
                            call.UtcNext = call.Delay > 0
                                ? DateTime.UtcNow.Ticks + (call.Delay * TimeSpan.TicksPerMillisecond)
                                : null;
                        }
                        else
                        {
                            call.Remove = true;
                        }
                    }
                    else
                    {
                        call.Remove = true;
                    }
                }

                catch (Exception ex)
                {
                    call.Remove = true;

                    log.Error(ex);
                }
            }

            if (call.Remove)
                remove = true;

            calls[i] = call;
        }

        if (remove)
            calls.RemoveAll(x => x.Remove);
    }
}