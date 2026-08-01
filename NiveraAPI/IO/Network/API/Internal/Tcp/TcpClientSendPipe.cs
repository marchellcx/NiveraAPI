using System.Net.Sockets;
using System.Collections.Concurrent;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API.Internal.Tcp;

/// <summary>
/// Represents a pipe for sending data from a TCP client.
/// This class manages the sending of messages through a queue
/// and ensures thread-safe communication with the associated TCP client.
/// </summary>
public class TcpClientSendPipe
{
    internal long sentBytes = 0;
    
    private volatile bool stopSignal;
    
    private volatile TcpClient tcpClient;
    private volatile NetClient netClient;
    private volatile NetworkStream netStream;

    private volatile ConcurrentQueue<ByteWriter> pool = new();
    private volatile ConcurrentQueue<ByteWriter> queue = new();

    /// <summary>
    /// Creates a new instance of the <see cref="TcpClientSendPipe"/> class.
    /// </summary>
    public TcpClientSendPipe(TcpClient tcpClient, NetClient netClient)
    {
        this.tcpClient = tcpClient;
        this.netClient = netClient;
    }

    /// <summary>
    /// Initializes and starts the TCP client send pipe, enabling data transmission.
    /// </summary>
    /// <remarks>
    /// This method establishes a network stream for communication with the TCP client
    /// and begins processing the send queue in a background thread.
    /// </remarks>
    public void Start()
    {
        netClient.Log.DebugIf("TcpClientSendPipe", $"Starting ..", netClient.DebugLogs);
        
        stopSignal = false;
        sentBytes = 0;
        
        netClient.Log.DebugIf("TcpClientSendPipe", $"Fetching stream ..", netClient.DebugLogs);
        
        try
        {
            netStream = tcpClient.GetStream();
            
            netClient.Log.DebugIf("TcpClientSendPipe", $"Starting update ..", netClient.DebugLogs);

            ThreadPool.QueueUserWorkItem(_ => UpdateQueue());
            
            netClient.Log.DebugIf("TcpClientSendPipe", $"Started!", netClient.DebugLogs);
        }
        catch (Exception ex)
        {
            netClient.TcpOnSendPipeError(ex);
        }
    }

    /// <summary>
    /// Stops the TCP client send pipe and cleans up resources.
    /// </summary>
    public void Stop()
    {
        stopSignal = true;
        
        netClient.Log.DebugIf("TcpClientSendPipe", $"Stopping ..", netClient.DebugLogs);

        try
        {
            netClient.Log.DebugIf("TcpClientSendPipe", $"Disposing stream ..", netClient.DebugLogs);
            
            if (netStream != null)
            {
                netStream.Close();
                netStream.Dispose();
                netStream = null!;
            }
        }
        catch
        {
            // ignored
        }
        
        netClient.Log.DebugIf("TcpClientSendPipe", $"Clearing queues ..", netClient.DebugLogs);
        
        while (pool.TryDequeue(out var writer))
            writer.ReturnToPool();
        
        while (queue.TryDequeue(out var writer))
            writer.ReturnToPool();
    }
    
    /// <summary>
    /// Retrieves a <see cref="ByteWriter"/> from the internal pool or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>
    /// A <see cref="ByteWriter"/> instance. If a pooled instance is available, it is reused;
    /// otherwise, a new instance with a predefined buffer size is created and returned.
    /// </returns>
    public ByteWriter GetWriter()
    {
        if (pool.TryDequeue(out var writer))
        {
            writer.Position = 0;
            return writer;
        }

        writer = new();
        writer.Position = 0;
        
        writer.Buffer = new byte[NetSettings.MTU];
        return writer;
    }
    
    /// <summary>
    /// Enqueues the provided <see cref="ByteWriter"/> for sending over the network.
    /// </summary>
    /// <param name="writer">The <see cref="ByteWriter"/> instance to be sent. It must contain data and not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="writer"/> is null.</exception>
    /// <exception cref="Exception">Thrown when the provided <paramref name="writer"/> has no data to send (Position is less than 1).</exception>
    public void Send(ByteWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        if (writer.Position < 1)
            throw new Exception("Cannot send an empty writer");
        
        netClient.Log.DebugIf("TcpClientSendPipe", $"Added data to queue: &1{writer.Position}&r", netClient.DebugLogs);
        
        queue.Enqueue(writer);
    }

    private void UpdateQueue()
    {
        var lengthPrefix = new byte[4];

        while (!stopSignal)
        {
            while (queue.TryDequeue(out var writer))
            {
                try
                {
                    var length = writer.Position;

                    if (length <= 0 || length > NetSettings.MTU)
                    {
                        pool.Enqueue(writer);
                        continue;
                    }
                    
                    lengthPrefix[0] = (byte)(length >> 24);
                    lengthPrefix[1] = (byte)(length >> 16);
                    lengthPrefix[2] = (byte)(length >> 8);
                    lengthPrefix[3] = (byte)length;

                    netStream.Write(lengthPrefix, 0, 4);
                    netStream.Write(writer.Buffer, 0, length);
                    
                    netStream.Flush();

                    Interlocked.Add(ref sentBytes, length);

                    netClient.Log.DebugIf("TcpClientSendPipe", $"Sent {length} byte(s) (total {sentBytes})", netClient.DebugLogs);
                }
                catch (Exception ex)
                {
                    netClient.TcpOnSendPipeError(ex);
                    
                    stopSignal = true;
                }
                finally
                {
                    pool.Enqueue(writer);
                }
            }

            Thread.Sleep(1); 
        }
    }
}