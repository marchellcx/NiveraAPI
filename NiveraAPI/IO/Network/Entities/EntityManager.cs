using NiveraAPI.Extensions;

using NiveraAPI.IO.Network.Entities.Messages;

using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;

using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// Provides functionality for managing entities within a networking context.
/// Offers capabilities to register, spawn, retrieve, and manage lifecycle of entities.
/// </summary>
public class EntityManager : NetService
{
    /// <summary>
    /// Represents detailed information about an entity, including its type classification,
    /// network mapping, and a factory method for constructing instances.
    /// Used primarily within the context of entity management in a networked environment.
    /// </summary>
    public struct EntityInfo
    {
        /// <summary>
        /// Represents the local type of the entity, which is used for internal identification and classification
        /// within the system for managing entity-specific logic and behavior.
        /// </summary>
        public readonly string LocalType;

        /// <summary>
        /// Represents the remote type of the entity, which is used for network identification and mapping of entity logic
        /// across different systems or instances.
        /// </summary>
        public readonly string? RemoteType;

        /// <summary>
        /// Represents a type of an entity in the system. Used to associate the entity's logic with its type information.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Defines a delegate that serves as a constructor function for creating new instances of entities.
        /// </summary>
        public readonly Func<Entity> Constructor;
        
        /// <summary>
        /// Creates a new instance of the <see cref="EntityInfo"/> struct.
        /// </summary>
        /// <param name="localType">The local type of the entity.</param>
        /// <param name="remoteType">The remote type of the entity.</param>
        /// <param name="type">The type of the entity.</param>
        /// <param name="constructor">The constructor function for creating the entity.</param>
        public EntityInfo(string localType, string? remoteType, Type type, Func<Entity> constructor)
        {
            LocalType = localType;
            RemoteType = remoteType;
            Type = type;
            Constructor = constructor;
        }
    }

    static EntityManager()
    {
        ObjectSerializer.RegisterDefaultSerializer<EntitySpawnMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<EntityDestroyMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<EntityWrappedMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<ConfirmSpawnMessage>(() => new());
    }
    
    private static readonly List<EntityInfo> registeredEntities = new();

    /// <summary>
    /// Provides a mapping of entity types to their respective constructors,
    /// enabling the dynamic creation of entities in the networking context.
    /// </summary>
    public static IReadOnlyList<EntityInfo> RegisteredEntities => registeredEntities;

    /// <summary>
    /// Registers a new entity type with the given remote and local identifiers and a constructor function.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to register, which must inherit from <see cref="Entity"/>.</typeparam>
    /// <param name="remoteType">The remote type identifier for the entity.</param>
    /// <param name="localType">The local type identifier for the entity.</param>
    /// <param name="constructor">A function that constructs an instance of the entity.</param>
    /// <returns>True if the registration was successful; otherwise, false.</returns>
    public static bool RegisterEntity<TEntity>(string? remoteType, string? localType, Func<Entity> constructor) where TEntity : Entity
    {
        Exceptions.NullArgument(nameof(constructor), constructor);
        return RegisterEntity(remoteType, localType, typeof(TEntity), constructor);
    }

    /// <summary>
    /// Registers a new entity type with the specified local and optional remote type.
    /// </summary>
    /// <param name="remoteType">The optional remote identifier for the entity type. This can be null or empty if not used.</param>
    /// <param name="localType">The local identifier for the entity type. This value must not be null or empty.</param>
    /// <param name="type">The type of the entity being registered. This value must not be null.</param>
    /// <param name="constructor">The constructor function used to create instances of the entity. This value must not be null.</param>
    /// <returns>
    /// A boolean value indicating whether the entity was successfully registered.
    /// Returns true if the entity was registered, or false if an entity with the same local type already exists.
    /// </returns>
    public static bool RegisterEntity(string? remoteType, string? localType, Type type, Func<Entity> constructor)
    {
        Exceptions.NullArgument(nameof(type), type);
        Exceptions.NullArgument(nameof(constructor), constructor);
        
        if (registeredEntities.Any(x => x.Type == type))
            return false;

        if (string.IsNullOrWhiteSpace(localType))
            localType = type.ToString();
        
        if (string.IsNullOrWhiteSpace(remoteType))
            remoteType = localType;

        registeredEntities.Add(new EntityInfo(localType!, remoteType, type, constructor));
        return true;
    }
    
    private ushort idEnumerator = 0;
    private List<Entity> entities = new();

