using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Logs;
using NiveraAPI.Networking.Interfaces;
using NiveraAPI.Pooling;
using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;
using Telepathy;

namespace NiveraAPI.Networking;

/// <summary>
/// Represents a network peer, which can function as either a client or a server in the networking hierarchy.
/// </summary>
/// <remarks>
/// A <see cref="Peer"/> instance is responsible for managing networking capabilities and providing access to networking services.
/// </remarks>
public class Peer : ServiceCollection
{
    private bool valid = false;
    private long timeOffset = 0;
    private float lastNetworkTime = 0f;

    private List<Type> providedServices;
    private List<INetworkService> networkServices = new(16);
    
    private Dictionary<Type, Action<ISerializableObject>> handlers = new(16);
    
    private Stopwatch timer;
    private Stopwatch updateTimer;
    private Stopwatch networkTimer;

    private object payloadLock = new();

    /// <summary>
    /// Gets the interval at which the peer sends a ping packet to the server.
    /// </summary>
    public virtual int PingInterval { get; }

    /// <summary>
    /// Gets the unique identifier of the peer.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the latency of the peer in milliseconds.
    /// </summary>
    public float Latency { get; private set; }

    /// <summary>
    /// Gets the type of the peer.
    /// </summary>
    public PeerType Type { get; private set; }

    /// <summary>
    /// Gets the client associated with this peer, if created by a client.
    /// </summary>
    public INetworkClient? Client { get; private set; }

    /// <summary>
    /// Gets the server associated with this peer, if created by a server.
    /// </summary>
    public INetworkServer? Server { get; private set; }

    /// <summary>
    /// Gets the local endpoint of the peer.
    /// </summary>
    public virtual IPEndPoint? LocalEndPoint
    {
        get
        {
            if (Client != null)
                return Client.LocalEndPoint;
            
            if (Server != null)
                return Server.LocalEndPoint;
            
            return null;
        }
    }

    /// <summary>
    /// Gets the remote endpoint of the peer.
    /// </summary>
    public virtual IPEndPoint? RemoteEndPoint
    {
        get
        {
            if (Client != null)
                return Client.RemoteEndPoint;

            if (Server != null)
                return Server.GetEndPoint(this);

            return null;
        }
    }
    
    /// <summary>
    /// Gets the writer of the peer.
    /// </summary>
    public ByteWriter Writer { get; private set; } = PoolBase<ByteWriter>.Shared.Rent();
    
    /// <summary>
    /// Gets the writer for payloads.
    /// </summary>
    public ByteWriter PayloadWriter { get; private set; } = PoolBase<ByteWriter>.Shared.Rent();
    
    /// <summary>
    /// Gets the reader of the peer.
    /// </summary>
    public ByteReader Reader { get; private set; } = PoolBase<ByteReader>.Shared.Rent();

    /// <summary>
    /// Gets the log sink associated with the peer.
    /// </summary>
    public LogSink Log { get; private set; }
    
    /// <summary>
    /// Whether the peer was created by a server.
    /// </summary>
    public bool IsServer => Type == PeerType.Peer;
    
    /// <summary>
    /// Whether the peer was created by a client.
    /// </summary>
    public bool IsClient => Type == PeerType.Local;
    
    /// <summary>
    /// Whether time synchronization has been completed.
    /// </summary>
    public bool IsTimeSynced => networkTimer.IsRunning;

    /// <summary>
    /// Whether the service is valid.
    /// </summary>
    public override bool IsValid => valid;

    /// <summary>
    /// Gets a value indicating whether the peer is currently connected.
    /// </summary>
    public virtual bool IsConnected
    {
        get
        {
            if (Client != null)
                return Client.IsConnected;

            if (Server != null)
                return Server.IsConnected(this);

            return false;
        }
    }

    /// <summary>
    /// Gets the current network time tracked by the peer, measured in ticks.
    /// </summary>
    /// <remarks>
    /// If the peer is acting as a server, the network time corresponds to the elapsed ticks of the internal network timer.
    /// If the peer is acting as a client and a time offset is applied, the elapsed ticks are adjusted by the offset.
    /// Returns 0 if the peer is neither a server nor a client or if no valid time calculation can be performed.
    /// </remarks>
    public long TimeTicks
    {
        get
        {
            if (IsServer && networkTimer.IsRunning)
                return networkTimer.ElapsedTicks;

            if (IsClient && networkTimer.IsRunning)
            {
                var offset = Volatile.Read(ref timeOffset);
                
                if (offset != 0)
                    return networkTimer.ElapsedTicks + offset;
            }
            
            return 0;
        }
    }
    
