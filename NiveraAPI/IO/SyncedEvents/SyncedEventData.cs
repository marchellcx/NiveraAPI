namespace NiveraAPI.IO.SyncedEvents;

/// <summary>
/// Represents the data associated with a synchronized event.
/// </summary>
public class SyncedEventData
{
    /// <summary>
    /// The delegate for the event.
    /// </summary>
    public Action<object>? Listener;

    /// <summary>
    /// A list of wrapped listeners keyed by their wrappers.
    /// </summary>
    public Dictionary<Action<object>, object> WrappersByWrapper = new();

    /// <summary>
    /// A list of wrapped listeners keyed by their source.
    /// </summary>
    public Dictionary<object, Action<object>> WrappersBySource = new();
}