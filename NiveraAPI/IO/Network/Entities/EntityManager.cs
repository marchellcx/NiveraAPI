using NiveraAPI.Extensions;
using NiveraAPI.IO.Network.Entities.Messages;
using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;

namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// Provides functionality for managing entities within a networking context.
/// Offers capabilities to register, spawn, retrieve, and manage lifecycle of entities.
/// </summary>
public class EntityManager : NetService
{
    static EntityManager()
    {
        constructors = new();
        
        ObjectSerializer.RegisterDefaultSerializer<EntitySpawnMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<EntityDestroyMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<EntityInvokeMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<EntitySyncVarMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<ConfirmSpawnMessage>(() => new());
    }

    private static readonly Dictionary<string, Func<Entity>> constructors;
    
    /// <summary>
    /// Gets the number of entities that have been registered in the entity manager.
    /// </summary>
    public static int RegisteredEntityCount => constructors.Count;

    /// <summary>
    /// Determines whether an entity of the specified type is registered.
    /// </summary>
    /// <typeparam name="T">The type of the entity to check for registration.</typeparam>
    /// <returns>
    /// True if the entity type is registered; otherwise, false.
    /// </returns>
    public static bool IsRegistered<T>() where T : Entity
        => constructors.ContainsKey(typeof(T).FullName);
    
    /// <summary>
    /// Registers an entity of the specified type with the provided constructor.
    /// </summary>
    /// <typeparam name="T">The type of the entity to register.</typeparam>
    /// <param name="constructor">A function that constructs an instance of the entity.</param>
    /// <returns>
    /// True if the entity type was successfully registered; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided constructor is null.
    /// </exception>
    public static bool RegisterEntity<T>(Func<Entity> constructor) where T : Entity
    {
        if (constructor == null)
            throw new ArgumentNullException(nameof(constructor));

        var type = typeof(T);
        
        if (constructors.ContainsKey(type.FullName))
            return false;
        
        constructors[type.FullName] = constructor;

        var info = EntityInfo.GetInfo(type);
        
        constructors[info.RemoteTypeName(true)] = constructor;
        constructors[info.RemoteTypeName(false)] = constructor;
        
        ReorderConstructors();
        return true;
    }

    /// <summary>
    /// Unregisters an entity of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the entity to unregister.</typeparam>
    /// <returns>
    /// True if the entity type was successfully unregistered; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified type is null.
    /// </exception>
    public static bool UnregisterEntity<T>() where T : Entity
    {
        if (typeof(T) == null)
            throw new ArgumentNullException(nameof(T));

        if (constructors.Remove(typeof(T).FullName))
        {
            var info = EntityInfo.GetInfo(typeof(T));

            constructors.Remove(info.RemoteTypeName(true));
            constructors.Remove(info.RemoteTypeName(false));
            
            ReorderConstructors();
            return true;
        }
        
        return false;
    }

    private static void ReorderConstructors()
    {
        var ordered = constructors
            .OrderBy(kvp => kvp.Key)
            .ToDictionary();
        
        constructors.Clear();
        constructors.AddRange(ordered);
    }
    
    private ushort idEnumerator = 0;

    private List<Type> entitySent = new();
    private List<Entity> entities = new();

    /// <summary>
    /// Gets the time elapsed in the local timer since the last update, in seconds.
    /// </summary>
    public float LocalTime { get; private set; } = 0f;
    
    /// <summary>
    /// Gets the time elapsed in the network timer since the last update, in seconds.
    /// </summary>
    public float NetworkTime { get; private set; } = 0f;
    
    /// <summary>
    /// Gets the total number of ticks that have passed since the peer started.
    /// </summary>
    public long TickCount { get; private set; } = 0;
    
    /// <summary>
    /// Gets the number of entities currently spawned in the entity manager.
    /// </summary>
    public int EntityCount => entities.Count;
    
    /// <summary>
    /// Gets an enumerable collection of all entities in the entity manager.
    /// </summary>
    public IReadOnlyList<Entity> Entities => entities;

    /// <summary>
    /// Counts the number of registered entities of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of entity to count.</typeparam>
    /// <returns>
    /// The total number of entities of the specified type.
    /// </returns>
    public int CountEntities<T>() where T : Entity
        => entities.Count(e => e is T);

    /// <summary>
    /// Retrieves an entity with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>
    /// The entity associated with the specified ID if found.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when an entity with the specified ID is not found.
    /// </exception>
    public Entity GetEntity(ushort id)
    {
        if (TryGetEntity(id, out var entity) || entity == null)
            throw new KeyNotFoundException($"Entity with ID {id} not found.");
        
        return entity;
    }

