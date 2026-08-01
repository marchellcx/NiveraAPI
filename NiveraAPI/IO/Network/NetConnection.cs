using System.Net;
using System.Net.Sockets;

using NiveraAPI.Logs;

using NiveraAPI.IO.Network.API;
using NiveraAPI.IO.Network.API.Internal.Tcp;
using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;

using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.IO.Network;

/// <summary>
/// Represents a network connection that operates as either a client or server connection,
/// enabling communication via sockets and providing utilities for sending and receiving data.
/// </summary>
public class NetConnection : ServiceCollection
{
    internal volatile bool debugLogs;
    internal volatile int id;

    private volatile object msgLock = new();
    private volatile ByteWriter msgWriter = ByteWriter.Get();
    
    internal volatile Socket? socket;
    internal volatile EndPoint endPoint;
    
    internal volatile IPEndPoint clientEndPoint;
    internal volatile IPEndPoint serverSendEndPoint;

    internal volatile NetServer? server;
    internal volatile NetClient? client;

    internal volatile NetPing ping;
    internal volatile NetTime time;

    internal volatile LogSink log;

    internal volatile TcpClient tcpClient;
    internal volatile TcpServerSendPipe tcpSendPipe;
    internal volatile TcpServerRecvPipe tcpRecvPipe;

    private float netTime = 0f;

    private Queue<ISerializableObject> messages = new();
    private Queue<RetransmittedMessage> retransmissions = new();
    
    private List<NetService> netServices = new();
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
    /// The socket associated with the connection.
    /// </summary>
    /// <remarks>Will be <c>null</c> if the connection is a server connection.</remarks>
    public Socket? Socket => socket;
    
    /// <summary>
    /// The end point of the connection.
    /// </summary>
    public IPEndPoint EndPoint => clientEndPoint ?? (IPEndPoint)endPoint;

    /// <summary>
    /// Gets the logging mechanism associated with the connection.
    /// </summary>
    public LogSink Log => log;

    /// <summary>
    /// The maximum number of retransmissions allowed for a message.
    /// </summary>
    public int MaxRetransmissions => client?.MaxRetransmissions ?? server?.MaxRetransmissions ?? 0;

    /// <summary>
    /// Whether the connection has any data to be sent.
    /// </summary>
    public bool HasData => msgWriter.Position > 0
                           || ping.ShouldWrite()
                           || time.ShouldWrite();

    /// <summary>
    /// Creates a new <see cref="NetConnection"/> instance.
    /// </summary>
    /// <param name="server">The server instance associated with the connection.</param>
    /// <param name="endPoint">The end point of the connection.</param>
    /// <param name="id">The unique identifier for the connection.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="server"/> or <paramref name="endPoint"/> is null.</exception>
    public NetConnection(NetServer server, EndPoint endPoint, int id)
    {
        this.id = id;
        this.server = server ?? throw new ArgumentNullException(nameof(server));
        this.endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        
        var ip = (IPEndPoint)endPoint;
        var address = new IPAddress(ip.Address.GetAddressBytes());

        debugLogs = server.debugLogs;
        serverSendEndPoint = new IPEndPoint(address, ip.Port);

        ping = new();
        time = new(this);

        log = LogManager.GetSource("IO", $"NetConnectionServer@{endPoint}[{id}]");
    }
    
    /// <summary>
    /// Creates a new <see cref="NetConnection"/> instance.
    /// </summary>
    /// <param name="client">The client instance associated with the connection.</param>
    /// <param name="socket">The socket used for communication.</param>
    /// <param name="endPoint">The end point of the connection.</param>
    /// <param name="id">The unique identifier for the connection.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> or <paramref name="socket"/> is null.</exception>
    public NetConnection(NetClient client, Socket socket, IPEndPoint endPoint, int id)
    {
        this.id = id;
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        this.clientEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));

        debugLogs = client.debugLogs;
        
        ping = new();
        time = new(this);
        
        log = LogManager.GetSource("IO", $"NetConnectionClient@{endPoint}");
    }
    
    /// <summary>
    /// Creates a new <see cref="NetConnection"/> instance.
    /// </summary>
    /// <param name="client">The client instance associated with the connection.</param>
    /// <param name="tcpClient">The socket used for communication.</param>
    /// <param name="endPoint">The end point of the connection.</param>
    /// <param name="id">The unique identifier for the connection.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> or <paramref name="tcpClient"/> is null.</exception>
    public NetConnection(NetClient client, IPEndPoint endPoint, TcpClient tcpClient, int id)
    {
        this.id = id;
        this.tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.clientEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));

        debugLogs = client.debugLogs;
        
        ping = new();
        time = new(this);
        
        log = LogManager.GetSource("IO", $"NetConnectionClient@{endPoint}");
    }

    /// <inheritdoc />
    public override void Start()
    {
        base.Start();
        
        ping.Start();
        time.Start();
        
        log.DebugIf("Started!", debugLogs);
    }

    /// <inheritdoc />
    public override void Stop()
    {
        base.Stop();
        
        StopAllServices();
        
        netServices.Clear();
        messageHandlers.Clear();
        
        ping.Stop();
        time.Stop();
        
        if (msgWriter != null)
            msgWriter.ReturnToPool();

        msgWriter = null!;
        
        log.DebugIf("Stopped!", debugLogs);
    }

    /// <inheritdoc />
    public override void OnServiceAdded(IService service)
    {
        base.OnServiceAdded(service);

        if (service is NetService netService)
        {
            netService.Connection = this;
            
            netServices.Add(netService);
            
            log.DebugIf($"Added network service &3{service.GetType().Name}&r", debugLogs);
        }
        else
        {
            log.DebugIf($"Added service &3{service.GetType().Name}&r", debugLogs);
        }
    }

    /// <inheritdoc />
    public override void OnServiceRemoved(IService service)
    {
        base.OnServiceRemoved(service);

        if (service is NetService netService)
        {
            netService.Connection = null!;
            
            netServices.Remove(netService);
        }
    }

    /// <summary>
    /// Registers a handler for processing messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of message to handle, which must implement <see cref="ISerializableObject"/>.</typeparam>
    /// <param name="handler">The action to execute when a message of type <typeparamref name="T"/> is received.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler"/> is null.</exception>
    public void RegisterHandler<T>(Action<T> handler) where T : ISerializableObject
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        
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
            client.Disconnect();

        if (IsServer)
            server.Disconnect(this);
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
                    
                    var msg = messages.Dequeue();
                    
                    msgWriter.WriteUInt16(index);

                    msg.Serializer.Serialize(obj, msgWriter);

                    if (msgWriter.Position > ushort.MaxValue)
                    {
                        msgWriter.Position = position;
                        
                        messages.Enqueue(msg);
                        break;
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
            
        for (var x = 0; x < netServices.Count; x++)
        {
            var service = netServices[x];
            
            if (!service.IsValid || !service.IsRunning)
                continue;

            try
            {
                service.Update(netDelta, LibraryUpdate.DeltaTime);
            }
            catch (Exception ex)
            {
                log.Error($"Failed to update service {service.GetType().Name}:\n{ex}");
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

            for (var x = 0; x < netServices.Count; x++)
            {
                var service = netServices[x];

                if (!service.IsValid || !service.IsRunning)
                    continue;

                if (service.Receive(obj))
                    return true;
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
        ping.RestartWatch();
        
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