using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;

using NiveraAPI.Logs;
using NiveraAPI.Console;
using NiveraAPI.Services;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Network;

/// <summary>
/// Represents a networking server that supports both TCP and UDP communication modes,
/// allowing the management of connections, data transmission, and service provisioning.
/// </summary>
public class NetServer : ServiceCollection
{
    private static volatile LogSink log = LogManager.GetSource("IO", "NetServer");

    internal long sentBytes;
    internal long recvBytes;
    
    private volatile TcpListener listener;
    
    private volatile CancellationTokenSource sendCts;
    private volatile CancellationTokenSource connectCts;
    
    internal volatile bool debugLogs;
    
    private volatile int connId = 0;
    private volatile Predicate<TcpClient>? predicate;

    private volatile ActionQueue queue = new();
    private volatile ConcurrentDictionary<int, NetConnection> conns = new();

    /// <summary>
    /// Gets called when a new connection is established.
    /// </summary>
    public event Action<NetConnection>? Connected;

    /// <summary>
    /// Gets called when a connection is disconnected.
    /// </summary>
    public event Action<NetConnection>? Disconnected; 
    
    /// <summary>
    /// Gets the total number of bytes sent by the server.
    /// </summary>
    public long SentBytes => sentBytes;

    /// <summary>
    /// Gets the total number of bytes received by the server.
    /// </summary>
    public long ReceivedBytes => recvBytes;
    
    /// <summary>
    /// Gets or sets the maximum number of retransmissions allowed for a message that does not have a handler assigned until it is discarded.
    /// </summary>
    public int MaxRetransmissions { get; set; }

    /// <summary>
    /// Whether debug logs are enabled.
    /// </summary>
    public bool DebugLogs
    {
        get => debugLogs;
        set => debugLogs = value;
    }

    /// <summary>
    /// Gets or sets a predicate that determines whether a TCP client connection is accepted.
    /// </summary>
    /// <remarks>
    /// This property allows filtering incoming TCP client connections based on custom logic.
    /// The assigned predicate must return true to accept the connection or false to reject it.
    /// If set to null, an <see cref="ArgumentNullException"/> will be thrown.
    /// </remarks>
    public Predicate<TcpClient>? Predicate
    {
        get => predicate;
        set => predicate = value;
    }

    /// <summary>
    /// Gets the logging mechanism associated with the network server.
    /// </summary>
    public LogSink Log => log;
    
    /// <summary>
    /// The list of connections currently connected to the server.
    /// </summary>
    public IReadOnlyDictionary<int, NetConnection> Connections => conns;
    
    /// <summary>
    /// The list of services provided by the server.
    /// </summary>
    public volatile ConcurrentBag<Type> ProvidedServices = new();

    /// <summary>
    /// Starts listening for incoming connections on the specified port, using TCP or UDP based on the provided configuration.
    /// </summary>
    /// <param name="port">The port number to listen on. If set to 0, an available port will be selected automatically.</param>
    public void Listen(int port = 0)
    {
        log.DebugIf($"Starting server on port {port}...", debugLogs);

        if (listener != null)
        {
            DisconnectAll();

            try
            {
                listener.Stop();
            }
            catch
            {
                // ignored
            }
        }

        sendCts?.Cancel();
        connectCts?.Cancel();

        sendCts = new();
        connectCts = new();
        
        listener = new(IPAddress.Any, port);
        listener.ExclusiveAddressUse = false;
        
        listener.Start();
        
        ThreadPool.QueueUserWorkItem(ThreadUpdate, sendCts);
        ThreadPool.QueueUserWorkItem(ThreadAccept, connectCts);
    }

