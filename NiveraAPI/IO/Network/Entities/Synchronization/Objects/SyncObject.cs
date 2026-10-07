using NiveraAPI.IO.Network.Entities.Synchronization.Interfaces;
using NiveraAPI.IO.Serialization;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network.Entities.Synchronization.Objects;

/// <summary>
/// Represents a synchronized object that maintains a set of synchronized variables
/// and is associated with a parent synchronization entity in a networked environment.
/// Provides mechanisms for initialization, state management, payload processing,
/// and cleanup within the synchronization system.
/// </summary>
public class SyncObject
{
    /// <summary>
    /// Gets the collection of registered SyncObject types mapped by their string identifiers.
    /// This dictionary is used to associate string-based type names with their corresponding
    /// <see cref="System.Type"/> instances, facilitating dynamic instantiation of SyncObject
    /// types during synchronization processes.
    /// </summary>
    public static Dictionary<string, Type> SyncObjectTypes { get; } = new();

    /// <summary>
    /// Registers a synchronized object type for use with the synchronization system.
    /// Associates a local type, a remote type identifier, and the generic object type,
    /// enabling the system to recognize and manage instances of the synchronized object
    /// during networked operations.
    /// </summary>
    /// <typeparam name="TObject">
    /// The type of the synchronized object being registered. This type must inherit from <see cref="SyncObject"/>.
    /// </typeparam>
    /// <param name="localType">
    /// The identifier representing the local type of the synchronized object.
    /// This value cannot be null or empty.
    /// </param>
    /// <param name="remoteType">
    /// The identifier representing the remote type of the synchronized object.
    /// This value cannot be null or empty.
    /// </param>
    public static void AddSyncObject<TObject>(string localType, string remoteType) where TObject : SyncObject
    {
        Exceptions.EmptyArgument(nameof(localType), localType);
        Exceptions.EmptyArgument(nameof(remoteType), remoteType);

        SyncObjectTypes[localType] = typeof(TObject);
        SyncObjectTypes[remoteType] = typeof(TObject);
    }

    /// <summary>
    /// Registers a synchronized object type for use with the synchronization system.
    /// Associates a local type and remote type identifier with the provided object type,
    /// enabling the system to recognize and manage instances of the synchronized object
    /// during networked operations.
    /// </summary>
    /// <param name="localType">
    /// The identifier representing the local type of the synchronized object.
    /// This value cannot be null or empty.
    /// </param>
    /// <param name="remoteType">
    /// The identifier representing the remote type of the synchronized object.
    /// This value cannot be null or empty.
    /// </param>
    /// <param name="type">
    /// The <see cref="Type"/> of the synchronized object being registered.
    /// This value cannot be null.
    /// </param>
    public static void AddSyncObject(string localType, string remoteType, Type type)
    {
        Exceptions.EmptyArgument(nameof(localType), localType);
        Exceptions.EmptyArgument(nameof(remoteType), remoteType);
        
        Exceptions.NullArgument(nameof(type), type);
        
        SyncObjectTypes[localType] = type;
        SyncObjectTypes[remoteType] = type;
    }
    
    /// <summary>
    /// Gets the owner <see cref="Entity"/> associated with this SyncObject.
    /// The Owner provides access to the entity context that manages the
    /// lifecycle, synchronization, and network-related functionality
    /// of this SyncObject via its associated Parent.
    /// </summary>
    public Entity Owner => Parent.Owner;

    /// <summary>
    /// Gets or sets the synchronization parent associated with this SyncObject.
    /// The Parent provides access to the owner context and methods necessary for managing
    /// synchronization logic, such as handling synchronized variables and synchronization updates.
    /// </summary>
    public SyncParent Parent { get; internal set; }
    
    /// <summary>
    /// A dictionary that maps unique identifiers (keys) to their corresponding synchronized
    /// variable objects (values). This property is used for tracking and managing state
    /// synchronization of variables across networked entities.
    /// </summary>
    public Dictionary<string, ISyncVar> SyncVars { get; } = new();

    /// <summary>
    /// Gets the collection of registered remote methods mapped by their string identifiers.
    /// This dictionary is used to associate string-based identifiers with their corresponding
    /// <see cref="RemoteMethod"/> instances, facilitating invocation and management of
    /// remote methods within a synchronized network context.
    /// </summary>
    public Dictionary<string, RemoteMethod> RemoteMethods { get; } = new();
    
    /// <summary>
    /// Maintains a collection of response handlers associated with remote method requests. Each handler
    /// is a tuple containing a request identifier, the time the request was made, and a callback function to handle
    /// the corresponding response. This property is used to track pending requests and ensure proper handling
    /// of their asynchronous responses.
    /// </summary>
    public List<(string RequestId, DateTime RequestTime, Action<ByteReader> Handler)> ResponseHandlers { get; } = new();

    /// <summary>
    /// Gets or sets the unique identifier for this SyncObject.
    /// This identifier is used to distinguish the SyncObject within its owner context
    /// and is critical for synchronization operations across the network.
    /// </summary>
    public string Id { get; internal set; } = string.Empty;

