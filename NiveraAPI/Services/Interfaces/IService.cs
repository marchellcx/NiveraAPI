namespace NiveraAPI.Services.Interfaces;

/// <summary>
/// Represents a service that can be started and stopped.
/// </summary>
public interface IService
{
    /// <summary>
    /// The service collection this service belongs to.
    /// </summary>
    IServiceCollection Collection { get; set; }

    /// <summary>
    /// Whether the service is running.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Whether the service is valid.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Determines whether the specified service collection allows a new service to be added.
    /// </summary>
    /// <param name="collection">The service collection to check.</param>
    /// <returns>
    /// True if the service can be added to the collection; otherwise, false.
    /// </returns>
    bool CanBeAdded(IServiceCollection collection);

    /// <summary>
    /// Starts the service.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the service.
    /// </summary>
    void Stop();
}