    /// <summary>
    /// Retrieves an entity with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>
    /// The entity associated with the specified ID if found.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when an entity with the specified ID is not found.
    /// </exception>
    public TEntity GetEntity<TEntity>(ushort id) where TEntity : Entity
    {
        if (!TryGetEntity(id, out TEntity? entity) || entity == null)
            throw new KeyNotFoundException($"Entity with ID {id} not found.");
        
        return entity;
    }
    
    /// <summary>
    /// Attempts to retrieve an entity with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="entity">
    /// When this method returns, contains the entity associated with the specified ID,
    /// if the ID is found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// True if an entity with the specified ID exists; otherwise, false.
    /// </returns>
    public bool TryGetEntity(ushort id, out Entity? entity)
        => (entity = entities.Find(e => e.Id == id)) != null;

    /// <summary>
    /// Attempts to retrieve an entity with the specified ID and cast it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the entity to retrieve.</typeparam>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="entity">The output parameter where the retrieved entity will be stored if successful.</param>
    /// <returns>
    /// True if an entity with the specified ID exists and can be cast to the specified type; otherwise, false.
    /// </returns>
    public bool TryGetEntity<T>(ushort id, out T? entity) where T : Entity
    {
        entity = null!;

        var obj = entities.Find(e => e.Id == id);
        
        if (obj is not T cast)
            return false;
        
        entity = cast;
        return true;
    }

    /// <summary>
    /// Attempts to retrieve the first entity of the specified type available in the collection.
    /// </summary>
    /// <typeparam name="T">The type of the entity to be retrieved.</typeparam>
    /// <param name="entity">When this method returns, contains the entity of the specified type, if found; otherwise, null.</param>
    /// <returns>
    /// True if an entity of the specified type is found; otherwise, false.
    /// </returns>
    public bool TryGetFirstEntity<T>(out T entity) where T : Entity
    {
        entity = null!;

        foreach (var obj in entities)
        {
            if (obj is T cast)
            {
                entity = cast;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        
        foreach (var entity in entities.ToArray())
        {
            try
            {
                DestroyEntity(entity);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not destroy entity &1{entity.Id}&r:\n{ex}");
            }
        }

        idEnumerator = 0;
        
        entities.Clear();
        entitySent.Clear();
    }

    /// <summary>
    /// Updates the state of all registered entities by invoking their individual update methods.
    /// </summary>
    /// <param name="localDeltaTime">The elapsed time for the current frame in a local context.</param>
    /// <param name="networkDeltaTime">The elapsed time for the current frame in the network context.</param>
    public override void Update(float networkDeltaTime, float localDeltaTime)
    {
        base.Update(networkDeltaTime, localDeltaTime);

        LocalTime = localDeltaTime;
        NetworkTime = networkDeltaTime;

        TickCount++;
        
        try
        {
            for (var x = 0; x < entities.Count; x++)
            {
                try
                {
                    var entity = entities[x];
                    
                    if (entity.destroyed || !entity.confirmed)
                        continue;
                    
                    entity.OnUpdate(localDeltaTime, networkDeltaTime);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to update entity &1{entities[x].Id}&r:\n{ex}");
                }
            }   
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to invoke entity update:\n{ex}");
        }
    }

    /// <summary>
    /// Destroys the specified entity, removing it from the entity manager and marking it as destroyed.
    /// </summary>
    /// <param name="entity">The entity to destroy. Must not be null and must belong to this entity manager.</param>
    /// <returns>
    /// True if the entity was successfully destroyed; otherwise, false.
    /// Returns false if the entity is already destroyed, does not belong to this manager, or is null.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to destroy an entity on the client. </exception>
    public bool DestroyEntity(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        if (!IsServer)
            throw new InvalidOperationException("Cannot destroy entity on client.");

        if (entity.destroyed)
        {
            Log.Warn($"Attempted to destroy already destroyed entity: &1{entity.Id}&r");
            return false;
        }

        if (entity.Manager == null)
        {
            Log.Warn($"Attempted to destroy entity with no manager: &1{entity.Id}&r");
            return false;
        }

        if (entity.Manager != this)
        {
            Log.Warn($"Attempted to destroy entity with incorrect manager: &1{entity.Id}&r");
            return false;       
        }

        Log.Debug($"Destroying entity &1{entity.Id}&r");

        entities.Remove(entity);
        entity.destroyed = true;

        try
        {
            entity.OnDestroyed();
        }
        catch (Exception ex)
        {
            Log.Error($"Could not destroy entity &1{entity.Id}&r:\n{ex}");
        }
        
        Log.Debug($"Entity &1{entity.Id}&r destroyed, sending message to client ..");

        Send(new EntityDestroyMessage(entity.Id));
        return true;
    }

    /// <summary>
    /// Spawns an entity of the specified type on the server.
    /// </summary>
    /// <typeparam name="T">The type of the entity to spawn. The entity type must be registered.</typeparam>
    /// <returns>
    /// The spawned entity instance of the specified type.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the entity type is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the client is disconnected, the operation is attempted on a non-server instance,
    /// or the specified entity type is not registered.
    /// </exception>
    public T SpawnEntity<T>() where T : Entity
        => (T)SpawnEntity(typeof(T));

    /// <summary>
    /// Spawns a new entity of the specified type on the server.
    /// </summary>
    /// <param name="type">The type of the entity to spawn. Must be a registered entity type.</param>
    /// <returns>The newly spawned entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the specified type is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the client is disconnected, if the client is not the server, or if the specified entity type is not registered.
    /// </exception>
    public Entity SpawnEntity(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!IsConnected)
            throw new InvalidOperationException("Cannot spawn entity on disconnected client.");

        if (!IsServer)
            throw new InvalidOperationException("Cannot spawn entity on client.");

        if (!constructors.TryGetValue(type.FullName, out var constructor))
            throw new InvalidOperationException($"Entity of type {type.FullName} is not registered.");
        
        var entity = constructor();
        
        InitEntity(entity, idEnumerator++);
        
        var msg = new EntitySpawnMessage((ushort)constructors.FindKeyIndex(entity.Info.RemoteTypeName(IsServer)), entity.Id);

        WriteCmds(entity, ref msg);
        
        Send(msg);
        
        entity.OnServerSpawned();
        return entity;
    }

    /// <summary>
    /// Attempts to spawn an entity of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the entity to spawn.</typeparam>
    /// <param name="entity">
    /// When this method returns, contains the spawned entity if the operation was successful; otherwise, null.
    /// </param>
    /// <returns>
    /// True if the entity was successfully spawned; otherwise, false.
    /// </returns>
    public bool TrySpawnEntity<T>(out T entity) where T : Entity
    {
        entity = null!;

        if (!TrySpawnEntity(typeof(T), out var obj)
            || obj is not T cast)
            return false;
        
        entity = cast;
        return true;
    }

    /// <summary>
    /// Attempts to spawn an entity of the specified type.
    /// </summary>
    /// <param name="type">The type of the entity to spawn.</param>
    /// <param name="entity">
    /// When this method returns, contains the spawned entity if the operation was successful; otherwise, null.
    /// </param>
    /// <returns>
    /// True if the entity was successfully spawned; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the specified <paramref name="type"/> is null.
    /// </exception>
    public bool TrySpawnEntity(Type type, out Entity entity)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "Type cannot be null.");