    /// <summary>
    /// Invoked when this SyncObject is spawned and added to a SyncParent.
    /// Provides an entry point for initializing or setting up logic specific to the SyncObject instance
    /// upon being associated with its parent.
    /// </summary>
    public virtual void OnLocalSpawned(ByteWriter payloadWriter)
    {
        
    }

    /// <summary>
    /// Invoked when this SyncObject is spawned in a remote context within the synchronization system.
    /// Provides a mechanism for processing and initializing data sent from the local instance
    /// during the spawning process.
    /// </summary>
    /// <param name="payloadReader">The <see cref="ByteReader"/> instance containing
    /// the serialized data payload for the SyncObject. This data is provided by the local peer
    /// during synchronization.</param>
    public virtual void OnRemoteSpawned(ByteReader? payloadReader)
    {
    }

    /// <summary>
    /// Invoked when this SyncObject is despawned and removed from a SyncParent.
    /// Provides a mechanism to handle cleanup or deinitialization logic specific to the SyncObject instance
    /// when it is disassociated from its parent.
    /// </summary>
    public virtual void OnDespawned()
    {
        RemoveAllSyncVars();
        RemoveAllRemoteMethods();
        
        ResponseHandlers.Clear();
    }

    /// <summary>
    /// Attempts to retrieve a remote method associated with the specified identifier.
    /// </summary>
    /// <param name="remoteMethodId">The unique identifier of the remote method to retrieve.</param>
    /// <param name="method">When this method returns, contains the remote method associated with the specified identifier, if the identifier is found; otherwise, null.</param>
    /// <returns>True if a remote method with the specified identifier is found; otherwise, false.</returns>
    public bool TryGetRemoteMethod(string remoteMethodId, out RemoteMethod method)
    {
        Exceptions.EmptyArgument(nameof(remoteMethodId), remoteMethodId);
        return RemoteMethods.TryGetValue(remoteMethodId, out method);
    }

    /// <summary>
    /// Retrieves a remote method associated with the specified identifier from the synchronization parent.
    /// </summary>
    /// <param name="remoteMethodId">The identifier of the remote method to retrieve.</param>
    /// <returns>
    /// The <see cref="RemoteMethod"/> associated with the specified identifier, or <c>null</c> if no such method exists.
    /// </returns>
    public RemoteMethod? GetRemoteMethod(string remoteMethodId)
    {
        Exceptions.EmptyArgument(nameof(remoteMethodId), remoteMethodId);
        return RemoteMethods.TryGetValue(remoteMethodId, out var remoteMethod) ? remoteMethod : null;
    }

    /// <summary>
    /// Adds a remote method to the synchronization object if it does not already exist.
    /// </summary>
    /// <param name="method">The remote method to be added.</param>
    /// <returns>True if the remote method was added successfully; otherwise, false.</returns>
    public bool AddRemoteMethod(RemoteMethod method)
    {
        Exceptions.NullArgument(nameof(method), method);

        if (RemoteMethods.ContainsKey(method.Id))
            return false;
        
        RemoteMethods.Add(method.Id, method);
        return true;
    }
    
    /// <summary>
    /// Adds a remote method to the synchronization object instance.
    /// If a remote method with the specified identifier already exists, it returns the existing instance.
    /// </summary>
    /// <param name="remoteMethodId">The unique identifier of the remote method.</param>
    /// <param name="hasReturnValue">Specifies whether the remote method has a return value.</param>
    /// <param name="invokeMethod">
    /// The delegate defining the logic to execute when the remote method is invoked.
    /// Accepts a <see cref="ByteReader"/> for reading input parameters and an optional <see cref="ByteWriter"/> for writing return data.
    /// </param>
    /// <returns>The <see cref="RemoteMethod"/> instance associated with the specified identifier.</returns>
    public RemoteMethod AddRemoteMethod(string remoteMethodId, bool hasReturnValue, Action<ByteReader, ByteWriter?> invokeMethod)
    {
        Exceptions.EmptyArgument(nameof(remoteMethodId), remoteMethodId);
        Exceptions.NullArgument(nameof(invokeMethod), invokeMethod);

        if (RemoteMethods.TryGetValue(remoteMethodId, out var remoteMethod))
            return remoteMethod;
        
        remoteMethod = new RemoteMethod(remoteMethodId, hasReturnValue, invokeMethod);
        
        RemoteMethods.Add(remoteMethodId, remoteMethod);
        return remoteMethod;
    }

    /// <summary>
    /// Removes a remote method with the specified identifier from the synchronization object.
    /// </summary>
    /// <param name="remoteMethodId">The identifier of the remote method to be removed.</param>
    /// <returns>
    /// Returns <c>true</c> if the remote method was successfully removed; otherwise, <c>false</c>.
    /// </returns>
    public bool RemoveRemoteMethod(string remoteMethodId)
    {
        if (string.IsNullOrWhiteSpace(remoteMethodId))
            return false;
        
        return RemoteMethods.Remove(remoteMethodId);
    }

