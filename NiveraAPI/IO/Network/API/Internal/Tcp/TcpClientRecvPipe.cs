using System.Collections.Concurrent;
using System.Net.Sockets;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API.Internal.Tcp;

/// <summary>
/// The <see cref="TcpClientRecvPipe"/> class provides a mechanism to handle
/// and manage data received from a <see cref="TcpClient"/>. It operates as a
/// processing pipeline for reading and dispatching inbound TCP network data.
/// </summary>
public class TcpClientRecvPipe
{
    internal long receivedBytes = 0;
    
    private volatile bool stopSignal = false;
    
    private volatile TcpClient tcpClient;
    private volatile NetClient netClient;
    private volatile NetworkStream netStream;

    private volatile ConcurrentQueue<ByteReader> pool = new();
    private volatile ConcurrentQueue<ByteReader> queue = new();

    /// <summary>
    /// Creates a new instance of the <see cref="TcpClientRecvPipe"/> class.
    /// </summary>
    public TcpClientRecvPipe(TcpClient tcpClient, NetClient netClient)
    {
        this.tcpClient = tcpClient;
        this.netClient = netClient;
    }

    /// <summary>
    /// Starts the receive processing pipeline for the associated TCP client.
    /// This initializes the network stream and queues the internal stream
    /// update process to the thread pool for continuous data reception.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the network stream cannot be initialized from the TCP client.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs while attempting to obtain the network stream.
    /// </exception>
    public void Start()
    {
        netClient.Log.DebugIf("TcpClientRecvPipe", $"Starting ..", netClient.DebugLogs);
        
        stopSignal = false;
        receivedBytes = 0;
        
        netClient.Log.DebugIf("TcpClientRecvPipe", $"Fetching stream ..", netClient.DebugLogs);
        
        try
        {
            netStream = tcpClient.GetStream();
        }
        catch (Exception ex)
        {
            netClient.TcpOnReceivePipeError(ex);
            return;
        }
        
        netClient.Log.DebugIf("TcpClientRecvPipe", $"Starting update ..", netClient.DebugLogs);

        ThreadPool.QueueUserWorkItem(_ => UpdateStream());
        
        netClient.Log.DebugIf("TcpClientRecvPipe", $"Started!", netClient.DebugLogs);
    }

    /// <summary>
    /// Stops the receive processing pipeline and performs necessary cleanup operations.
    /// This includes signaling the stop process, closing and disposing the network stream if active,
    /// and returning all queued resources, such as <see cref="ByteReader"/> objects, to their respective pools.
    /// </summary> 
    public void Stop()
    {
        netClient.Log.DebugIf("Stopping TcpClientRecvPipe ..", netClient.DebugLogs);
        
        stopSignal = true;

        try
        {
            netClient.Log.DebugIf("TcpClientRecvPipe", $"Disposing stream ..", netClient.DebugLogs);
            
            if (netStream != null)
            {
                netStream.Close();
                netStream.Dispose();
            }
        }
        catch
        {
            // ignored
        }

        netStream = null!;
        
        netClient.Log.DebugIf("TcpClientRecvPipe", $"Clearing pools and queues", netClient.DebugLogs);
        
        while (pool.TryDequeue(out var reader))
            reader.ReturnToPool();
        
        while (queue.TryDequeue(out var reader))
            reader.ReturnToPool();
    }

    /// <summary>
    /// Returns a <see cref="ByteReader"/> instance to the internal pool for reuse.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="ByteReader"/> instance to be returned to the pool.
    /// This parameter cannot be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="reader"/> is null.
    /// </exception>
    public void Return(ByteReader reader)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));
        
        pool.Enqueue(reader);
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="ByteReader"/> instance from the internal pool.
    /// </summary>
    /// <param name="reader">
    /// When this method returns, contains the <see cref="ByteReader"/> instance retrieved from the pool if the operation was successful;
    /// otherwise, it will be null if the pool is empty.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if a <see cref="ByteReader"/> instance was successfully retrieved from the pool; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGrab(out ByteReader reader)
        => queue.TryDequeue(out reader);

    private void UpdateStream()
    {
        var length = new byte[4];
        var buffer = new byte[NetSettings.MTU + 4];
        var accum = new MemoryStream();
        
        while (!stopSignal)
        {
            try
            {
                if (!netStream.DataAvailable)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                var bytesRead = netStream.Read(buffer, 0, buffer.Length);
                
                if (bytesRead == 0)
                {
                    stopSignal = true;
                    
                    netClient.TcpOnReceivePipeError(new IOException("Connection closed by remote host"));
                    break;
                }
                
                accum.Write(buffer, 0, bytesRead);
                
                while (TryExtractMessage(length, accum, out var reader))
                {
                    queue.Enqueue(reader);
                    
                    Interlocked.Add(ref receivedBytes, reader.Count);

                    netClient.Log.DebugIf("TcpClientRecvPipe", $"Received {reader.Count} byte(s) (total {receivedBytes})", netClient.DebugLogs);
                }
            }
            catch (Exception ex)
            {
                stopSignal = true;
                netClient.TcpOnReceivePipeError(ex);
                return;
            }
        }
        
        accum.Dispose();
    }
    
    private bool TryExtractMessage(byte[] length, MemoryStream accum, out ByteReader reader)
    {
        reader = null!;

        if (accum.Length < 4)
            return false;
        
        var originalPos = accum.Position;
        
        accum.Position = 0;
        
        accum.Read(length, 0, 4);
        accum.Position = originalPos;

        var size = (length[0] << 24) | (length[1] << 16) | (length[2] << 8) | length[3];
        
        if (size < 0 || size > NetSettings.MTU)
            throw new InvalidDataException($"Invalid message length: {size}");

        if (accum.Length < 4 + size)
            return false;
        
        if (!pool.TryDequeue(out reader))
        {
            reader = new();
            reader.Buffer = new byte[NetSettings.MTU];
        }
        
        accum.Position = 0;
        
        accum.Read(length, 0, 4);
        accum.Read(reader.Buffer, 0, size);

        reader.Count    = size;
        reader.Position = 0;
        
        var remaining = (int)accum.Length - (4 + size);
        
        if (remaining > 0)
        {
            var leftover = new byte[remaining];
            
            accum.Read(leftover, 0, remaining);
            
            accum.SetLength(0);
            
            accum.Write(leftover, 0, remaining);
        }
        else
        {
            accum.SetLength(0);
        }

        return true;
    }
}