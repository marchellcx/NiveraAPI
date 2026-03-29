using System.Collections.Concurrent;

using NiveraAPI.Logs;

namespace NiveraAPI.IO.SyncedEvents
{
    /// <summary>
    /// Used to execute and listen to synchronized events.
    /// </summary>
    public class SyncedEventManager
    {
        private static LogSink log = LogManager.GetSource("IO", "SyncedEvents");
        
        private Dictionary<Type, SyncedEventData> listeners = new();
        private volatile ConcurrentQueue<object> events = new();

        /// <summary>
        /// Gets the number of events in the queue.
        /// </summary>
        public int QueueSize => events.Count;

        /// <summary>
        /// Subscribes to an event.
        /// </summary>
        /// <typeparam name="TEvent">The event to subscribe to.</typeparam>
        /// <param name="listener">The listener to subscribe.</param>
        /// <returns>true if the event was subscribed to</returns>
        /// <exception cref="ArgumentNullException">listener is null</exception>
        public bool SubscribeEvent<TEvent>(Action<TEvent> listener)
        {
            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            var wrapper = new Action<object>(syncedEvent => listener((TEvent)syncedEvent));

            if (listeners.TryGetValue(typeof(TEvent), out var eventData))
            {
                if (eventData.WrappersBySource.ContainsKey(listener))
                    return false;

                eventData.WrappersBySource.Add(listener, wrapper);
                eventData.WrappersByWrapper.Add(wrapper, listener);

                eventData.Listener = Delegate.Combine(eventData.Listener, wrapper) as Action<object>;
                return true;
            }

            eventData = new();
            eventData.Listener = wrapper;

            eventData.WrappersBySource.Add(listener, wrapper);
            eventData.WrappersByWrapper.Add(wrapper, listener);

            listeners.Add(typeof(TEvent), eventData);
            return true;
        }

        /// <summary>
        /// Unsubscribes a listener from an event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="listener">The listener to unsubscribe.</param>
        /// <returns>true if the listener was removed</returns>
        /// <exception cref="ArgumentNullException">listener is null</exception>
        public bool UnsubscribeEvent<TEvent>(Action<TEvent> listener)
        {
            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            if (!listeners.TryGetValue(typeof(TEvent), out var eventData))
                return false;

            if (!eventData.WrappersBySource.TryGetValue(listener, out var wrapper))
                return false;

            eventData.WrappersBySource.Remove(listener);
            eventData.WrappersByWrapper.Remove(wrapper);
            
            eventData.Listener = Delegate.RemoveAll(eventData.Listener, wrapper) as Action<object>;

            if (eventData.Listener is null)
            {
                eventData.WrappersBySource.Clear();
                eventData.WrappersBySource = null!;

                eventData.WrappersByWrapper.Clear();
                eventData.WrappersByWrapper = null!;

                listeners.Remove(typeof(TEvent));
            }

            return true;
        }

        /// <summary>
        /// Adds an event to the queue.
        /// </summary>
        /// <param name="syncedEvent">The event to add.</param>
        /// <exception cref="ArgumentNullException">syncedEvent is null</exception>
        public void QueueEvent(object syncedEvent)
        {
            if (syncedEvent is null)
                throw new ArgumentNullException(nameof(syncedEvent));

            events.Enqueue(syncedEvent);
        }

        /// <summary>
        /// Starts event execution.
        /// </summary>
        /// <returns>The number of processed events.</returns>
        /// <remarks>This method should be called on the same thread as which you intend to use properties of processed events.</remarks>
        public int ProcessEvents(int maxCount = -1)
        {
            if (events.Count == 0)
                return 0;

            var execCount = 0;

            while (events.TryDequeue(out var syncedEvent))
            {
                try
                {
                    if (syncedEvent != null 
                        && listeners.TryGetValue(syncedEvent.GetType(), out var eventData)
                        && eventData.Listener != null)
                    {
                        try
                        {
                            eventData.Listener(syncedEvent);
                        }
                        catch (Exception ex)
                        {
                            log.Error("ProcessEvents_B", ex);
                        }
                        
                        execCount++;
                    }
                }
                catch (Exception ex)
                {
                    log.Error("ProcessEvents_A", ex);
                }
                
                if (maxCount > 0 && execCount >= maxCount)
                    break;
            }

            return execCount;
        }
    }
}