    /// <summary>
    /// Gets the current network time in seconds, derived from the internal time representation.
    /// </summary>
    public float TimeSeconds => (float)TimeTicks / TimeSpan.TicksPerSecond;

    /// <summary>
    /// Gets the current network time in milliseconds, derived from the internal time representation.
    /// </summary>
    public float TimeMilliseconds => (float)TimeTicks / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// Gets a list of all network services associated with the peer.
    /// </summary>
    public IReadOnlyList<Type> ProvidedServices => providedServices;
    
    /// <summary>
    /// Gets a list of all active network services.
    /// </summary>
    public IReadOnlyList<INetworkService> NetworkServices => networkServices;
    
    /// <summary>
    /// Gets a dictionary of custom handlers for payload types.
    /// </summary>
    public IReadOnlyDictionary<Type, Action<ISerializableObject>> Handlers => handlers;
    
    /// <summary>
    /// Creates a new instance of <see cref="Peer"/> representing a client-side peer.
    /// </summary>
    /// <param name="id">The unique identifier of the peer.</param>
    /// <param name="pingInterval">The interval at which the peer sends a ping packet to the server.</param>   
    /// <param name="client">The <see cref="INetworkClient"/> instance to associate with the peer.</param>
    /// <param name="services">Optional array of service types provided by the peer.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/> is null.</exception>
    public Peer(int id, int pingInterval, INetworkClient client, Type[]? services = null)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        Id = id;
        Client = client;
        PingInterval = pingInterval;

        Type = PeerType.Local;
        
        valid = true;
        
        timer = Stopwatch.StartNew();
        
        updateTimer = new();
        networkTimer = new();

        providedServices = new();
        providedServices.AddRange(services ?? Array.Empty<Type>());
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="Peer"/> representing a server-side peer.
    /// </summary>
    /// <param name="id">The unique identifier of the peer.</param>
    /// <param name="server">The <see cref="INetworkServer"/> instance to associate with the peer.</param>
    /// <param name="services">Optional array of service types provided by the peer.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="server"/> is null.</exception>
    public Peer(int id, INetworkServer server, Type[]? services = null)
    {
        if (server == null)
            throw new ArgumentNullException(nameof(server));
        
        Id = id;
        Server = server;

        Type = PeerType.Peer;
        
        valid = true;
        
        timer = Stopwatch.StartNew();
        
        updateTimer = new();
        networkTimer = new();
        
        providedServices = new();
        providedServices.AddRange(services ?? Array.Empty<Type>());
    }

