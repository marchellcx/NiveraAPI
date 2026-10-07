using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;

using NiveraAPI.Logs;
using NiveraAPI.Utilities;

using NiveraAPI.IO.Network.API;
using NiveraAPI.IO.Network.API.Internal.Tcp;

using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;

namespace NiveraAPI.IO.Network;

/// <summary>
/// Represents a network connection that operates as either a client or server connection,
/// enabling communication via sockets and providing utilities for sending and receiving data.
/// </summary>
public class NetConnection
{
    private volatile bool debugLogs;
    private volatile int id;

    private volatile object msgLock;
    private volatile ByteWriter msgWriter;

    private volatile NetServer? server;
    private volatile NetClient? client;

    private volatile NetPing ping;
    private volatile NetTime time;

    private volatile LogSink log;

    private volatile TcpClient tcpClient;
    
    private volatile TcpServerSendPipe? sendPipe;
    private volatile TcpServerRecvPipe? recvPipe;

    private float netTime = 0f;

    private Queue<ISerializableObject> messages = new();
    private Queue<RetransmittedMessage> retransmissions = new();

    private Dictionary<Type, NetService> servicesByType = new();
    private Dictionary<Type, Action<ISerializableObject>> messageHandlers = new();
    
    /// <summary>
    /// The unique identifier of the connection.
    /// </summary>
    public int Id => id;
    
    /// <summary>
    /// Whether the connection is a server connection.
    /// </summary>
    public bool IsServer => server != null;
    
    /// <summary>
    /// Whether the connection is a client connection.
    /// </summary>
    public bool IsClient => client != null;

    /// <summary>
    /// Represents the total number of bytes sent over the network connection.
    /// This value is sourced from either the associated client or the server's send pipe,
    /// depending on the connection type. Defaults to 0 if neither is available.
    /// </summary>
    public long SentBytes => client?.SentBytes ?? sendPipe?.sentBytes ?? 0;

    /// <summary>
    /// Gets the total number of bytes received by the connection.
    /// For client connections, this value reflects the data received through the underlying client.
    /// For server connections, it reflects the data received through the server's receive pipeline.
    /// </summary>
    public long ReceivedBytes => client?.ReceivedBytes ?? recvPipe?.receivedBytes ?? 0;
    
    /// <summary>
    /// Gets the active ping component.
    /// </summary>
    public NetPing Ping => ping;

    /// <summary>
    /// Gets the active time component.
    /// </summary>
    public NetTime Time => time;

    /// <summary>
    /// Represents the client instance associated with the network connection.
    /// Provides functionality for communication and managing client-specific behaviors.
    /// </summary>
    public NetClient? Client => client;

    /// <summary>
    /// Represents the associated server instance for the network connection, if the connection
    /// is operating as a server. Returns null if the connection is operating as a client.
    /// </summary>
    public NetServer? Server => server;

    /// <summary>
    /// Provides access to the underlying TCP client used for managing the network connection.
    /// This property is primarily used for sending and receiving data over a network socket.
    /// </summary>
    public TcpClient TcpClient => tcpClient;
    
    /// <summary>
    /// Provides access to the underlying TCP server pipe used for sending data to the network.
    /// </summary>
    public TcpServerSendPipe? ServerSendPipe => sendPipe;

    /// <summary>
    /// Provides access to the underlying TCP server pipe used for receiving data from the network.
    /// </summary>
    public TcpServerRecvPipe? ServerReceivePipe => recvPipe;
    
    /// <summary>
    /// The end point of the connection.
    /// </summary>
    public IPEndPoint? EndPoint => tcpClient?.Client?.RemoteEndPoint as IPEndPoint;

    /// <summary>
    /// Gets the logging mechanism associated with the connection.
    /// </summary>
    public LogSink Log => log;

    /// <summary>
    /// The maximum number of retransmissions allowed for a message.
    /// </summary>
    public int MaxRetransmissions => client?.MaxRetransmissions ?? server?.MaxRetransmissions ?? 0;

    /// <summary>
    /// Indicates whether debug logs are enabled for the network connection.
    /// When set to true, additional debug information is logged to facilitate troubleshooting and monitoring of the connection.
    /// </summary>
    public bool DebugLogs
    {
        get => debugLogs;
        set => debugLogs = value;
    }

    /// <summary>
    /// Indicates whether the network connection is currently active and operational.
    /// A connection is considered active if the underlying TCP client is connected
    /// and the instance is associated with either a client or a server.
    /// </summary>
    public bool IsConnected => tcpClient != null && tcpClient.Connected && (client != null || server != null);

    /// <summary>
    /// Whether the connection has any data to be sent.
    /// </summary>
    public bool HasData => msgWriter.Position > 0
                           || ping.ShouldWrite()
                           || time.ShouldWrite();

