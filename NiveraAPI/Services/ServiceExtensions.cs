using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.Services;

/// <summary>
/// Extensions targeting <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Attempts to retrieve a service instance of the specified type from the service collection.
    /// </summary>
    /// <typeparam name="TService">The type of the service to retrieve.</typeparam>
    /// <param name="services">The service collection to search for the specified service.</param>
    /// <param name="service">
    /// When this method returns, contains the service instance of type <typeparamref name="TService"/> if found;
    /// otherwise, the default value for the type of the <paramref name="service"/> parameter.
    /// </param>
    /// <returns>
    /// <c>true</c> if the service instance of type <typeparamref name="TService"/> was found in the service collection;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetService<TService>(this IServiceCollection services, out TService service)
    {
        service = default!;

        if (services == null)
            return false;
        
        if (services.GetService(typeof(TService)) is not TService castService)
            return false;
        
        service = castService;
        return true;
    }
    
    /// <summary>
    /// Retrieves a service instance of the specified type from the service collection.
    /// </summary>
    /// <typeparam name="TService">The type of the service to retrieve.</typeparam>
    /// <param name="services">The service collection to search for the specified service.</param>
    /// <returns>The service instance of type <typeparamref name="TService"/> if found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> parameter is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service of type <typeparamref name="TService"/> is not found in the service collection
    /// or cannot be cast to the specified type.
    /// </exception>
    public static TService GetService<TService>(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var service = services.GetService(typeof(TService));

        if (service is not TService castService)
            throw new InvalidOperationException($"Service of type {typeof(TService)} not found in the service collection.");
        
        return castService;
    }
}