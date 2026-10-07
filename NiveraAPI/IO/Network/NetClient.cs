using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;

using NiveraAPI.IO.Network.API.Internal.Tcp;

using NiveraAPI.Logs;
using NiveraAPI.Services;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Network;

/// <summary>
/// Represents a network client capable of handling connections, sending, and receiving data
/// over a network. This class manages the lifecycle of a network connection, provides logging,
/// and internal state tracking while facilitating communication with a remote endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The <c>NetClient</c> class extends the <see cref="ServiceCollection"/> type,
/// inheriting service-related functionality. It focuses on networking features and exposes
/// various methods for connecting to a remote server, managing communication pipelines,
/// and interacting with internal mechanisms such as the send and receive pipelines.
/// </para>
/// <para>
/// Thread safety is ensured for key operations through the use of volatile fields
/// and threading mechanisms where applicable.
/// </para>
/// </remarks>
public class NetClient : ServiceCollection
{
    private volatile bool connecting;
    private volatile bool connected;
    
    private volatile TcpClient client;
    
    private volatile TcpClientSendPipe sendPipe;
    private volatile TcpClientRecvPipe recvPipe;

    private volatile CancellationTokenSource ctsConnect;
    
    internal volatile bool debugLogs;

    private volatile LogSink log = LogManager.GetSource("IO", "NetClient");

    internal volatile ActionQueue queue = new();
    internal volatile NetConnection conn;

    /// <summary>
    /// Gets called when the client successfully establishes a connection to a remote server.
    /// </summary>
    public event Action? Connected;

    /// <summary>
    /// Gets called when the client is disconnected from the remote server.
    /// </summary>
    public event Action? Disconnected;

    /// <summary>
    /// Gets or sets a value indicating whether debug logs are enabled for the network client.
    /// </summary>
    public bool DebugLogs
    {
        get => debugLogs;
        set => debugLogs = value;
    }

    /// <summary>
    /// Gets the logging mechanism associated with the network client.
    /// </summary>
    public LogSink Log => log;
    
    /// <summary>
    /// Gets or sets the maximum number of retransmissions allowed for a message that does not have a handler assigned until it is discarded.
    /// </summary>
    public int MaxRetransmissions { get; set; }

    /// <summary>
    /// Whether the client is currently attempting to connect to a remote server.
    /// </summary>
    public bool IsConnecting => connecting;

    /// <summary>
    /// Whether the client is currently connected to a remote server.
    /// </summary>
    public bool IsConnected => client is { Connected: true };

    /// <summary>
    /// Gets the total number of bytes sent by the network client's send pipeline.
    /// </summary>
    public long SentBytes => sendPipe?.sentBytes ?? 0;

    /// <summary>
    /// Gets the total number of bytes received by the client through the network pipeline.
    /// </summary>
    public long ReceivedBytes => recvPipe?.receivedBytes ?? 0;

    /// <summary>
    /// Gets the network connection associated with the client.
    /// </summary>
    public NetConnection? Connection
    {
        get => conn;
        private set => conn = value!;
    }

    /// <summary>
    /// List of services that should be added to a newly created connection.
    /// </summary>
    public volatile ConcurrentBag<Type> ProvidedServices = new();

    /// <summary>
    /// Establishes a connection to the specified endpoint using either TCP or UDP based on the provided parameter.
    /// </summary>
    /// <param name="target">The endpoint to which the connection will be established.</param>
    public void Connect(IPEndPoint target)
    {
        if (client != null)
        {
            if (connecting)
            {
                ctsConnect.Cancel();
            }
            else
            {
                Disconnect();
            }
        }

        client = new();
            
        client.SendBufferSize = NetSettings.MTU;
        client.ReceiveBufferSize = NetSettings.MTU;

        connected = false;
        connecting = true;

        ctsConnect = new();
            
        Task.Run(async () =>
        {
            var cts = this.ctsConnect;
            var client = this.client;
            
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000);
                    await client.ConnectAsync(target.Address, target.Port);
                        
                    connected = true;
                    connecting = false;

                    queue.AddToQueue(OnConnected);
                    break;
                }
                catch
                {
                    // ignored
                }
            }

            if (cts.IsCancellationRequested)
            {
                try
                {
                    client.Dispose();
                }
                catch 
                {
                    // ignored
                }
            }
            
            cts.Dispose();
        });
    }

    /// <summary>
    /// Disconnects the client socket if a connection is currently established.
    /// Ensures that the socket is safely disconnected and logs any errors
    /// encountered during the disconnection process.
    /// </summary>
    public void Disconnect()
    {
        try
        {
            log.DebugIf("Disconnecting ..", debugLogs);

            connected = false;
            connecting = false;
            
            if (Connection != null)
            {
                Disconnected?.Invoke();

                RemoveService(typeof(NetConnection));
                
                Connection = null;
            }

            try
            {
                if (client is { Connected: true })
                    client.Close();
                
                client.Dispose();
                client = null!;
            }
            catch
            {
                // ignored
            }
            
            recvPipe?.Stop();
            recvPipe = null!;
            
            sendPipe?.Stop();
            sendPipe = null!;
        }
        catch (Exception ex)
        {
            log.Error($"Could not disconnect!\n{ex}");
        }
    }

    /// <summary>
    /// Updates the state of the client by processing necessary network operations
    /// for either UDP or TCP based on the current mode.
    /// </summary>
    public void Update()
    {
        try
        {
            queue.UpdateQueue();

            if (Connection != null)
            {
                while (recvPipe.TryGrab(out var data))
                {
                    log.DebugIf($"Processing received data: {data.Count} bytes", debugLogs);
                    
                    try
                    {
                        Connection.Receive(data); // in theory this should never throw because it's wrapped in a try-catch
                                                  // block itself but just in case
                    }
                    finally
                    {
                        recvPipe.Return(data);
                    }
                }
                
                Connection.Update();
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed to process action queue:\n{ex}");
        }
    }
    
    /// <inheritdoc />
    public override void Stop()
    {
        base.Stop();
        
        log.DebugIf("Stopping client ..", debugLogs);
        
        Disconnect();
        
        queue.ClearQueue();
    }
    
    private void OnConnected()
    {
        log.DebugIf("Setting up local connection ..", debugLogs);
        
        recvPipe = new(client, this);
        recvPipe.Start();
        
        log.DebugIf("TcpRecvPipe started", debugLogs);

        sendPipe = new(client, this);
        sendPipe.Start();
        
        log.DebugIf("TcpSendPipe started", debugLogs);

        Connection = new(this, client, 0);
        Connection.Start();
        
        log.DebugIf("Connection started", debugLogs);
        
        ThreadPool.QueueUserWorkItem(_ => ThreadUpdate());
        
        log.DebugIf("Update thread started", debugLogs);
        
        Connected?.Invoke();
    }

    internal void OnSendPipeError(Exception ex)
    {
        Disconnect();
    }

    internal void OnReceivePipeError(Exception ex)
    {
        Disconnect();
    }
    
    private void ThreadUpdate()
    {
        log.DebugIf("Update thread started", debugLogs);
        
        while (client != null)
        {
            Thread.Sleep(1);
            
            try
            {
                if (Connection is { HasData: true })
                {
                    log.DebugIf("There is data available to send", debugLogs);
                    
                    var writer = sendPipe.GetWriter();

                    if (Connection.TryWrite(writer))
                        sendPipe.Send(writer);
                    else
                        sendPipe.ReturnWriter(writer);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error while updating connection send:\n{ex}");
            }
        }
        
        log.DebugIf("Update thread exited", debugLogs);
    }
}