using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Logs;

namespace NiveraAPI.IO.Network;

/// <summary>
/// Represents a network service that provides functionality for managing network connections.
/// </summary>
public class NetService
{
    /// <summary>
    /// The log associated with the service.
    /// </summary>
    public LogSink Log
    {
        get
        {
            if (field == null)
            {
                field = LogManager.GetSource("Services", $"{GetType().Name}@{Connection?.EndPoint}");
            }

            return field;
        }
    }
    
    /// <summary>
    /// The connection associated with the service.
    /// </summary>
    public NetConnection? Connection { get; internal set; }
    
    /// <summary>
    /// Whether the service is a server.
    /// </summary>
    public bool IsServer => Connection?.IsServer ?? false;
    
    /// <summary>
    /// Whether the service is a client.
    /// </summary>
    public bool IsClient => Connection?.IsClient ?? false;

    /// <summary>
    /// Whether the service is currently connected to a remote server.
    /// </summary>
    public bool IsConnected => Connection != null && Connection.IsConnected;

    /// <summary>
    /// Initializes and starts the network service.
    /// This method must be overridden in derived classes to provide additional functionalities specific to those implementations.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="Connection"/> property is null, indicating that no network connection is available.
    /// </exception>
    public virtual void Start()
    {

    }

    /// <summary>
    /// Stops the network service and releases associated resources.
    /// This method should be overridden in derived classes to handle additional cleanup operations specific to their needs.
    /// </summary>
    public virtual void Stop()
    {
        
    }

    /// <summary>
    /// Sends a serializable object over the network connection.
    /// </summary>
    /// <param name="obj">The object to be serialized and sent via the current network connection.</param>
    public void Send(ISerializableObject obj)
        => Connection?.Send(obj);

    /// <summary>
    /// Disconnects the current network connection, if established.
    /// </summary>
    public void Disconnect()
        => Connection?.Disconnect();

    /// <summary>
    /// Updates the service with the provided time deltas for network and general update operations.
    /// </summary>
    /// <param name="netDelta">The elapsed time since the last network update, in seconds.</param>
    /// <param name="updateDelta">The elapsed time since the last update, in seconds.</param>
    public virtual void Update(float netDelta, float updateDelta)
    {
        
    }

    /// <summary>
    /// Receives a serializable object message and processes it.
    /// </summary>
    /// <param name="msg">The serializable object message to be processed.</param>
    /// <returns>True if the message was successfully processed; otherwise, false.</returns>
    public virtual bool Receive(ISerializableObject msg)
    {
        return false;
    }
}