    /// <summary>
    /// Registers a handler for processing objects of type <typeparamref name="T"/> that implement <see cref="ISerializableObject"/>.
    /// </summary>
    /// <typeparam name="T">The type of the serializable object the handler will process.</typeparam>
    /// <param name="handler">An action to invoke when an object of type <typeparamref name="T"/> is received.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="handler"/> is null.</exception>
    public void SetHandler<T>(Action<T> handler) where T : ISerializableObject
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        handlers[typeof(T)] = payload =>
        {
            try
            {
                handler((T)payload);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not handle payload:\n{ex}");
            }
        };
    }

    /// <summary>
    /// Removes the handler associated with the specified serializable object type.
    /// </summary>
    /// <typeparam name="T">The type of the serializable object whose handler should be removed.</typeparam>
    public void UnsetHandler<T>() where T : ISerializableObject
    {
        handlers.Remove(typeof(T));
    }

    /// <summary>
    /// Sends a payload to be transmitted over the network by adding it to the outgoing queue.
    /// </summary>
    /// <param name="payload">The <see cref="ISerializableObject"/> to be sent.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="payload"/> is null.</exception>
    /// <exception cref="Exception">Thrown if the peer is not in a valid state for sending data.</exception>
    public void Send(ISerializableObject payload)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        if (!valid)
            return;

        if (payload.Serializer == null)
            throw new Exception("Payload serializer is null.");

        lock (payloadLock) // Extra precaution to make sure we don't override.
        {
            try
            {
                var index = payload.Serializer.GetIndex();

                if (index == 0)
                    throw new Exception("Payload serializer index is invalid!");

                PayloadWriter.WriteUInt16(index);

                payload.Serializer.Serialize(payload, PayloadWriter);
            }
            finally
            {
                DisposePayloadConditionally(payload);
            }
        }
    }

    /// <summary>
    /// Disconnects the peer from the network by invoking the appropriate disconnect logic
    /// on the associated network client or network server.
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown when the peer is not connected to either a network client or a network server.
    /// </exception>
    public void Disconnect()
    {
        if (!IsConnected)
            return;
        
        if (Client != null)
        {
            Client.Disconnect();
            return;
        }

        if (Server != null)
        {
            Server.Disconnect(this);
            return;
        }
        
        throw new Exception("Peer is not connected.");
    }
    
    /// <summary>
    /// Starts the service.
    /// </summary>
    public override void Start()
    {
        base.Start();
        
        if (!valid)
            throw new Exception("Peer is not valid.");
        
        if (IsClient)
            Log = LogManager.GetSource("Networking", $"ClientPeer_{Id}@{RemoteEndPoint?.ToString() ?? "(null)"}");
        else
            Log = LogManager.GetSource("Networking", $"ServerPeer_{Id}@{RemoteEndPoint?.ToString() ?? "(null)"}");

        Log.Info($"Peer connected, adding {providedServices.Count} service(s).");
        
        providedServices.ForEach(type => AddService(type, Array.Empty<object>()));
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public override void Stop()
    {
        base.Stop();

        valid = false;
        
        Log.Info("Peer disconnected.");
        
        if (Writer != null)
            PoolBase<ByteWriter>.Shared.Return(Writer);
        
        if (Reader != null)
            PoolBase<ByteReader>.Shared.Return(Reader);
        
        if (PayloadWriter != null)
            PoolBase<ByteWriter>.Shared.Return(PayloadWriter);

        Writer = null!;
        Reader = null!;
        PayloadWriter = null!;
        
        handlers.Clear();
        networkServices.Clear();
        providedServices.Clear();
        
        timer.Stop();
        updateTimer.Stop();
        networkTimer.Stop();
        
        Type = PeerType.Disposed;
    }

    /// <summary>
    /// Called when a service is added to the collection.
    /// </summary>
    /// <param name="service">The service instance that was added.</param>
    public override void OnServiceAdded(IService service)
    {
        base.OnServiceAdded(service);
        
        Log.Info($"Service added: &1{service.GetType().Name}&r");

        if (service is INetworkService networkService)
            networkServices.Add(networkService);
    }

    /// <summary>
    /// Called when a service is removed from the collection.
    /// </summary>
    /// <param name="service">The service instance that was removed.</param>
    public override void OnServiceRemoved(IService service)
    {
        base.OnServiceRemoved(service);
        
        Log.Info($"Service removed: &1{service.GetType().Name}&r");
        
        if (service is INetworkService networkService)
            networkServices.Remove(networkService);
    }
    
    /// <summary>
    /// Handles periodic updates for all associated network services within the <see cref="Peer"/>.
    /// </summary>
    /// <remarks>
    /// This method is responsible for invoking the <see cref="INetworkService.Update(float)"/> method on each
    /// network service associated with the peer, passing the time elapsed since the last update.
    /// It ensures services are updated while the peer remains in a valid and connected state.
    /// </remarks>
    /// <exception cref="Exception">
    /// Logs any exception that occurs during the execution of a service update,
    /// including the service type and the exception details.
    /// </exception>
    public void Update()
    {
        if (!valid)
            return;

        try
        {
            var localDelta = 0f;

            var networkDelta = 0f;
            var networkMs = TimeMilliseconds;

            if (updateTimer is { IsRunning: true })
                localDelta = (float)updateTimer.Elapsed.TotalSeconds;

            if (networkTimer is { IsRunning: true })
            {
                if (lastNetworkTime == 0f)
                    lastNetworkTime = networkMs;

                networkDelta = networkMs - lastNetworkTime;

                lastNetworkTime = networkMs;
            }

            updateTimer.Restart();

            for (var x = 0; x < networkServices.Count; x++)
            {
                var service = networkServices[x];

                if (service == null)
                    continue;

                try
                {
                    service.Update(localDelta, networkDelta);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to update service &1{service.GetType().Name}&r:\n{ex}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to handle peer update:\n{ex}");
        }
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        if (Log != null)
            return Log.Category;

        return "disposed peer";
    }

    /// <summary>
    /// Writes output data from the peer to the associated network layer.
    /// Depending on the state of the peer, this method may perform time synchronization,
    /// send ping packets, or transmit payload data.
    /// </summary>
    /// <remarks>
    /// The method performs the following operations based on the state and configuration:
    /// - If the peer is a server and a time synchronization event is required, it sends a
    /// time sync packet and starts the network timer.
    /// - If the elapsed time exceeds the configured ping interval, it sends a ping packet
    /// to maintain connectivity.
    /// - If there is buffered payload data, it sends the payload to the associated network layer.
    /// </remarks>
    /// <exception cref="Exception">Logs any exceptions that occur during the output process.</exception>
    public void WriteOutput()
    {
        if (!valid)
            return;

        try
        {
            Writer.Reset();
            
            if (!networkTimer.IsRunning) // Send time sync if server first
            {
                if (IsServer)
                {
                    var offset = DateTime.UtcNow.Ticks;

                    Writer.WriteByte(0); // TimeSync Header
                    Writer.WriteInt64(offset);

                    networkTimer.Start();
                }

                return;
            }

            if (PingInterval > 0
                && timer.ElapsedMilliseconds >= PingInterval) // Ping takes priority over payloads
            {
                timer.Restart();

                Writer.WriteByte(1); // Ping Header
                Writer.WriteInt64(DateTime.UtcNow.Ticks);

                return;
            }

            lock (payloadLock)
            {
                if (PayloadWriter.Position < 1)
                    return;
                
                Writer.WriteByte(2); // Payload Header

                for (var x = 0; x < PayloadWriter.Position; x++)
                    Writer.WriteByte(PayloadWriter.Buffer[x]);

                PayloadWriter.Reset();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to write output:\n{ex}");
        }
    }

    /// <summary>
    /// Processes and interprets incoming network data by reading from the associated <see cref="ByteReader"/>.
    /// </summary>
    /// <remarks>
    /// This method handles different types of packets based on their header, including time synchronization,
    /// latency calculation, and custom payloads. If an invalid packet header is encountered, a warning is logged.
    /// </remarks>
    /// <exception cref="Exception">Thrown if an error occurs during input reading or packet processing.</exception>
    public void ReadInput()
    {
        void ReadPing()
        {
            var ticks = Reader.ReadInt64();
            var delta = DateTime.UtcNow.Ticks - ticks;
            var latency = (float)(delta / TimeSpan.TicksPerMillisecond);
            
            Latency = latency;
        }

        void ReadTime()
        {
            if (IsServer)
            {
                Log.Warn("Received time sync on server, ignoring ..");
                return;
            }
            
            var ticks = Reader.ReadInt64();
            var delta = DateTime.UtcNow.Ticks - ticks;
            
            networkTimer.Start();
            
            timeOffset = delta;
            
            Log.Debug($"Received time sync: &1{delta}&r ({TimeMilliseconds} ms)");
        }

        void ReadPayload()
        {
            try
            {
                while (Reader.Remaining > 2)
                {
                    var index = Reader.ReadUInt16();

                    if (index == 0)
                    {
                        Log.Warn("Received invalid payload index!");
                        break;
                    }

                    var serializer = ObjectSerializer.GetSerializer(index);

                    if (serializer == null)
                    {
                        Log.Warn($"Received payload with invalid index: &1{index}&r");
                        break;
                    }

                    var payload = serializer.Construct();

                    if (payload == null)
                    {
                        Log.Warn($"Failed to construct payload with index: &1{index}&r");
                        break;
                    }

                    try
                    {
                        serializer.Deserialize(payload, Reader);

                        // Handlers override NetworkServices
                        if (handlers.TryGetValue(payload.GetType(), out var handler))
                        {
                            handler(payload);
                        }
                        else
                        {
                            var handled = false;

                            for (var x = 0; x < networkServices.Count; x++)
                            {
                                try
                                {
                                    var service = networkServices[x];

                                    if (service is not { IsRunning: true })
                                        continue;

                                    if (service.HandlePayload(payload))
                                    {
                                        handled = true;
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(
                                        $"Service &1{networkServices[x]?.GetType().FullName ?? "null"}&r failed to handle payload:\n{ex}");
                                }
                            }

                            if (!handled)
                                Log.Warn($"No handler for payload type: &1{payload.GetType().Name}&r");
                        }
                    }
                    finally
                    {
                        DisposePayloadConditionally(payload);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle payload:\n{ex}");
            }
        }

        try
        {
            if (!valid)
                return;

            if (Reader.Count < 1)
                return;
            
            var header = Reader.ReadByte();
            
            switch (header)
            {
                case 0:
                    ReadTime();
                    break;

                case 1:
                    ReadPing();
                    break;

                case 2:
                    ReadPayload();
                    break;

                default:
                    Log.Warn($"Invalid packet header: &1{header}&r");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read input:\n{ex}");
        }
        
        Reader.Clear();
    }

    private bool DisposePayloadConditionally(ISerializableObject payload)
    {
        if (payload is IDisposableObject disposableObject && !disposableObject.ShouldDispose())
            return false;

        if (payload.Serializer is not IDisposingSerializer disposingSerializer)
        {
            if (payload is IDisposable disposable)
            {
                disposable.Dispose();
                return true;
            }

            return false;
        }

        disposingSerializer.DisposeObject(payload);
        return true;
    }
}