using System.Net.Sockets;
using System.Collections.Concurrent;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API.Internal.Tcp;

/// <summary>
/// Represents a pipe for sending data from a TCP client.
/// This class manages the sending of messages through a queue
/// and ensures thread-safe communication with the associated TCP client.
/// </summary>
public class TcpServerSendPipe
{
    internal long sentBytes = 0;
    
    private volatile bool stopSignal;
    
    private volatile TcpClient tcpClient;
    private volatile NetConnection netConn;
    private volatile NetworkStream netStream;

    private volatile ConcurrentQueue<ByteWriter> pool = new();
    private volatile ConcurrentQueue<ByteWriter> queue = new();

    /// <summary>
    /// Creates a new instance of the <see cref="TcpServerSendPipe"/> class.
    /// </summary>
    public TcpServerSendPipe(TcpClient tcpClient, NetConnection netConn)
    {
        this.tcpClient = tcpClient;
        this.netConn = netConn;
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
        netConn.Log.DebugIf("TcpServerSendPipe", $"Starting ..", netConn.debugLogs);
        
        stopSignal = false;
        sentBytes = 0;
        
        try
        {
            netConn.Log.DebugIf("TcpServerSendPipe", $"Fetching stream ..", netConn.debugLogs);
            netStream = tcpClient.GetStream();
            
            netConn.Log.DebugIf("TcpServerSendPipe", $"Starting update ..", netConn.debugLogs);

            ThreadPool.QueueUserWorkItem(_ => UpdateQueue());
            
            netConn.Log.DebugIf("TcpServerSendPipe", $"Started!", netConn.debugLogs);
        }
        catch (Exception ex)
        {
            netConn.Log.DebugIf("TcpServerSendPipe", ex, netConn.debugLogs);
            netConn.server.Disconnect(netConn);
        }
    }

    /// <summary>
    /// Stops the TCP client send pipe and cleans up resources.
    /// </summary>
    public void Stop()
    {
        stopSignal = true;

        netConn.Log.DebugIf("TcpServerSendPipe", $"Stopping ..", netConn.debugLogs);
        
        try
        {
            netConn.Log.DebugIf("TcpServerSendPipe", $"Disposing stream ..", netConn.debugLogs);
            
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
        
        netConn.Log.DebugIf("TcpServerSendPipe", $"Clearing queues ..", netConn.debugLogs);
        
        while (pool.TryDequeue(out var writer))
            writer.ReturnToPool();
        
        while (queue.TryDequeue(out var writer))
            writer.ReturnToPool();
    }

    /// <summary>
    /// Returns a previously used <see cref="ByteWriter"/> back to the pool for reuse.
    /// Ensures efficient memory usage by pooling instances of <see cref="ByteWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="ByteWriter"/> instance to return to the pool.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="writer"/> is null.</exception>
    public void ReturnWriter(ByteWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        
        pool.Enqueue(writer);
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

                    netConn.Log.DebugIf("TcpClientSendPipe", $"Sent {length} byte(s) (total {sentBytes})", netConn.debugLogs);
                }
                catch (Exception ex)
                {
                    netConn.Log.Error("TcpClientSendPipe", ex);
                    netConn.server.Disconnect(netConn);
                    
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