        entity = null!;

        if (!IsConnected)
        {
            Log.Warn($"Attempted to spawn entity on disconnected client: &1{type.FullName}&r");
            return false;
        }

        if (!IsServer)
        {
            Log.Warn($"Attempted to spawn entity on client: &1{type.FullName}&r");
            return false;
        }

        if (!constructors.TryGetValue(type.FullName, out var constructor))
        {
            Log.Warn($"Attempted to spawn entity of unknown type: &1{type.FullName}&r");
            return false;
        }

        try
        {
            entity = constructor();

            InitEntity(entity, idEnumerator++);
            
            var msg = new EntitySpawnMessage((ushort)constructors.FindKeyIndex(entity.Info.RemoteTypeName(IsServer)), entity.Id);

            WriteCmds(entity, ref msg);
        
            Send(msg);

            entity.OnServerSpawned();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to spawn entity of type &1{type.FullName}&r:\n{ex}");
            return false;
        }
        
        return true;
    }
    
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
    public override bool Receive(ISerializableObject serializableObject)
    {
        switch (serializableObject)
        {
            case EntitySpawnMessage spawnMsg:
                OnEntitySpawnMessage(spawnMsg);
                return true;
            
            case EntityInvokeMessage invokeMsg:
                OnEntityInvokeMessage(invokeMsg);
                return true;
            
            case EntityDestroyMessage destroyMsg:
                OnEntityDestroyMessage(destroyMsg);
                return true;
            
            case EntitySyncVarMessage syncVarMsg:
                OnEntitySyncVarMessage(syncVarMsg);
                return true;
            
            case ConfirmSpawnMessage confirmSpawnMsg:
                OnConfirmSpawnMessage(confirmSpawnMsg);
                return true;
        }
        
        return base.Receive(serializableObject);
    }

    private void OnEntitySyncVarMessage(EntitySyncVarMessage msg)
    {
        var entity = entities.Find(e => e.Id == msg.Entity);
        
        if (entity == null)
        {
            Log.Warn($"Received entity sync var for an unknown entity: &1{msg.Entity}&r");
            return;
        }

        try
        {
            entity.OnEntitySyncVarMessage(msg);
        }   
        catch (Exception ex)
        {
            Log.Error($"Failed to handle entity invoke:\n{ex}");
        }
    }

    private void OnEntityInvokeMessage(EntityInvokeMessage msg)
    {
        var entity = entities.Find(e => e.Id == msg.Entity);
        
        if (entity == null)
        {
            Log.Warn($"Received entity invoke for an unknown entity: &1{msg.Id}&r");
            return;
        }

        try
        {
            entity.OnEntityInvokeMessage(msg);
        }   
        catch (Exception ex)
        {
            Log.Error($"Failed to handle entity invoke:\n{ex}");
        }
    }

    private void OnEntityDestroyMessage(EntityDestroyMessage msg)
    {
        if (IsServer)
        {
            Log.Warn($"Received entity destroy message on server: &1{msg.Id}&r");
            return;
        }
        
        var entity = entities.Find(e => e.Id == msg.Id);
        
        if (entity == null)
        {
            Log.Warn($"Received entity destroy message for unknown entity: &1{msg.Id}&r");
            return;
        }
        
        Log.Debug($"Received entity destroy message for entity &1{entity.Id}&r");

        try
        {
            if (!entity.destroyed)
            {
                entity.destroyed = true;
                entity.OnDestroyed();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Could not destroy entity:\n{ex}");
        }
    }
    
    private void OnConfirmSpawnMessage(ConfirmSpawnMessage msg)
    {
        if (!IsServer)
        {
            Log.Warn($"Received spawn confirmation message on client: &1{msg.Id}&r");
            return;
        }
        
        var entity = entities.Find(e => e.Id == msg.Id);        
        
        if (entity == null)
        {
            Log.Warn($"Received spawn confirmation message for unknown entity: &1{msg.Id}&r");
            return;
        }

        if (entity.confirmed)
        {
            Log.Warn($"Received duplicate spawn confirmation message for entity &1{entity.Id}&r");
            return;
        }

        try
        {
            entity.Info.ReadRpcs(entity, msg);
            
            entity.OnClientConfirmed();
            entity.confirmed = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Could not confirm spawn for entity &1{entity.Id}&r:\n{ex}");
        }
    }

    private void OnEntitySpawnMessage(EntitySpawnMessage msg)
    {
        if (!IsClient)
        {
            Log.Warn($"Received entity spawn message on server: &1{msg.Id}&r");
            return;
        }
        
        var constructor = constructors.ElementAtOrDefault(msg.Type);
        
        if (constructor.Value == null)
        {
            Log.Warn($"Received entity spawn message for unknown type: &1{msg.Type}&r");
            return;
        }
        
        Log.Debug($"Found constructor: &1{constructor.Key}&r, spawning entity ..");

        try
        {
            var entity = constructor.Value();
            
            InitEntity(entity, msg.Id);
            
            var confirmSpawnMessage = new ConfirmSpawnMessage(msg.Id);
            
            WriteRpcs(entity, ref confirmSpawnMessage);
            
            Send(confirmSpawnMessage);
            
            entity.Info.ReadCmds(entity, msg);
            entity.OnClientSpawned();
            entity.confirmed = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to spawn entity of type &1{msg.Type}&r:\n{ex}");
        }
    }

    private void WriteCmds(Entity entity, ref EntitySpawnMessage msg)
    {
        var type = entity.GetType();

        if (!entitySent.AddUnique(type))
            return;
        
        entity.Info.WriteCmds(entity, ref msg);
    }

    private void WriteRpcs(Entity entity, ref ConfirmSpawnMessage msg)
    {
        var type = entity.GetType();

        if (!entitySent.AddUnique(type))
            return;
        
        entity.Info.WriteRpcs(entity, ref msg);
    }

    private void InitEntity(Entity entity, ushort id)
    {
        entity.Id = id;
        entity.Manager = this;
        
        entity.Info = EntityInfo.GetInfo(entity.GetType());
        
        entities.Add(entity);
    }
}