namespace NiveraAPI.Services.Interfaces;

/// <summary>
/// Represents a collection of services.
/// </summary>
public interface IServiceCollection : IService
{
    /// <summary>
    /// List of services in this collection.
    /// </summary>
    IEnumerable<IService> Services { get; }

    /// <summary>
    /// Retrieves the service instance of the specified type from the service collection.
    /// </summary>
    /// <param name="serviceType">The type of the service to retrieve.</param>
    /// <returns>
    /// An instance of the requested service if it exists in the collection; otherwise, null.
    /// </returns>
    IService? GetService(Type serviceType);

    /// <summary>
    /// Adds a new service instance of the specified type to the service collection.
    /// </summary>
    /// <param name="serviceType">The type of the service to add.</param>
    /// <param name="arguments">An array of arguments required to initialize the service instance.</param>
    /// <returns>
    /// The newly added service instance if the operation is successful; otherwise, null.
    /// </returns>
    IService? AddService(Type serviceType, object[] arguments);

    /// <summary>
    /// Removes the service instance of the specified type from the service collection.
    /// </summary>
    /// <param name="serviceType">The type of the service to remove.</param>
    /// <returns>
    /// True if the service was successfully removed; otherwise, false.
    /// </returns>
    bool RemoveService(Type serviceType);
}