    /// <summary>
    /// A read-only dictionary that maps service types to their respective instances,
    /// enabling management and access to network services associated with the current connection.
    /// </summary>
    public IReadOnlyDictionary<Type, NetService> Services => servicesByType;

    /// <summary>
    /// Creates a new <see cref="NetConnection"/> instance.
    /// </summary>
    public NetConnection(TcpClient client, int id)
    {
        this.id = id;
        this.tcpClient = client ?? throw new ArgumentNullException(nameof(client));
        
        ping = new();
        time = new(this);
        
        msgLock = new();
        msgWriter = ByteWriter.Get();
    }

    /// <summary>
    /// Creates a new <see cref="NetConnection"/> instance.
    /// </summary>
    /// <param name="server">The server instance associated with the connection.</param>
    /// <param name="client">The client instance associated with the connection.</param>
    /// <param name="id">The unique identifier for the connection.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="server"/> is null.</exception>
    public NetConnection(NetServer server, TcpClient client, int id) : this(client, id)
    {
        this.server = server ?? throw new ArgumentNullException(nameof(server));

        debugLogs = server.debugLogs;
        
        log = LogManager.GetSource("IO", $"NetConnectionServer@{EndPoint?.ToString() ?? "null"}[{id}]");

        sendPipe = new(client, this);
        sendPipe.Start();
        
        recvPipe = new(client, this);
        recvPipe.Start();
    }
    
