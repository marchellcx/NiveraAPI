using System.Collections.Concurrent;
using System.Net.Sockets;

namespace NiveraAPI.IO.Network.API.Internal;

/// <summary>
/// Represents a network data receiving pipeline for a client.
/// Handles the reception, error monitoring, and exception collection
/// from multiple receive threads associated with a client socket.
/// </summary>
public class ClientRecvPipe
{
    private volatile Socket sock;
    private volatile NetClient client;
    private volatile CancellationTokenSource cts;

    private volatile ConcurrentQueue<ReceivedData> dataPool = new();
    private volatile ConcurrentQueue<ReceivedData> dataQueue = new();

    private long recvBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientRecvPipe"/> class.
    /// </summary>
    /// <param name="client">The client associated with this receive pipe.</param>
    /// <param name="socket">The socket used for receiving data.</param>
    /// <exception cref="ArgumentNullException">Thrown if any of the parameters are null.</exception>
    public ClientRecvPipe(NetClient client, Socket socket)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.sock = socket ?? throw new ArgumentNullException(nameof(socket));
    }

    /// <summary>
    /// Gets the client-side socket.
    /// </summary>
    public Socket Socket => sock;

    /// <summary>
    /// The client associated with this receive pipe.
    /// </summary>
    public NetClient Client => client;

    /// <summary>
    /// Gets the total number of bytes received by the client through all receive threads.
    /// </summary>
    public long ReceivedBytes => recvBytes;

    /// <summary>
    /// Starts receiving data.
    /// </summary>
    public void Start()
    {
        cts = new();
        
        for (var x = 0; x < client.ReceiveThreads; x++)
            StartThread(x);
    }

    /// <summary>
    /// Stops all active receive threads, cancels ongoing operations, clears internal queues,
    /// and resets the internal state of the client receive pipeline.
    /// </summary>
    /// <returns>
    /// An array of key-value pairs representing the status of each stopped thread.
    /// Each pair contains a <c>SocketError?</c> value indicating the error status (if any)
    /// and an <c>Exception?</c> value representing any encountered exception.
    /// </returns>
    public void Stop()
    {
        cts.Cancel();
        
        client.Log.Debug("ClientRecvPipe", "Stopping threads ..");

        while (dataPool.TryDequeue(out var data))
        {
            data.Args.Dispose();
            data.Reader.ReturnToPool();
        }
        
        while (dataQueue.TryDequeue(out var data)) 
        {
            data.Args.Dispose();
            data.Reader.ReturnToPool();
        }
        
        client.Log.Debug("ClientRecvPipe", "Threads stopped, internal queues cleared");
    }

    /// <summary>
    /// Returns a previously used instance of <see cref="ClientRecvPipe.ReceivedData"/>
    /// back to the internal data pool for reuse. Resets its state to the default values.
    /// </summary>
    /// <param name="data">The <see cref="ClientRecvPipe.ReceivedData"/> instance to be returned
    /// to the data pool. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="data"/> is null.</exception>
    public void Return(ReceivedData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        dataPool.Enqueue(data);
    }

    /// <summary>
    /// Attempts to retrieve and remove the next available <see cref="ReceivedData"/> object from the data queue.
    /// </summary>
    /// <param name="data">
    /// When this method returns, contains the <see cref="ReceivedData"/> object removed from the queue, or <c>null</c> if the queue is empty.
    /// </param>
    /// <returns>
    /// <c>true</c> if an object was successfully retrieved from the queue; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGrab(out ReceivedData data)
        => dataQueue.TryDequeue(out data);

    private void StartThread(int index)
    {
        var data = GetData();
        
        DispatchThread(data.Args);
        
        client.Log.Debug("ClientRecvPipe", $"Started thread ID {index}");
    }

    private void DispatchThread(SocketAsyncEventArgs args)
    {
        if (cts.IsCancellationRequested)
        {
            client.Log.Debug("ClientRecvPipe", "Thread was cancelled");
            return;
        }

        try
        {
            args.RemoteEndPoint = null;
            args.SetBuffer(args.Buffer, 0, args.Buffer.Length);

            var pending = sock.ReceiveAsync(args);

            if (!pending)
                OnCompleted(null!, args);
        }
        catch (Exception ex)
        {
            if (!cts.IsCancellationRequested)
                cts.Cancel();
            
            client.queue.AddToQueue(() => client.OnReceivePipeError(SocketError.OperationAborted, ex));
        }
    }

    private void OnCompleted(object _, SocketAsyncEventArgs args)
    {
        if (args.UserToken is not ReceivedData data)
        {
            client.Log.Error("ClientRecvPipe", "OnCompleted received a SocketAsyncEventArgs instance with a null or invalid UserToken");
            return;
        }
        
        if (cts.IsCancellationRequested)
        {
            client.Log.Debug("ClientRecvPipe", "Thread was cancelled");
            return;
        }

        if (args.SocketError != SocketError.Success)
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                client.queue.AddToQueue(() => client.OnReceivePipeError(args.SocketError, null!));
            }

            return;
        }
        
        if (args.BytesTransferred > 0)
        {
            data.Reader.Offset = 0;
            data.Reader.Position = 0;
            
            data.Reader.Count = args.BytesTransferred;
            
            dataQueue.Enqueue(data);

            Interlocked.Add(ref recvBytes, args.BytesTransferred);
            
            client.Log.Debug("ClientRecvPipe", $"Received {args.BytesTransferred} bytes ({recvBytes} total)");
        }
        
        DispatchThread(args);
    }

    private ReceivedData GetData()
    {
        if (dataPool.TryDequeue(out var data))
            return data;

        data = new(false);
        data.Args.Completed += OnCompleted;
        
        return data;
    }
}