    /// <summary>
    /// Removes all remote methods associated with this synchronization object instance.
    /// Clears the collection of remote methods to ensure no remote method callbacks remain registered.
    /// </summary>
    public void RemoveAllRemoteMethods()
    {
        RemoteMethods.Clear();
    }
    
    /// <summary>
    /// Adds a new synchronized variable (SyncVar) to the synchronization object, or retrieves an
    /// existing one if it is already registered with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the SyncVar within this synchronization context.</param>
    /// <param name="defaultValue">The default value to assign to the SyncVar if it is newly created.</param>
    /// <typeparam name="T">The type of the value being synchronized.</typeparam>
    /// <returns>A SyncVar instance of the specified type associated with the given identifier.</returns>
    public SyncVar<T> AddSyncVar<T>(string id, T? defaultValue = default)
    {
        if (SyncVars.TryGetValue(id, out var syncVarObj))
            return (SyncVar<T>)syncVarObj;

        var syncVar = new SyncVar<T>(id, this, Parent, defaultValue);
        
        SyncVars.Add(id, syncVar);
        return syncVar;
    }

    /// <summary>
    /// Removes a synchronized variable with the specified identifier from the SyncObject instance.
    /// </summary>
    /// <param name="id">The unique identifier of the synchronized variable to remove.</param>
    /// <typeparam name="T">The type of the synchronized variable.</typeparam>
    /// <returns>True if the synchronized variable was successfully removed; otherwise, false.</returns>
    public bool RemoveSyncVar<T>(string id)
    {
        if (SyncVars.TryGetValue(id, out var syncVarObj) && syncVarObj is SyncVar<T> syncVar)
            syncVar.Remove();
        
        return SyncVars.Remove(id);
    }

    /// <summary>
    /// Removes all synchronized variables (SyncVars) from the synchronization object.
    /// </summary>
    public void RemoveAllSyncVars()
    {
        foreach (var kvp in SyncVars)
        {
            try
            {
                kvp.Value.Remove();
            }
            catch (Exception ex)
            {
                Parent?.Owner?.Log?.Error($"Failed to remove SyncVar '{kvp.Key}' from SyncObject '{Id}': {ex}");
            }
        }
        
        SyncVars.Clear();
    }

    /// <summary>
    /// Sends a payload to the associated synchronization system by invoking a writer action to populate the payload data.
    /// Handles creating, managing, and disposing of the underlying ByteWriter instance used for the payload.
    /// </summary>
    /// <param name="writer">An action that writes the payload data using the provided <see cref="ByteWriter"/> instance.</param>
    public void SendPayload(Action<ByteWriter> writer)
    {
        Exceptions.NullArgument(nameof(writer), writer);
        
        var data = ByteWriter.Get();

        try
        {
            writer(data);

            SendPayload(data);
        }
        catch (Exception ex)
        {
            Parent?.Owner?.Log?.Error($"Failed to write payload for SyncObject '{Id}': {ex}");
        }

        data.ReturnToPool();
    }

    /// <summary>
    /// Sends a payload of data from this SyncObject to its associated owner entity
    /// via the network connection. The data is encapsulated within an
    /// EntityWrappedMessage and includes metadata about this SyncObject.
    /// </summary>
    /// <param name="data">The ByteWriter instance containing the serialized payload
    /// data to be sent. The buffer and position of the ByteWriter are used to construct
    /// the message.</param>
    public void SendPayload(ByteWriter data)
    {
        Parent?.Owner?.SendWrapped(EntityHeader.SyncObjectPayload, writer =>
        {
            writer.WriteString(Id);
            writer.WriteWriter(data);
        });
    }

    /// <summary>
    /// Handles incoming payload data for this SyncObject instance. This method is invoked when
    /// payload data is received, either for initialization or update purposes, depending on the context.
    /// </summary>
    /// <param name="reader">The ByteReader containing the serialized payload data to be processed.</param>
    public virtual void OnPayload(ByteReader reader)
    {
        
    }

    /// <summary>
    /// Called to provide regular update logic for this SyncObject instance.
    /// This method is typically invoked periodically as part of the synchronization lifecycle to handle tasks like maintaining state consistency,
    /// processing remote method responses, or performing other time-sensitive operations.
    /// By default, it removes any expired response handlers where the elapsed time since their request exceeds the configured timeout of the parent.
    /// </summary>
    public virtual void OnUpdate()
    {
        if (ResponseHandlers.Count > 0 && Parent.RemoteMethodTimeout > 0)
        {
            ResponseHandlers.RemoveAll(x => (DateTime.UtcNow - x.RequestTime).TotalSeconds >= Parent.RemoteMethodTimeout);
        }
    }

    /// <summary>
    /// Invoked when a synchronized variable associated with this SyncObject is updated.
    /// This method allows you to handle logic triggered by changes to the specified SyncVar's value.
    /// </summary>
    /// <typeparam name="T">The type of the value held by the SyncVar.</typeparam>
    /// <param name="previousValue">The previous value of the synchronized variable before the update.</param>
    /// <param name="syncVar">The SyncVar instance that was updated.</param>
    public virtual void OnSyncVarUpdated<T>(T? previousValue, SyncVar<T> syncVar)
    {
        
    }
}