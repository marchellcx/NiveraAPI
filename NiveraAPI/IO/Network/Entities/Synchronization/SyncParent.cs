using NiveraAPI.Extensions;
using NiveraAPI.IO.Network.Entities.Synchronization.Interfaces;
using NiveraAPI.IO.Network.Entities.Synchronization.Objects;
using NiveraAPI.IO.Serialization;

using NiveraAPI.Pooling;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network.Entities.Synchronization;

/// <summary>
/// Represents a synchronization parent that manages synchronization variables and objects for a network entity.
/// Provides mechanisms to add, retrieve, and remove synchronization variables and objects, while maintaining authority verification.
/// </summary>
public class SyncParent
{
    /// <summary>
    /// Represents the network entity that owns this synchronization instance. This property provides
    /// access to the parent entity associated with the current synchronization object, enabling interaction
    /// with the owning entity's state, events, and lifecycle methods.
    /// </summary>
    public Entity Owner { get; }

    /// <summary>
    /// Indicates whether the current synchronization instance has the authority to perform state modifications
    /// or initiate updates. This property is used to enforce control mechanisms, ensuring that only entities
    /// with proper privileges are able to alter or synchronize values within the networked environment.
    /// </summary>
    public bool HasAuthority { get; set; }

    /// <summary>
    /// Specifies the maximum duration, in seconds, after which a pending remote method response
    /// is considered timed out. This property is used to remove expired response handlers
    /// associated with remote method calls, ensuring resource cleanup and preventing indefinite waits.
    /// </summary>
    public int RemoteMethodTimeout { get; set; } = 10;
    
    /// <summary>
    /// A dictionary that maps unique identifiers (keys) to their corresponding synchronized
    /// variable objects (values). This property is used for tracking and managing state
    /// synchronization of variables across networked entities.
    /// </summary>
    public Dictionary<string, ISyncVar> SyncVars { get; } = new();

    /// <summary>
    /// A collection of synchronized objects utilized for maintaining state consistency
    /// across networked entities. Each entry in the collection is associated with
    /// a unique identifier (key) and its corresponding <see cref="SyncObject"/> (value).
    /// </summary>
    public Dictionary<string, SyncObject> SyncObjects { get; } = new();

    /// <summary>
    /// Represents a collection of remote methods available for invocation on the current synchronization entity.
    /// Each remote method in this dictionary is identified by a unique string key and provides functionality
    /// to execute methods remotely, handle their invocations, and optionally return results. This property
    /// supports dynamic addition, removal, and retrieval of remote methods tied to the synchronization instance.
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
    /// Creates a new synchronization parent instance for the specified network entity.
    /// </summary>
    /// <param name="owner">The network entity that owns this synchronization instance.</param>
    /// <param name="overrideAuthority">Indicates whether to override the authority check for this synchronization instance.</param>
    public SyncParent(Entity owner, bool overrideAuthority)
    {
        Owner = owner;
        HasAuthority = owner.Connection!.IsServer || overrideAuthority;

        if (Owner.IsConfirmed)
            OnConfirmed();
        else
            Owner.Confirmed += OnConfirmed;
    }

