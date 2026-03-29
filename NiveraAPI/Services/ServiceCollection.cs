using System.Collections.Concurrent;
using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.Services;

/// <summary>
/// Represents a collection of services.
/// </summary>
public class ServiceCollection : IServiceCollection
{
    private volatile bool running;
    private volatile IServiceCollection? parent;
    private volatile ConcurrentDictionary<Type, IService> services;

    /// <summary>
    /// Whether the service is running.
    /// </summary>
    public virtual bool IsRunning => running;

    /// <summary>
    /// Whether the service is valid.
    /// </summary>
    public virtual bool IsValid => true;

    /// <summary>
    /// List of services in this collection.
    /// </summary>
    public IEnumerable<IService> Services => services.Values;

    /// <summary>
    /// The service collection this service belongs to.
    /// </summary>
    public IServiceCollection? Collection
    {
        get => parent;
        set => parent = value;
    }

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
        if (running)
            throw new InvalidOperationException("Service already running");
        
        running = true;

        services ??= new();
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public virtual void Stop()
    {
        if (!running)
            return;

        parent = null;
        running = false;
        
        services.Clear();
    }

    /// <summary>
    /// Retrieves the service instance of the specified type from the service collection.
    /// </summary>
    /// <param name="serviceType">The type of the service to retrieve.</param>
    /// <returns>
    /// An instance of the requested service if it exists in the collection; otherwise, null.
    /// </returns>
    public IService? GetService(Type serviceType)
    {
        if (services.TryGetValue(serviceType, out var service))
            return service;
        
        return null;
    }

    /// <summary>
    /// Adds a new service instance of the specified type to the service collection.
    /// </summary>
    /// <param name="serviceType">The type of the service to add.</param>
    /// <param name="arguments">An array of arguments required to initialize the service instance.</param>
    /// <returns>
    /// The newly added service instance if the operation is successful; otherwise, null.
    /// </returns>
    public IService? AddService(Type serviceType, object[] arguments)
    {
        if (services.TryGetValue(serviceType, out var service))
            return service;
        
        service = Activator.CreateInstance(serviceType, arguments) as IService;

        if (service != null)
            return AddService(service) ? service : null;
        
        return null;
    }

    /// <summary>
    /// Adds a new service to the collection if it meets the necessary conditions.
    /// </summary>
    /// <param name="service">The service instance to be added.</param>
    /// <param name="serviceType">
    /// The type of the service being added. If not specified, the type of the provided service instance is used.
    /// </param>
    /// <returns>
    /// True if the service is successfully added to the collection; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided service instance is null.
    /// </exception>
    public bool AddService(IService service, Type? serviceType = null)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var type = serviceType ?? service.GetType();
        
        if (services.ContainsKey(type))
            return false;
        
        if (service is IRequireServices requireServices
            && requireServices.RequiredServices.Length > 0
            && !requireServices.RequiredServices.All(services.ContainsKey))
            return false;
        
        if (!service.CanBeAdded(this))
            return false;
        
        service.Collection = this;
        service.Start();
        
        services.TryAdd(type, service);
        
        OnServiceAdded(service);
        return true;
    }

    /// <summary>
    /// Removes the service instance of the specified type from the service collection.
    /// </summary>
    /// <param name="serviceType">The type of the service to remove.</param>
    /// <returns>
    /// True if the service was successfully removed; otherwise, false.
    /// </returns>
    public bool RemoveService(Type serviceType)
    {
        if (services.TryRemove(serviceType, out var service))
        {
            OnServiceRemoved(service);
            
            service.Stop();
            service.Collection = null!;
            
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Called when a service is added to the collection.
    /// </summary>
    /// <param name="service">The service instance that was added.</param>
    public virtual void OnServiceAdded(IService service)
    {

    }

    /// <summary>
    /// Called when a service is removed from the collection.
    /// </summary>
    /// <param name="service">The service instance that was removed.</param>
    public virtual void OnServiceRemoved(IService service)
    {
    }
}