    /// <summary>
    /// Whether or not to log debug messages.
    /// </summary>
    public bool DebugLogs => Connection?.DebugLogs ?? false;
    
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
                LocalDestroy(entity);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not destroy entity &1{entity.Id}&r:\n{ex}");
            }
        }

        idEnumerator = 0;
        
        entities.Clear();
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
                    
                    if (entity.IsDestroyed || !entity.IsConfirmed)
                        continue;
                    
                    entity.OnUpdate();
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
        if (LocalDestroy(entity) && IsConnected)
        {
            Log.DebugIf($"Entity &1{entity.Id}&r destroyed, sending message to client ..", DebugLogs);

            Send(new EntityDestroyMessage(entity.Id));
            return true;
        }

        return false;
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

        if (!registeredEntities.TryGetFirst(x => x.Type == type, out var info))
            throw new InvalidOperationException($"Entity type {type} is not registered.");
        
        if (string.IsNullOrEmpty(info.RemoteType))
            throw new InvalidOperationException($"Entity type {type} does not have a remote type registered.");

        var entity = info.Constructor();
        
        if (entity == null)
            throw new InvalidOperationException($"Could not construct entity type {type}");
        
        InitEntity(entity, idEnumerator++);
        
        Send(new EntitySpawnMessage(info.RemoteType!, entity.Id));
        
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

        if (!registeredEntities.TryGetFirst(x => x.Type == type, out var info))
        {
            Log.Warn($"Attempted to spawn entity of unknown type: &1{type.FullName}&r");
            return false;
        }

        if (string.IsNullOrEmpty(info.RemoteType))
        {
            Log.Warn($"Attempted to spawn entity of type {type.FullName} without a remote type registered.");
            return false;
        }

        try
        {
            entity = info.Constructor();

            InitEntity(entity, idEnumerator++);
            
            Send(new EntitySpawnMessage(info.RemoteType!, entity.Id));

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
            
            case EntityDestroyMessage destroyMsg:
                OnEntityDestroyMessage(destroyMsg);
                return true;
            
            case EntityWrappedMessage wrappedMsg:
                OnEntityWrappedMessage(wrappedMsg);
                return true;
            
            case ConfirmSpawnMessage confirmSpawnMsg:
                OnConfirmSpawnMessage(confirmSpawnMsg);
                return true;
        }
        
        return base.Receive(serializableObject);
    }

    private void OnEntityWrappedMessage(EntityWrappedMessage msg)
    {
        var entity = entities.Find(e => e.Id == msg.Id);
        
        if (entity == null)
        {
            Log.Warn($"Received entity wrapped message for an unknown entity: &1{msg.Id}&r");
            return;
        }
        
        try
        {
            entity.OnEntityWrappedMessage(msg);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to handle entity wrapped message:\n{ex}");
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
        
        Log.DebugIf($"Received entity destroy message for entity &1{entity.Id}&r", DebugLogs);

        try
        {
            if (!entity.IsDestroyed)
            {
                entity.IsDestroyed = true;
                entity.OnDestroyed();
            }
            
            entities.Remove(entity);
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

        if (entity.IsConfirmed)
        {
            Log.Warn($"Received duplicate spawn confirmation message for entity &1{entity.Id}&r");
            return;
        }

        try
        {
            entity.OnClientConfirmed();
            entity.IsConfirmed = true;
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
        
        if (!registeredEntities.TryGetFirst(x => string.Equals(x.LocalType, msg.Type), out var info))
        {
            Log.Warn($"Received entity spawn message for unknown type: &1{msg.Type}&r");
            return;
        }
        
        Log.DebugIf($"Found constructor: &1{msg.Type}&r, spawning entity ..", DebugLogs);

        try
        {
            var entity = info.Constructor();
            
            InitEntity(entity, msg.Id);
            
            Send(new ConfirmSpawnMessage(msg.Id));
            
            entity.OnClientSpawned();
            entity.IsConfirmed = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to spawn entity of type &1{msg.Type}&r:\n{ex}");
        }
    }

    private void InitEntity(Entity entity, ushort id)
    {
        entity.Id = id;
        entity.Manager = this;
        
        entities.Add(entity);
    }

    private bool LocalDestroy(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        Log.DebugIf($"Destroying entity &1{entity.Id}&r", DebugLogs);

        entities.Remove(entity);
        
        entity.IsDestroyed = true;

        try
        {
            entity.OnDestroyed();
        }
        catch (Exception ex)
        {
            Log.Error($"Could not destroy entity &1{entity.Id}&r:\n{ex}");
        }
        
        Log.DebugIf($"Entity &1{entity.Id}&r destroyed", DebugLogs);
        return true;
    }
}