    /// <summary>
    /// Invokes a remote method associated with the specified identifier, optionally including additional payload data.
    /// </summary>
    /// <param name="remoteMethodId">The unique identifier for the remote method to be called.</param>
    /// <param name="payloadWriter">An optional writer to serialize additional data to be sent with the method invocation.</param>
    /// <param name="responseHandler">An optional handler to process the response data received from the remote method invocation.</param>
    public void CallRemoteMethod(string remoteMethodId, ByteWriter? payloadWriter, Action<ByteReader>? responseHandler = null)
    {
        Exceptions.EmptyArgument(nameof(remoteMethodId), remoteMethodId);

        var responseId = string.Empty;
        
        if (responseHandler != null)
        {
            responseId = Guid.NewGuid().ToString();
            
            ResponseHandlers.Add(new(responseId, DateTime.UtcNow, responseHandler));
        }
        
        Owner.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(remoteMethodId);
            writer.WriteString(string.Empty);
            writer.WriteString(responseId);

            if (payloadWriter != null)
            {
                writer.WriteBool(true);
                writer.WriteWriter(payloadWriter);
            }
            else
            {
                writer.WriteBool(false);
            }
        });
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
    /// Adds a remote method to the synchronization parent if it does not already exist.
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
    /// Adds a remote method to the synchronization parent instance.
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
    /// Removes a remote method with the specified identifier from the synchronization parent.
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
    /// Removes all remote methods associated with this synchronization parent instance.
    /// Clears the collection of remote methods to ensure no remote method callbacks remain registered.
    /// </summary>
    public void RemoveAllRemoteMethods()
    {
        RemoteMethods.Clear();
    }

    /// <summary>
    /// Retrieves the synchronization object of the specified type and identifier if it exists.
    /// </summary>
    /// <typeparam name="TObject">The type of the synchronization object to retrieve.</typeparam>
    /// <param name="syncObjectId">The unique identifier of the synchronization object to retrieve.</param>
    /// <returns>The synchronization object of the specified type if found; otherwise, null.</returns>
    public TObject? GetSyncObject<TObject>(string syncObjectId) where TObject : SyncObject
    {
        Exceptions.EmptyArgument(nameof(syncObjectId), syncObjectId);
        return SyncObjects.TryGetValue(syncObjectId, out var obj) ? (obj as TObject) : null;
    }

    /// <summary>
    /// Retrieves a synchronization object identified by the specified ID.
    /// </summary>
    /// <param name="syncObjectId">The unique identifier of the synchronization object to retrieve.</param>
    /// <returns>
    /// The synchronization object associated with the specified ID, or null if no matching object is found.
    /// </returns>
    public SyncObject? GetSyncObject(string syncObjectId)
    {
        Exceptions.EmptyArgument(nameof(syncObjectId), syncObjectId);
        return SyncObjects.TryGetValue(syncObjectId, out var syncObject) ? syncObject : null;
    }

    /// <summary>
    /// Attempts to retrieve a synchronization object of the specified type using the specified identifier.
    /// </summary>
    /// <param name="syncObjectId">The identifier of the synchronization object to retrieve.</param>
    /// <param name="syncObject">When this method returns, contains the synchronization object of the specified type, if found; otherwise, null.</param>
    /// <typeparam name="TObject">The type of synchronization object to retrieve.</typeparam>
    /// <returns>True if the synchronization object is found and is of the specified type; otherwise, false.</returns>
    public bool TryGetSyncObject<TObject>(string syncObjectId, out TObject syncObject) where TObject : SyncObject
    {
        Exceptions.EmptyArgument(nameof(syncObjectId), syncObjectId);

        syncObject = null!;
        
        if (!SyncObjects.TryGetValue(syncObjectId, out var obj))
            return false;

        return (syncObject = obj as TObject) != null;
    }

    /// <summary>
    /// Attempts to retrieve a synchronization object from the collection using the specified identifier.
    /// </summary>
    /// <param name="syncObjectId">The unique identifier of the synchronization object to retrieve.</param>
    /// <param name="syncObject">When the method returns, contains the synchronization object associated with the specified identifier, if it exists; otherwise, the default value of the output parameter.</param>
    /// <returns>True if the synchronization object was found; otherwise, false.</returns>
    public bool TryGetSyncObject(string syncObjectId, out SyncObject syncObject)
    {
        Exceptions.EmptyArgument(nameof(syncObjectId), syncObjectId);
        return SyncObjects.TryGetValue(syncObjectId, out syncObject);
    }

    /// <summary>
    /// Adds a new synchronization object of the specified type and associates it with the given identifier.
    /// </summary>
    /// <param name="syncObjectId">The unique identifier for the synchronization object.</param>
    /// <typeparam name="TObject">The type of the synchronization object to instantiate. Must inherit from <see cref="SyncObject"/>.</typeparam>
    /// <returns>The newly created synchronization object of type <typeparamref name="TObject"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided <paramref name="syncObjectId"/> is empty or whitespace.</exception>
    /// <exception cref="Exception">Thrown when the instantiation of the synchronization object fails.</exception>
    public TObject AddSyncObject<TObject>(string syncObjectId) where TObject : SyncObject
    {
        Exceptions.EmptyArgument(nameof(syncObjectId), syncObjectId);
        return (TObject)AddSyncObject(typeof(TObject), syncObjectId, true)!;
    }

    /// <summary>
    /// Adds a new synchronization object of the specified type with the given ID to the current synchronization parent.
    /// </summary>
    /// <param name="syncObjectType">The type of the synchronization object to create and add.</param>
    /// <param name="syncObjectId">The unique identifier for the synchronization object.</param>
    /// <param name="sendSpawnMessage">Whether or not to send a spawn message to the client.</param>
    /// <param name="payloadReader">Reader for data received from the client during synchronization.</param>
    /// <returns>The created and added synchronization object, or null if the object could not be instantiated.</returns>
    public SyncObject? AddSyncObject(Type syncObjectType, string syncObjectId, bool sendSpawnMessage = true, ByteReader? payloadReader = null)
    {
        Exceptions.NullArgument(nameof(syncObjectType), syncObjectType);
        Exceptions.EmptyArgument(nameof(syncObjectId), syncObjectId);

        if (TryGetSyncObject(syncObjectId, out var syncObject))
            return syncObject;
        
        if (!HasAuthority && sendSpawnMessage)
        {
            Owner.Log.Error($"Attempted to add synchronization object &3{syncObjectId}&r without authority.");
            return null;
        }

        if (!SyncObject.SyncObjectTypes.TryGetKey(syncObjectType, out var typeName))
        {
            Owner.Log.Error($"Could not find sync object type &3{syncObjectType}&r.");
            return null;
        }
        
        if ((syncObject = (Activator.CreateInstance(syncObjectType) as SyncObject)) == null)
        {
            Owner.Log.Error($"Could not instantiate sync object of type &3{syncObjectType}&r.");
            return null;
        }

        syncObject.Id = syncObjectId;
        syncObject.Parent = this;

        SyncObjects[syncObjectId] = syncObject;

        var payloadWriter = ObjectPool<ByteWriter>.Shared.Rent();

        try
        {
            if (sendSpawnMessage)
            {
                syncObject.OnLocalSpawned(payloadWriter);

                Owner.SendWrapped(EntityHeader.SyncParentMessage, writer =>
                {
                    writer.WriteByte(0);

                    writer.WriteString(typeName);
                    writer.WriteString(syncObjectId);
                    
                    writer.WriteWriter(payloadWriter);
                });
            }
            else
            {
                syncObject.OnRemoteSpawned((payloadReader?.IsEof ?? true) ? null : payloadReader);
            }
        }
        catch (Exception ex)
        {
            Owner.Log.Error($"Failed to spawn synchronization object: {ex}");
        }

        ObjectPool<ByteWriter>.Shared.Return(payloadWriter);
        return syncObject;
    }

    /// <summary>
    /// Removes a synchronization object from the current synchronization parent by its identifier.
    /// </summary>
    /// <param name="syncObjectId">The unique identifier of the synchronization object to be removed.</param>
    /// <returns>
    /// Returns <c>true</c> if the synchronization object with the specified identifier is found and removed;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool RemoveSyncObject(string syncObjectId)
    {
        Exceptions.EmptyArgument(nameof(syncObjectId), syncObjectId);

        if (SyncObjects.TryGetValue(syncObjectId, out var syncObject))
        {
            try
            {
                syncObject.OnDespawned();
            }
            catch (Exception ex)
            {
                Owner.Log.Error($"Failed to despawn synchronization object:\n{ex}");
            }

            return SyncObjects.Remove(syncObjectId);
        }

        return false;
    }

    /// <summary>
    /// Adds a new synchronized variable (SyncVar) to the synchronization parent, or retrieves an
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

        var syncVar = new SyncVar<T>(id, null, this, defaultValue);
        
        SyncVars.Add(id, syncVar);
        return syncVar;
    }

    /// <summary>
    /// Removes a synchronized variable with the specified identifier from the SyncParent instance.
    /// </summary>
    /// <param name="id">The unique identifier of the synchronized variable to remove.</param>
    /// <typeparam name="T">The type of the synchronized variable.</typeparam>
    /// <returns>True if the synchronized variable was successfully removed; otherwise, false.</returns>
    public bool RemoveSyncVar<T>(string id)
    {
        if (SyncVars.TryGetValue(id, out var syncVarObj) && syncVarObj is SyncVar<T> syncVar)
        {
            try
            {
                syncVar.Remove();
            }
            catch (Exception ex)
            {
                Owner.Log.Error($"Failed to remove synchronization variable:\n{ex}");
            }
        }

        return SyncVars.Remove(id);
    }

    /// <summary>
    /// Removes all synchronized variables (SyncVars) from the synchronization parent
    /// and triggers the destruction event for these variables, if subscribed.
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
                Owner.Log.Error($"Failed to remove synchronization variable:\n{ex}");
            }
        }
        
        SyncVars.Clear();
    }

    /// <summary>
    /// Removes all synchronization objects associated with the current synchronization parent.
    /// </summary>
    /// <remarks>
    /// This method invokes the <c>OnDespawned</c> method for each synchronization object and then clears the collection
    /// that holds the synchronization objects.
    /// </remarks>
    public void RemoveAllSyncObjects()
    {
        foreach (var kvp in SyncObjects)
        {
            try
            {
                kvp.Value.OnDespawned();
            }
            catch (Exception ex)
            {
                Owner.Log.Error($"Failed to despawn synchronization object:\n{ex}");
            }
        }
        
        SyncObjects.Clear();
        
        Owner?.SendWrapped(EntityHeader.SyncParentMessage, writer => writer.WriteByte(2));
    }
    

    internal void Destroy()
    {
        RemoveAllSyncVars();
        RemoveAllSyncObjects();
        RemoveAllRemoteMethods();
        
        ResponseHandlers.Clear();
    }

    internal void Update()
    {
        if (ResponseHandlers.Count > 0 && RemoteMethodTimeout > 0)
        {
            ResponseHandlers.RemoveAll(x => (DateTime.UtcNow - x.RequestTime).TotalSeconds >= RemoteMethodTimeout);
        }

        foreach (var kvp in SyncObjects)
        {
            try
            {
                kvp.Value.OnUpdate();
            }
            catch (Exception ex)
            {
                Owner.Log.Error($"An error occurred while updating sync object &3{kvp.Key}&r: {ex}");
            }
        }
    }

    internal void OnResponseMessage(ByteReader reader)
    {
        var responseId = reader.ReadString();

        if (!ResponseHandlers.TryGetFirst(x => x.RequestId == responseId, out var responseHandler))
        {
            Owner.Log.Error($"Could not find a matching response handler for ID &3{responseId}&r");
            return;
        }
        
        ResponseHandlers.RemoveAll(x => x.RequestId == responseId);

        try
        {
            responseHandler.Handler(reader);
        }
        catch (Exception ex)
        {
            Owner.Log.Error($"An error occurred while handling response &3{responseId}&r: {ex}");
        }
    }

    internal void OnMethodMessage(ByteReader reader)
    {
        var remoteMethodId = reader.ReadString();
        var syncObjectId = reader.ReadString();
        var responseId = reader.ReadString();

        RemoteMethod? method = null;

        if (!string.IsNullOrWhiteSpace(syncObjectId))
        {
            if (!SyncObjects.TryGetValue(syncObjectId, out var syncObject))
            {
                Owner.Log.Error($"Unknown sync object &3{syncObjectId}&r.");
                return;
            }

            if (!syncObject.RemoteMethods.TryGetValue(remoteMethodId, out var remoteMethod))
            {
                Owner.Log.Error($"Unknown remote method &3{remoteMethodId}&r.");
                return;
            }

            method = remoteMethod;
        }
        else if (RemoteMethods.TryGetValue(remoteMethodId, out var remoteMethod))
        {
            method = remoteMethod;
        }
        else
        {
            Owner.Log.Error($"Unknown remote method &3{remoteMethodId}&r.");
            return;
        }

        if (method.HasReturnValue)
        {
            var responseWriter = ByteWriter.Get();

            try
            {
                method.Invoke(reader, responseWriter);
            }
            catch (Exception ex)
            {
                Owner.Log.Error($"Error invoking remote method &3{remoteMethodId}&r: {ex}");
            }
            
            Owner.SendWrapped(EntityHeader.Response, writer =>
            {
                writer.WriteString(responseId);
                writer.WriteWriter(responseWriter);
            });
        }
        else
        {
            try
            {
                method.Invoke(reader, null);
            }
            catch (Exception ex)
            {
                Owner.Log.Error($"Error invoking remote method &3{remoteMethodId}&r: {ex}");
            }
        }
    }

    internal void OnSyncParentMessage(ByteReader reader)
    {
        var typeId = reader.ReadByte();

        switch (typeId)
        {
            case 0: // Spawn SyncObject
            {
                var typeName = reader.ReadString();
                var syncObjectId = reader.ReadString();

                if (!SyncObject.SyncObjectTypes.TryGetValue(typeName, out var syncObjectType))
                {
                    Owner.Log.Error($"Unknown sync object type &3{typeName}&r.");
                    return;
                }
                
                var syncObject = AddSyncObject(syncObjectType, syncObjectId, false, reader);

                if (syncObject == null)
                {
                    Owner.Log.Error($"Failed to instantiate sync object with ID &3{syncObjectId}&r.");
                    return;
                }

                break;
            }

            case 1: // Destroy SyncObject
            {
                var syncObjectId = reader.ReadString();
                
                if (!SyncObjects.TryGetValue(syncObjectId, out var syncObject))
                {
                    Owner.Log.Error($"Sync object with ID &3{syncObjectId}&r not found.");
                    return;
                }
                
                syncObject.OnDespawned();
                
                SyncObjects.Remove(syncObjectId);
                break;
            }

            case 2: // Destroy All SyncObjects
            {
                foreach (var kvp in SyncObjects)
                {
                    kvp.Value.OnDespawned();
                }
                
                SyncObjects.Clear();
                break;
            }
        }
    }

    internal void OnSyncObjectPayload(ByteReader reader)
    {
        var syncObjectId = reader.ReadString();

        if (!SyncObjects.TryGetValue(syncObjectId, out var syncObject))
        {
            Owner.Log.Error($"Sync object with ID &3{syncObjectId}&r not found.");
            return;
        }

        syncObject.OnPayload(reader);
    }

    internal void OnSyncVarMessage(ByteReader reader)
    {
        var syncVarObjectId = reader.ReadString();
        var syncVarId = reader.ReadString();

        if (!string.IsNullOrWhiteSpace(syncVarObjectId))
        {
            if (!SyncObjects.TryGetValue(syncVarObjectId, out var syncObject))
            {
                Owner.Log.Error($"Sync object with ID &3{syncVarObjectId}&r not found.");
                return;
            }
            
            if (!syncObject.SyncVars.TryGetValue(syncVarId, out var syncVar))
            {
                Owner.Log.Error($"Sync var with ID &3{syncVarId}&r not found.");
                return;
            }

            syncVar.Receive(reader);
        }
        else if (SyncVars.TryGetValue(syncVarId, out var syncVar))
        {
            syncVar.Receive(reader);
        }
        else
        {
            Owner.Log.Error($"Sync var with ID &3{syncVarId}&r not found.");
        }
    }

    private void OnConfirmed()
    {
        foreach (var kvp in SyncVars)
        {
            try
            {
                kvp.Value.Confirmed();
            }
            catch (Exception ex)
            {
                Owner.Log.Error($"Sync var with ID &3{kvp.Key}&r failed to confirm:\n{ex}");
            }
        }
    }
}