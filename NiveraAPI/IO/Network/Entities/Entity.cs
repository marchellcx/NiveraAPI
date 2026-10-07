using NiveraAPI.IO.Network.Entities.Messages;
using NiveraAPI.IO.Network.Entities.Synchronization;

using NiveraAPI.IO.Serialization;

using NiveraAPI.Logs;
using NiveraAPI.Pooling;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// A network entity.
/// </summary>
public class Entity
{
    private Dictionary<ushort, Action<ByteReader>> handlers = new();
    
    /// <summary>
    /// Gets the event that is invoked when the entity is spawned.
    /// </summary>
    public event Action? Spawned;

    /// <summary>
    /// Gets the event that is invoked when the entity is destroyed.
    /// </summary>
    public event Action? Destroyed;

    /// <summary>
    /// Gets the event that is invoked when the client confirms the entity spawn.
    /// </summary>
    public event Action? Confirmed;

    /// <summary>
    /// Gets the event that is invoked whenever the entity's state is updated.
    /// </summary>
    public event Action? Updated;

    /// <summary>
    /// Gets the entity's ID.
    /// </summary>
    public ushort Id { get; internal set; }

    /// <summary>
    /// Whether or not the server has received a spawn confirmation message from the client - or - if the client has sent a spawn confirmation message.
    /// </summary>
    public bool IsConfirmed { get; internal set; }

    /// <summary>
    /// Whether the entity is destroyed.
    /// </summary>
    public bool IsDestroyed
    {
        get => field || Manager == null;
        internal set => field = value;
    }

    /// <summary>
    /// Gets the logging sink associated with the network entity.
    /// </summary>
    public LogSink Log
    {
        get
        {
            if (field == null)
                field = LogManager.GetSource($"Entities@{Connection?.EndPoint?.ToString() ?? "null"}", GetType().Name);
            
            return field;
        }
    }
    
    public SyncParent SyncParent { get; private set; }

    /// <summary>
    /// Gets the parent entity manager.
    /// </summary>
    public EntityManager? Manager { get; internal set; }

    /// <summary>
    /// Gets the network connection associated with the entity's manager, if available.
    /// </summary>
    public NetConnection? Connection => Manager?.Connection;

    /// <summary>
    /// Gets the local time for the entity, derived from the associated <see cref="EntityManager"/>.
    /// </summary>
    public float LocalTime => Manager?.LocalTime ?? 0f;

    /// <summary>
    /// Gets the network time in seconds.
    /// </summary>
    public float NetworkTime => Manager?.NetworkTime ?? 0f;

    /// <summary>
    /// Gets the current network tick count. Represents the number of server ticks since the server started.
    /// </summary>
    public long NetworkTick => Manager?.TickCount ?? 0;

    /// <summary>
    /// Gets called when the entity is destroyed.
    /// </summary>
    public virtual void OnDestroyed()
    {
        handlers.Clear();
        
        SyncParent?.Destroy();
        
        Destroyed?.Invoke();
    }

    /// <summary>
    /// Gets called when the entity is spawned on the server.
    /// </summary>
    public virtual void OnServerSpawned()
    {
        CommonSetup();
        
        Spawned?.Invoke();
    }

    /// <summary>
    /// Gets called when the entity is spawned on the client.
    /// </summary>
    public virtual void OnClientSpawned()
    {
        CommonSetup();
        
        Spawned?.Invoke();
    }

    /// <summary>
    /// Gets called when the client confirms the entity spawn.
    /// </summary>
    public virtual void OnClientConfirmed()
    {
        Confirmed?.Invoke();
    }

    /// <summary>
    /// Gets called periodically to update the entity's state.
    /// </summary>
    public virtual void OnUpdate()
    {
        SyncParent?.Update();
        
        Updated?.Invoke();
    }

    /// <summary>
    /// Attempts to destroy the entity by marking it as destroyed, removing it from the managing entity list,
    /// and notifying other systems through the manager.
    /// </summary>
    /// <returns>
    /// True if the entity was successfully destroyed; otherwise, false if destruction failed or the entity
    /// was already destroyed.
    /// </returns>
    public bool Destroy()
    {
        if (IsDestroyed)
            return false;

        return Manager!.DestroyEntity(this);
    }

    /// <summary>
    /// Registers a handler for processing incoming messages with the specified message ID and deserializing the message data into the specified type.
    /// </summary>
    /// <typeparam name="TValue">The type to which the incoming message data will be deserialized.</typeparam>
    /// <param name="msgId">The unique identifier of the message type to be handled.</param>
    /// <param name="handler">The action to be invoked when a message with the specified ID is received. The action takes a parameter of type <typeparamref name="TValue"/> representing the deserialized message data.</param>
    public void Receive<TValue>(ushort msgId, Action<TValue?> handler)
    {
        Receive(msgId, reader => handler(reader.Read<TValue>()));
    }

    /// <summary>
    /// Registers a handler for processing incoming messages with the specified message ID.
    /// </summary>
    /// <param name="msgId">The unique identifier of the message type to be handled.</param>
    /// <param name="handler">The action to be invoked when a message with the specified ID is received. The action takes a <see cref="ByteReader"/> parameter for reading the message data.</param>
    public void Receive(ushort msgId, Action<ByteReader> handler)
    {
        Exceptions.NullArgument(nameof(handler), handler);
        
        handlers[msgId] = handler;
    }

