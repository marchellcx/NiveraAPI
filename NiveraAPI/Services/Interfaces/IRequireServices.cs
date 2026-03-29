namespace NiveraAPI.Services.Interfaces;

/// <summary>
/// Represents a service that requires other services.
/// </summary>
public interface IRequireServices
{
    /// <summary>
    /// The services required by this service.
    /// </summary>
    Type[] RequiredServices { get; }
}