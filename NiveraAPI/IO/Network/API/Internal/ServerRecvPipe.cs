using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;

namespace NiveraAPI.IO.Network.API.Internal;

/// <summary>
/// Represents a server-side receive pipe for managing and processing incoming network data.
/// This class provides functionality for receiving data from a socket and dispatching it
/// to multiple threads for processing.
/// </summary>
public class ServerRecvPipe
{
    private static volatile IPEndPoint defaultEp = new(IPAddress.Any, 0);
    
    private volatile Socket socket;
    private volatile NetServer server;
    private volatile CancellationTokenSource cts;

    private volatile ConcurrentQueue<ReceivedData> dataPool = new();
    private volatile ConcurrentQueue<ReceivedData> dataQueue = new();

    private long recvBytes = 0;

    /// <summary>
    /// Gets the size of the received data queue.
    /// </summary>
    public int Size => dataQueue.Count;
    
    /// <summary>
    /// Gets the total number of bytes received by the server through all receive threads.
    /// </summary>
    public long ReceivedBytes => recvBytes;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerRecvPipe"/> class.
    /// </summary>
    /// <param name="server">The associated <see cref="NetServer"/> instance.</param>
    /// <param name="socket">The socket used for receiving data.</param>
    public ServerRecvPipe(NetServer server, Socket socket)
    {
        this.server = server;
        this.socket = socket;
    }

    /// <summary>
    /// Starts the server receive pipeline by initializing the cancellation token source
    /// and launching multiple threads for handling incoming data based on the specified
    /// number of receive threads in the associated <see cref="NetServer"/>.
    /// </summary>
    public void Start()
    {
        cts = new();
        
        for (var x = 0; x < server.ReceiveThreads; x++)
            StartThread(x);
    }

    /// <summary>
    /// Stops the server receive pipeline by canceling all ongoing operations,
    /// clearing the connection dictionary, and disposing of all pooled received data.
    /// </summary>
    public void Stop()
    {
        cts.Cancel();
        
        server.Log.Debug("ServerRecvPipe", "Stopping threads...");

        while (dataPool.TryDequeue(out var data)
               || dataQueue.TryDequeue(out data))
        {
            data.Args.Dispose();
            data.Reader.ReturnToPool();
        }

        server.Log.Debug("ServerRecvPipe", "Threads stopped, internal queues cleared");
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="ReceivedData"/> instance from the queue.
    /// </summary>
    /// <param name="data">
    /// When this method returns, contains the <see cref="ReceivedData"/> instance removed from the queue,
    /// or null if the queue is empty.
    /// </param>
    /// <returns>
    /// True if a <see cref="ReceivedData"/> instance was successfully dequeued; otherwise, false.
    /// </returns>
    public bool Grab(out ReceivedData data)
        => dataQueue.TryDequeue(out data);

    /// <summary>
    /// Returns a <see cref="ReceivedData"/> instance to the data pool for reuse.
    /// </summary>
    /// <param name="data">
    /// The <see cref="ReceivedData"/> object to be returned. This cannot be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="data"/> is null.
    /// </exception>
    public void Return(ReceivedData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        dataPool.Enqueue(data);
    }

    private void StartThread(int index)
    {
        var data = GetData();
        
        DispatchThread(data.Args);
        
        server.Log.Debug("ServerRecvPipe", $"Started thread ID {index}");
    }

    private void DispatchThread(SocketAsyncEventArgs args)
    {
        if (cts.IsCancellationRequested)
        {
            server.Log.Debug("ServerRecvPipe", "Thread was cancelled");
            return;
        }

        if (args.UserToken is not ReceivedData data)
        {
            server.Log.Error("ServerRecvPipe", "UserToken is not a ReceivedData instance");
            return;
        }

        try
        {
            args.SetBuffer(args.Buffer, 0, args.Buffer.Length);

            var pending = socket.ReceiveFromAsync(args);

            if (!pending)
                OnCompleted(null!, args);
        }
        catch (Exception ex)
        {
            server.Log.Error("ServerRecvPipe", ex);
        }
    }
    
    private void OnCompleted(object _, SocketAsyncEventArgs args)
    {
        if (args.UserToken is not ReceivedData data)
        {
            server.Log.Error("ServerRecvPipe", "OnCompleted received a SocketAsyncEventArgs instance with a null or invalid UserToken");
            return;
        }
        
        if (cts.IsCancellationRequested)
        {
            server.Log.Debug("ServerRecvPipe", "Thread was cancelled");
            return;
        }

        if (args.SocketError != SocketError.Success)
        {
            server.Log.Error("ServerRecvPipe", $"Thread caught an error: {args.SocketError}");
        }
        else
        {
            if (args.BytesTransferred > 0)
            {
                data.Reader.Offset = 0;
                data.Reader.Position = 0;

                data.Reader.Count = args.BytesTransferred;

                dataQueue.Enqueue(data);

                Interlocked.Add(ref recvBytes, args.BytesTransferred);
                
                server.Log.Debug("ServerRecvPipe", $"Received {args.BytesTransferred} bytes ({recvBytes} total)");
            }
        }

        DispatchThread(args);
    }


    private ReceivedData GetData()
    {
        if (dataPool.TryDequeue(out var data))
            return data;

        data = new(true);
        data.Args.Completed += OnCompleted;
        
        return data;
    }
}