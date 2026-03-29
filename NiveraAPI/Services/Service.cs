using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.Services;

/// <summary>
/// Represents a concrete implementation of a service that can be started, stopped, and disposed.
/// </summary>
public class Service : IService, IRequireServices, IDisposable
{
    private volatile bool valid = true;
    private volatile bool running = false;
    
    private volatile IServiceCollection collection;
    
    /// <summary>
    /// The services required by this service.
    /// </summary>
    public virtual Type[] RequiredServices { get; } = Array.Empty<Type>();

    /// <summary>
    /// The service collection this service belongs to.
    /// </summary>
    public IServiceCollection Collection
    {
        get => collection;
        set => collection = value;
    }

    /// <summary>
    /// Whether the service is running.
    /// </summary>
    public bool IsRunning => running;

    /// <summary>
    /// Whether the service is valid.
    /// </summary>
    public virtual bool IsValid => valid;

    /// <summary>
    /// Determines whether the specified service collection allows a new service to be added.
    /// </summary>
    /// <param name="collection">The service collection to check.</param>
    /// <returns>
    /// True if the service can be added to the collection; otherwise, false.
    /// </returns>
    public virtual bool CanBeAdded(IServiceCollection collection)
        => true;

    /// <summary>
    /// Starts the service.
    /// </summary>
    public virtual void Start()
    {
        if (!valid)
            throw new ObjectDisposedException(GetType().Name);
        
        running = true;
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public virtual void Stop()
    {
        if (!valid)
            throw new ObjectDisposedException(GetType().Name);
        
        running = false;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public virtual void Dispose()
    {
        if (!valid)
            throw new ObjectDisposedException(GetType().Name);
        
        if (running)
            Stop();

        valid = false;
    }
}