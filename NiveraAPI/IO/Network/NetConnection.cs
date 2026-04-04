using System.Net;
using System.Net.Sockets;

using NiveraAPI.Logs;

using NiveraAPI.IO.Network.API;

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
    private volatile int id;

    private volatile object msgLock = new();
    private volatile ByteWriter msgWriter = ByteWriter.Get();
    
    private volatile Socket? socket;
    private volatile EndPoint endPoint;
    private volatile IPEndPoint clientEndPoint;
    
    internal volatile IPEndPoint serverSendEndPoint;

    private volatile NetServer? server;
    private volatile NetClient? client;

    private volatile NetPing ping;
    private volatile NetTime time;

    private volatile LogSink log;

    private float netTime = 0f;

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
        
        log.Info("Started!");
    }

    /// <inheritdoc />
    public override void Stop()
    {
        base.Stop();
        
        netServices.Clear();
        messageHandlers.Clear();
        
        ping.Stop();
        time.Stop();
        
        if (msgWriter != null)
            msgWriter.ReturnToPool();

        msgWriter = null!;
        
        log.Info("Stopped!");
    }

    /// <inheritdoc />
    public override void OnServiceAdded(IService service)
    {
        base.OnServiceAdded(service);

        if (service is NetService netService)
        {
            netService.Connection = this;
            netServices.Add(netService);
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
    }

    /// <summary>
    /// Removes the handler associated with the specified serializable object type.
    /// </summary>
    /// <typeparam name="T">The type of the serializable object for which the handler should be removed.</typeparam>
    public void RemoveHandler<T>() where T : ISerializableObject
    {
        messageHandlers.Remove(typeof(T));
    }

    /// <summary>
    /// Disconnects the current network connection. If the connection is a client, it initiates
    /// a disconnection using the client-specific implementation. If the connection is a server,
    /// it disconnects using the server-specific implementation for the current connection instance.
    /// </summary>
    public void Disconnect()
    {
        if (IsClient)
        {
            client.Disconnect();
            return;
        }

        if (IsServer)
        {
            server.Disconnect(this);
            return;
        }
    }

    /// <summary>
    /// Sends a serialized message over the network using the specified object.
    /// </summary>
    /// <param name="obj">The serializable object to be sent. Must have an associated serializer.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the object does not have an associated serializer or if the serializer
    /// has not been registered.
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
                msgWriter.CompressUInt64(index);

                obj.Serializer.Serialize(obj, msgWriter);
            }
            catch (Exception ex)
            {
                msgWriter.Position = position;
                
                log.Error($"Failed to serialize message, rolling back!\n{ex}");
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
                writer.CompressInt64(msgWriter.Position);

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

    private void Handle(ISerializableObject obj)
    {
        var type = obj.GetType();

        try
        {
            if (messageHandlers.TryGetValue(type, out var handler))
            {
                handler(obj);
            }
            else
            {
                for (var x = 0; x < netServices.Count; x++)
                {
                    var service = netServices[x];

                    if (!service.IsValid || !service.IsRunning)
                        continue;

                    if (service.Receive(obj))
                        return;
                }

                log.Warn($"No handler for message of type &1{type.Name}&r");
            }
        }
        catch (Exception ex)
        {
            log.Error($"Could not handle message of type &1{type.Name}&r:\n{ex}");
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
        var count = (int)reader.DecompressInt64();
        var position = reader.Position + count;
        
        log.Debug($"Attempting to read messages (Count={count}; Position={position}; CurPosition={reader.Position}) ...");

        while (reader.Position < position && reader.Remaining > 2)
        {
            try
            {
                var index = (ushort)reader.DecompressUInt64();
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

                Handle(message);
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