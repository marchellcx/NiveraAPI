using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.Networking.Interfaces;

/// <summary>
/// Represents a network service.
/// </summary>
public interface INetworkService : IService
{
    /// <summary>
    /// The peer associated with this network service.
    /// </summary>
    Peer Peer { get; }

    /// <summary>
    /// Updates the network service.
    /// </summary>
    void Update(float localDeltaTime, float networkDeltaTime);

    /// <summary>
    /// Processes a given serializable object payload, determining if the payload can be handled by the network service.
    /// </summary>
    /// <param name="serializableObject">
    /// The payload object that implements the <see cref="ISerializableObject"/> interface.
    /// This object is intended to be processed by the network service.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the payload was successfully handled.
    /// Returns <c>true</c> if the payload was processed, otherwise <c>false</c>.
    /// </returns>
    bool HandlePayload(ISerializableObject serializableObject);
}