    /// <inheritdoc />
    public override void Stop()
    {
        log.DebugIf("Stopping server...", debugLogs);
        
        base.Stop();
        
        DisconnectAll();
        
        sendCts.Cancel();
        connectCts.Cancel();

        recvBytes = 0;
        sentBytes = 0;

        try
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null!;
            }
        }
        catch
        {
            // ignored
        }
        
        log.DebugIf("Stopped server", debugLogs);
    }

    /// <summary>
    /// Disconnects the specified network connection from the server.
    /// Upon disconnection, the connection will no longer be managed by the server or processed by the associated action queue.
    /// </summary>
    /// <param name="conn">The network connection to be disconnected.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided connection is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided connection is not managed by this server.</exception>
    public void Disconnect(NetConnection conn)
    {
        if (conn == null)
            throw new ArgumentNullException(nameof(conn));
        
        queue.AddToQueue(() => RemoveConnection(conn));
    }

    /// <summary>
    /// Disconnects all currently connected clients by removing their connections from the server's connection list.
    /// This operation ensures that no active connections remain.
    /// </summary>
    public void DisconnectAll()
    {
        foreach (var kvp in conns)
        {
            log.DebugIf($"Removing connection {kvp.Key}", debugLogs);

            try
            {
                kvp.Value.Stop();

                Disconnected?.Invoke(kvp.Value);

                if (kvp.Value.TcpClient != null)
                {
                    try
                    {
                        kvp.Value.TcpClient.Close();
                    }
                    catch
                    {
                        // ignored
                    }

                    kvp.Value.ServerSendPipe?.Stop();
                    kvp.Value.ServerReceivePipe?.Stop();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        conns.Clear();
    }

    /// <summary>
    /// Processes incoming network data and updates the state of all active network connections.
    /// This method retrieves and processes data from the receive pipe, attempting to find or register
    /// the appropriate connection for the received data. If a connection is found or created, the data
    /// is passed to the connection's receive handler. After processing the receive pipe, the state of all
    /// active connections is updated.
    /// </summary>
    /// <remarks>
    /// Exceptions encountered during data processing or connection updates are logged but do not interrupt
    /// the execution of the method or processing of other connections.
    /// </remarks>
    public void Update()
    {
        queue.UpdateQueue();
        
        foreach (var kvp in conns)
        {
            try
            {
                if (kvp.Value.ServerReceivePipe == null)
                {
                    log.Warn("Encountered connection with a null RecvPipe!");
                    continue;
                }

                while (kvp.Value.ServerReceivePipe.TryGrab(out var reader))
                {
                    try
                    {
                        kvp.Value.Receive(reader);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Could not process incoming data for connection &1{kvp.Value.EndPoint}&r:\n{ex}");
                        
                        kvp.Value.ServerReceivePipe.Return(reader);
                        continue;
                    }

                    kvp.Value.ServerReceivePipe.Return(reader);
                }
                
                kvp.Value.Update();
            }
            catch (Exception ex)
            {
                log.Error($"Could not update connection:\n{ex}");
            }
        }
    }
    
    private void RemoveConnection(NetConnection conn)
    {
        if (!conns.TryRemove(conn.Id, out _))
            return;
        
        queue.AddToQueue(() =>
        {
            log.DebugIf($"Removing connection {conn.Id}", debugLogs);

            try
            {
                conn.Stop();

                Disconnected?.Invoke(conn);

                if (conn.TcpClient != null)
                {
                    try
                    {
                        conn.TcpClient.Close();
                    }
                    catch
                    {
                        // ignored
                    }

                    conn.ServerSendPipe?.Stop();
                    conn.ServerReceivePipe?.Stop();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        });
    }
    
    private void ThreadUpdate(object obj)
    {
        var cts = (CancellationTokenSource)obj;
        
        while (!cts.IsCancellationRequested)
        {
            Thread.Sleep(1);
            
            try
            {
                foreach (var kvp in conns)
                {
                    if (!kvp.Value.HasData)
                        continue;
                    
                    if (kvp.Value.ServerSendPipe == null)
                        continue;
                    
                    log.DebugIf($"Connection &1{kvp.Value.EndPoint}&r has data, serializing ..", debugLogs);
                    
                    var writer = kvp.Value.ServerSendPipe.GetWriter();

                    try
                    {
                        if (!kvp.Value.TryWrite(writer))
                        {
                            log.DebugIf($"Connection &1{kvp.Value.EndPoint}&r is not ready to send data, queuing ..", debugLogs);
                            
                            kvp.Value.ServerSendPipe.ReturnWriter(writer);
                        }
                        else
                        {
                            kvp.Value.ServerSendPipe.Send(writer);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error sending data to &1{kvp.Value.EndPoint}&r:\n{ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }

    private void RegisterNew(TcpClient client)
    {
        log.DebugIf($"Registering new connection: {client.Client.RemoteEndPoint}", debugLogs);
        
        var conn = new NetConnection(this, client, Interlocked.Increment(ref connId));
        
        conn.Start();
        
        conns.TryAdd(conn.Id, conn);

        Connected?.Invoke(conn);
    }

    private void ThreadAccept(object obj)
    {
        var cts = (CancellationTokenSource)obj;

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var client = listener.AcceptTcpClient();

                if (client != null)
                {
                    try
                    {
                        if (predicate != null && !predicate(client))
                        {
                            log.DebugIf($"Rejected connection from &1{client.Client.RemoteEndPoint}&r", debugLogs);

                            client.Close();
                            client.Dispose();
                        }
                        else
                        {
                            client.NoDelay = true;

                            client.SendBufferSize = NetSettings.MTU;
                            client.ReceiveBufferSize = NetSettings.MTU;

                            queue.AddToQueue(() => RegisterNew(client));
                        }
                    }
                    catch (Exception ex)
                    {
                         ConsoleOutput.Write($"Error while accepting client:\n{ex}", ConsoleColor.DarkRed);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}