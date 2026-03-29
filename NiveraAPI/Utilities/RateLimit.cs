namespace NiveraAPI.Utilities
{
    /// <summary>
    /// Provides a mechanism to limit the rate of requests processed over a specified time window, allowing for an
    /// initial burst of requests.
    /// </summary>
    public class RateLimit
    {
        private long rCurrentCount;
        private long rLastResetTimestampTicks;

        private readonly object _lock = new();

        /// <summary>
        /// Gets or sets the maximum number of requests that can be processed in a burst.
        /// </summary>
        public int MaxBurst { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of requests allowed within a specified time window.
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } 

        /// <summary>
        /// Gets or sets the duration of the time window used for processing events.
        /// </summary>
        public TimeSpan WindowDuration { get; set; }

        /// <summary>
        /// Creates a rate limiter that allows 'maxRequestsPerWindow' requests every 'windowDuration'
        /// with an initial burst allowance up to 'maxBurst' requests.
        /// </summary>
        /// <param name="maxRequestsPerWindow">Maximum requests allowed in the time window (after burst is exhausted)</param>
        /// <param name="windowDuration">Length of the sliding/fixed window</param>
        /// <param name="maxBurst">Maximum initial burst size (usually ≥ maxRequestsPerWindow)</param>
        public RateLimit(int maxRequestsPerWindow, TimeSpan windowDuration, int maxBurst)
        {
            if (maxRequestsPerWindow <= 0) 
                throw new ArgumentOutOfRangeException(nameof(maxRequestsPerWindow));

            if (windowDuration <= TimeSpan.Zero) 
                throw new ArgumentOutOfRangeException(nameof(windowDuration));

            if (maxBurst < maxRequestsPerWindow) 
                throw new ArgumentException("maxBurst should be ≥ maxRequestsPerWindow");

            MaxBurst = maxBurst;
            WindowDuration = windowDuration;
            MaxRequestsPerWindow = maxRequestsPerWindow;

            // Initialize so first call can burst
            rCurrentCount = 0;
            rLastResetTimestampTicks = DateTime.UtcNow.Ticks - windowDuration.Ticks;
        }

        /// <summary>
        /// Attempts to acquire one token / permission to proceed.
        /// Returns true if allowed, false if rate-limited.
        /// </summary>
        public bool TryAcquire()
            => TryAcquire(1);

        /// <summary>
        /// Attempts to acquire the requested number of tokens.
        /// </summary>
        public bool TryAcquire(int count)
        {
            if (count <= 0)
                return true;

            lock (_lock)
            {
                var nowTicks = DateTime.UtcNow.Ticks;
                var windowTicks = WindowDuration.Ticks;

                var elapsedTicks = nowTicks - rLastResetTimestampTicks;
                var windowsPassed = elapsedTicks / windowTicks;

                if (windowsPassed >= 1)
                {
                    rCurrentCount = 0;
                    rLastResetTimestampTicks = nowTicks - (elapsedTicks % windowTicks);
                }

                // Effective limit = normal window limit + remaining burst capacity
                var remainingBurstCapacity = Math.Max(0, MaxBurst - rCurrentCount);
                var effectiveLimit = MaxRequestsPerWindow + remainingBurstCapacity;

                if (rCurrentCount + count <= effectiveLimit)
                {
                    rCurrentCount += count;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns how many requests are still allowed in current window (including burst)
        /// </summary>
        public int Remaining()
        {
            lock (_lock)
            {
                var nowTicks = DateTime.UtcNow.Ticks;
                var elapsedTicks = nowTicks - rLastResetTimestampTicks;

                if (elapsedTicks >= WindowDuration.Ticks)
                    return MaxBurst;

                var remainingBurst = Math.Max(0, MaxBurst - rCurrentCount);
                return (int)(MaxRequestsPerWindow + remainingBurst);
            }
        }

        /// <summary>
        /// Returns approximate time until next reset / refill (for logging / headers)
        /// </summary>
        public TimeSpan TimeUntilReset()
        {
            lock (_lock)
            {
                var nowTicks = DateTime.UtcNow.Ticks;
                var elapsedTicks = nowTicks - rLastResetTimestampTicks;
                var remainingTicks = WindowDuration.Ticks - (elapsedTicks % WindowDuration.Ticks);

                if (remainingTicks <= 0)
                    return TimeSpan.Zero;

                return TimeSpan.FromTicks(remainingTicks);
            }
        }

        /// <summary>
        /// Creates a new RateLimit instance with the same configuration as the current instance.
        /// </summary>
        /// <returns>A RateLimit object that is a copy of the current instance, containing identical settings for maximum
        /// requests, window duration, and burst limit.</returns>
        public RateLimit Clone()
            => new(MaxRequestsPerWindow, WindowDuration, MaxBurst);
    }
}