    /// <summary>
    /// Sends a message with the specified identifier and value.
    /// </summary>
    /// <typeparam name="TArg">
    /// The type of the value to send with the message. This type must be compatible with the serialization system.
    /// </typeparam>
    /// <param name="id">
    /// The identifier of the message to be sent.
    /// </param>
    /// <param name="value">
    /// The value to be serialized and sent with the message.
    /// </param>
    public void Send<TArg>(ushort id, TArg value)
    {
        Send(id, writer => writer.Write(value));
    }

    /// <summary>
    /// Sends a message with the specified identifier and writes its data using the provided <see cref="ByteWriter"/> instance.
    /// </summary>
    /// <param name="id">
    /// The identifier of the message to be sent.
    /// </param>
    /// <param name="dataWriter">
    /// An action that writes the data of the message using the provided <see cref="ByteWriter"/>.
    /// </param>
    public void Send(ushort id, Action<ByteWriter> dataWriter)
    {
        Exceptions.NullArgument(nameof(dataWriter), dataWriter);

        var writer = ByteWriter.Get();

        try
        {
            dataWriter(writer);

            Send(id, writer);
        }
        catch (Exception ex)
        {
            Log.Error($"Caught exception while sending message with ID &3{id}&r:\n{ex}");
        }

        writer.ReturnToPool();
    }

    /// <summary>
    /// Sends a message with the specified identifier and data to the associated network connection.
    /// </summary>
    /// <param name="id">The unique identifier for the message being sent.</param>
    /// <param name="data">The data payload to include in the message, represented as a <see cref="ByteWriter"/>.</param>
    public void Send(ushort id, ByteWriter data)
    {
        Exceptions.NullArgument(nameof(data), data);
        
        SendWrapped(EntityHeader.Message, writer =>
        {
            writer.WriteUInt16(id);
            writer.WriteWriter(data);
        });
    }

    /// <summary>
    /// Sends a wrapped message using the specified header and a data writer action that writes the message contents.
    /// </summary>
    /// <param name="header">The header that describes the type of the wrapped message.</param>
    /// <param name="dataWriter">An action that writes the data of the message to the provided ByteWriter.</param>
    public void SendWrapped(EntityHeader header, Action<ByteWriter> dataWriter)
    {
        Exceptions.NullArgument(nameof(dataWriter), dataWriter);

        var writer = ByteWriter.Get();

        try
        {
            dataWriter(writer);

            SendWrapped(header, writer);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to send wrapped message:\n{ex}");
        }

        writer.ReturnToPool();
    }

    /// <summary>
    /// Sends a wrapped message containing the entity's ID and additional serialized data to the associated connection.
    /// </summary>
    /// <param name="header">The header type specifying the purpose of the message.</param>
    /// <param name="writer">The <see cref="ByteWriter"/> instance containing the serialized data to be sent.</param>
    public void SendWrapped(EntityHeader header, ByteWriter writer)
    {
        Exceptions.NullArgument(nameof(writer), writer);

        var position = writer.Position;

        writer.Position = 0;
        
        writer.WriteByte((byte)header);
        writer.WriteUInt16(Id);

        writer.Position += position;
        
        Connection?.Send(new EntityWrappedMessage(Id, writer.ToArray()));
    }

    internal void OnEntityWrappedMessage(EntityWrappedMessage msg)
    {
        using (var reader = ObjectPool<ByteReader>.Shared.Rent())
        {
            reader.Buffer = msg.Data;
            reader.Count = msg.Data.Length;
            reader.Position = 0;
            
            var headerByte = reader.ReadByte();

            if (!Enum.IsDefined(typeof(EntityHeader), headerByte))
            {
                Log.Warn($"Received invalid entity header byte: {headerByte}");
                return;
            }

            try
            {
                var header = (EntityHeader)headerByte;

                if (header is EntityHeader.Message)
                {
                    var msgId = reader.ReadUInt16();

                    if (!handlers.TryGetValue(msgId, out var messageHandler))
                    {
                        Log.Warn($"Received unknown message ID: &3{msgId}&r");
                        return;
                    }

                    messageHandler(reader);
                }
                else if (header is EntityHeader.Method)
                {
                    SyncParent.OnMethodMessage(reader);
                }
                else if (header is EntityHeader.Response)
                {
                    SyncParent.OnResponseMessage(reader);
                }
                else if (header is EntityHeader.SyncVar)
                {
                    SyncParent.OnSyncVarMessage(reader);
                }
                else if (header is EntityHeader.SyncParentMessage)
                {
                    SyncParent.OnSyncParentMessage(reader);
                }
                else if (header is EntityHeader.SyncObjectPayload)
                {
                    SyncParent.OnSyncObjectPayload(reader);
                }
                else
                {
                    Log.Warn($"Received unknown header: &3{header}&r");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while handling message &3{headerByte}&r:\n{ex}");
            }
        }
    }

    private void CommonSetup()
    {
        SyncParent = new(this, false);
    }
}