    /// <summary>
    /// Creates a new <see cref="NetConnection"/> instance.
    /// </summary>
    /// <param name="client">The client instance associated with the connection.</param>
    /// <param name="tcpClient">The socket used for communication.</param>
    /// <param name="id">The unique identifier for the connection.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> or <paramref name="tcpClient"/> is null.</exception>
    public NetConnection(NetClient client, TcpClient tcpClient, int id) : this(tcpClient, id)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));

        debugLogs = client.debugLogs;
        
        log = LogManager.GetSource("IO", $"NetConnectionClient@{EndPoint?.ToString() ?? "null"}");
    }

    /// <summary>
    /// Initializes and starts the connection by activating and registering services associated with the client or server.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if neither a client nor a server is set for the connection before calling this method.
    /// </exception>
    public void Start()
    {
        var services = default(ConcurrentBag<Type>);

        if (client != null)
            services = client.ProvidedServices;
        else if (server != null)
            services = server.ProvidedServices;
        else
            throw new InvalidOperationException($"The connection MUST have a server or client set before being started!");

        foreach (var serviceType in services)
        {
            if (serviceType.IsSubclassOf(typeof(NetService)))
            {
                Log.DebugIf($"Attempting to add service &3{serviceType}&r ..", debugLogs);
                
                if (Activator.CreateInstance(serviceType) is not NetService netService)
                {
                    Log.Error($"Failed to create service of type &3{serviceType}&r");
                    continue;
                }

                servicesByType[serviceType] = netService;

                netService.Connection = this;
                netService.Start();
                
                Log.Info($"Added service &3{serviceType}&r!");
            }
            else
            {
                Log.Error($"Service type &3{serviceType}&r is not a subclass of &3NetService&r");
            }
        }

        ping.Start();
        time.Start();
        
        log.DebugIf("Started!", debugLogs);
    }

    /// <summary>
    /// Stops the current <see cref="NetConnection"/> instance and cleans up resources.
    /// </summary>
    /// <remarks>
    /// This method halts all associated services, clears internal collections,
    /// and stops internal components such as <see cref="NetPing"/> and <see cref="NetTime"/>.
    /// It also releases the <see cref="ByteWriter"/> back to the object pool.
    /// </remarks>
    public void Stop()
    {
        foreach (var kvp in servicesByType)
        {
            try
            {
                kvp.Value.Stop();
                kvp.Value.Connection = null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to stop service &3{kvp.Key.Name}&r!\n{ex}");
            }
        }

        servicesByType.Clear();
        messageHandlers.Clear();
        
        ping.Stop();
        time.Stop();
        
        msgWriter?.ReturnToPool();
        msgWriter = null!;
        
        log.DebugIf("Stopped!", debugLogs);
    }

    /// <summary>
    /// Adds a network service to the current <see cref="NetConnection"/> instance.
    /// </summary>
    /// <param name="service">The <see cref="NetService"/> to be added to the connection.</param>
    /// <returns>
    /// A boolean value indicating whether the service was successfully added.
    /// Returns <c>true</c> if the service was added successfully; otherwise, <c>false</c>.
    /// </returns>
    public bool AddService(NetService service)
    {
        Exceptions.NullArgument(nameof(service), service);

        var type = service.GetType();

        if (servicesByType.ContainsKey(type))
            return false;
        
        servicesByType[type] = service;
        
        service.Connection = this;
        service.Start();
        
        return true;
    }

    /// <summary>
    /// Removes the specified <see cref="NetService"/> from the connection.
    /// </summary>
    /// <param name="service">The <see cref="NetService"/> instance to remove.</param>
    /// <returns>
    /// <c>true</c> if the service was successfully removed; otherwise, <c>false</c>,
    /// indicating that the service was not found in the collection.
    /// </returns>
    public bool RemoveService(NetService service)
    {
        Exceptions.NullArgument(nameof(service), service);
        
        var type = service.GetType();

        if (!servicesByType.Remove(type))
            return false;
        
        service.Stop();
        service.Connection = null;
        
        return true;
    }

    /// <summary>
    /// Registers a handler for processing messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of message to handle, which must implement <see cref="ISerializableObject"/>.</typeparam>
    /// <param name="handler">The action to execute when a message of type <typeparamref name="T"/> is received.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler"/> is null.</exception>
    public void RegisterHandler<T>(Action<T> handler) where T : ISerializableObject
    {
        Exceptions.NullArgument(nameof(handler), handler);

        messageHandlers[typeof(T)] = obj => handler((T)obj);
        
        log.DebugIf($"Registered handler for message &3{typeof(T).Name}&r", debugLogs);
    }

    /// <summary>
    /// Removes the handler associated with the specified serializable object type.
    /// </summary>
    /// <typeparam name="T">The type of the serializable object for which the handler should be removed.</typeparam>
    public void RemoveHandler<T>() where T : ISerializableObject
    {
        if (messageHandlers.Remove(typeof(T)))
            log.DebugIf($"Removed handler for message &3{typeof(T).Name}&r", debugLogs);
        else
            log.DebugIf($"No handler for message &3{typeof(T).Name}&r", debugLogs);
    }

    /// <summary>
    /// Disconnects the current network connection. If the connection is a client, it initiates
    /// a disconnection using the client-specific implementation. If the connection is a server,
    /// it disconnects using the server-specific implementation for the current connection instance.
    /// </summary>
    public void Disconnect()
    {
        log.DebugIf("Disconnecting ...", debugLogs);
        
        if (IsClient)
            client!.Disconnect();

        if (IsServer)
            server!.Disconnect(this);
    }

    /// <summary>
    /// Sends the specified serializable object to the connected endpoint.
    /// </summary>
    /// <param name="obj">The object implementing <see cref="ISerializableObject"/> to be sent.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer associated with the <paramref name="obj"/> is null,
    /// or when the serializer has not been registered with a valid index.
    /// </exception>
    public void Send(ISerializableObject obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (obj.Serializer == null)
            throw new InvalidOperationException("Message does not have a serializer associated with it.");
        
        var index = obj.Serializer.GetIndex();
        
        if (index == 0)
            throw new InvalidOperationException("Message serializer has not been registered.");

        lock (msgLock)
        {
            var position = msgWriter.Position;

            try
            {
                msgWriter.WriteUInt16(index);

                obj.Serializer.Serialize(obj, msgWriter);

                if (msgWriter.Position > ushort.MaxValue)
                {
                    msgWriter.Position = position;
                    
                    messages.Enqueue(obj);
                }
            }
            catch (Exception ex)
            {
                msgWriter.Position = position;
                
                log.Error($"Failed to serialize message, rolling back!\n{ex}");
            }

            if (msgWriter.Position < ushort.MaxValue)
            {
                while (messages.Count > 0)
                {
                    position = msgWriter.Position;
                    
                    try
                    {
                        var msg = messages.Dequeue();
                    
                        msgWriter.WriteUInt16(index);

                        msg.Serializer.Serialize(msg, msgWriter);

                        if (msgWriter.Position > ushort.MaxValue)
                        {
                            msgWriter.Position = position;
                        
                            messages.Enqueue(msg);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        msgWriter.Position = position;
                    
                        log.Error($"Failed to serialize queued message, rolling back!\n{ex}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates the state of the <see cref="NetConnection"/> instance.
    /// </summary>
    /// <remarks>
    /// This method calculates the time delta and updates all the services associated with the connection.
    /// It ensures each service is valid and running before invoking their update logic. If an exception
    /// occurs while updating a service, the error is logged without interrupting the update process for
    /// other services.
    /// </remarks>
    /// <exception cref="Exception">
    /// Logged if an error occurs during the update process of any service.
    /// </exception>
    public void Update()
    {
        var netDelta = 0f;
        var curTime = time.Time;
        
        if (netTime > 0f)
            netDelta = curTime - netTime;
        
        netTime = curTime;

        var count = retransmissions.Count;
        
        for (var x = 0; x < count; x++)
        {
            if (retransmissions.Count < 1)
                break;
            
            var msg = retransmissions.Dequeue();

            if (!Handle(msg.Message))
            {
                if (msg.Count + 1 < MaxRetransmissions)
                {
                    retransmissions.Enqueue(new(msg.Count + 1, msg.Message));
                }
                else
                {
                    log.Warn($"Max retransmissions reached for message &1{msg.Message.GetType().Name}&r");
                }
            }
        }
            
        foreach (var kvp in servicesByType)
        {
            try
            {
                kvp.Value.Update(netDelta, LibraryUpdate.DeltaTime);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to update service &3{kvp.Key.Name}&r:\n{ex}");
            }
        }
    }

    /// <summary>
    /// Attempts to write the current state of the <see cref="NetConnection"/> instance to a <see cref="ByteWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// When this method returns, contains the <see cref="ByteWriter"/> instance with the current state written into it,
    /// or null if no data was written.
    /// </param>
    /// <returns>
    /// <c>true</c> if any data was written to the <see cref="ByteWriter"/>; otherwise, <c>false</c>.
    /// </returns>
    public bool TryWrite(ByteWriter writer)
    {
        var writeMsg = msgWriter.Position > 0;
        var writePing = ping.ShouldWrite();
        var writeTime = time.ShouldWrite();

        if (!writeTime && !writePing && !writeMsg)
            return false;

        if (writeTime)
            time.Write(writer);

        if (writePing)
            ping.Write(writer);

        if (writeMsg)
        {
            lock (msgLock)
            {
                writer.WriteByte((byte)NetHeader.Message);
                writer.WriteUInt16((ushort)msgWriter.Position);

                for (var x = 0; x < msgWriter.Position; x++)
                    writer.WriteByte(msgWriter.Buffer[x]);

                msgWriter.Reset();
            }
        }

        return true;
    }

    internal void Receive(ByteReader reader)
    {
        try
        {
            while (reader.Remaining > 2)
            {
                if (!TryReadPacket(reader))
                {
                    log.Warn($"Discarding invalid packet: &1{reader.Remaining} bytes&r");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed while reading packet:\n{ex}");
        }
    }

    private bool Handle(ISerializableObject obj)
    {
        var type = obj.GetType();

        try
        {
            if (messageHandlers.TryGetValue(type, out var handler))
            {
                handler(obj);
                return true;
            }

            foreach (var kvp in servicesByType)
            {
                if (kvp.Value.Receive(obj))
                {
                    return true;
                }
            }

            log.Warn($"No handler for message of type &1{type.Name}&r");
            return false;
        }
        catch (Exception ex)
        {
            log.Error($"Could not handle message of type &1{type.Name}&r:\n{ex}");
            return true;
        }
    }

    private bool TryReadPacket(ByteReader reader)
    {
        var headerByte = reader.ReadByte();

        if (!Enum.IsDefined(typeof(NetHeader), headerByte))
        {
            log.Warn($"Received invalid header: &1{headerByte}&r");
            return false;
        }

        var header = (NetHeader)headerByte;

        switch (header)
        {
            case NetHeader.Ping: return TryReadPing(reader);
            case NetHeader.Time: return TryReadTime(reader);
            case NetHeader.Message: return TryReadMessage(reader);
            
            default: 
                log.Error($"Received unknown header: &1{header}&r");
                return false;
        }
    }

    private bool TryReadPing(ByteReader reader)
    {
        ping.Read(reader);
        ping.RestartWatch();
        
        return true;
    }

    private bool TryReadTime(ByteReader reader)
    {
        time.Read(reader);
        return true;
    }

    private bool TryReadMessage(ByteReader reader)
    {
        var count = reader.ReadUInt16();
        var position = reader.Position + count;
        
        log.DebugIf($"Attempting to read messages (Count={count}; Position={position}; CurPosition={reader.Position}) ...", debugLogs);

        while (reader.Position < position && reader.Remaining > 2)
        {
            try
            {
                var index = reader.ReadUInt16();
                var serializer = ObjectSerializer.GetSerializer(index);

                if (serializer == null)
                {
                    log.Warn($"Received message with unknown serializer: &1{index}&r");
                    return false;
                }

                var message = serializer.Construct();

                if (message == null)
                {
                    log.Warn($"Failed to construct message with serializer: &1{index}&r");
                    return false;
                }

                serializer.Deserialize(message, reader);

                if (!Handle(message) && MaxRetransmissions > 0)
                    retransmissions.Enqueue(new(0, message));
            }
            catch (Exception ex)
            {
                log.Error($"Failed to deserialize message:\n{ex}");
                return false;
            }
        }

        return true;
    }
}