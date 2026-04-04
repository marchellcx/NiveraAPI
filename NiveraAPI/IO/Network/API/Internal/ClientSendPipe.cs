using System.Net.Sockets;

using System.Collections.Concurrent;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API.Internal;

/// <summary>
/// Represents a client-side send pipe for managing and processing outbound network data.
/// This class provides functionality for enqueuing data to be sent, retrieving reusable
/// writers, and handling data transfer using an internal sending queue.
/// </summary>
public class ClientSendPipe
{
    private volatile Socket socket;
    private volatile NetClient client;

    private volatile ConcurrentQueue<ByteWriter> pool = new();
    private volatile ConcurrentQueue<SocketAsyncEventArgs> argsPool = new();

    private long sentBytes = 0;

    /// <summary>
    /// Gets the cumulative number of bytes successfully sent over the network.
    /// </summary>
    public long SentBytes => sentBytes;

    /// <summary>
    /// Creates a new instance of the <see cref="ClientSendPipe"/> class.
    /// </summary>
    /// <param name="client">The associated <see cref="NetClient"/> instance.</param>
    /// <param name="socket">The underlying <see cref="Socket"/> for data transmission.</param>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="client"/> or <paramref name="socket"/> is null.</exception>
    public ClientSendPipe(NetClient client, Socket socket)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
    }

    /// <summary>
    /// Stops the internal sending queue process and cancels any pending operations.
    /// </summary>
    public void Stop()
    {
        client.Log.Debug("Stopping ClientSendPipe");
        
        while (pool.TryDequeue(out var writer))
            writer.ReturnToPool();

        while (argsPool.TryDequeue(out var args))
            args.Dispose();
        
        client.Log.Debug("ClientSendPipe stopped");

        sentBytes = 0;
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
        
        var args = GetArgs();

        args.UserToken = writer;
        args.RemoteEndPoint = socket.RemoteEndPoint;

        try
        {
            args.SetBuffer(writer.Buffer, 0, writer.Position);

            var pending = socket.SendAsync(args);

            if (!pending)
                OnCompleted(null!, args);
        }
        catch (Exception ex)
        {
            client.Log.Error("ClientSendPipe", ex);
            client.queue.AddToQueue(() => client.OnSendPipeError(SocketError.OperationAborted, ex));
        }
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

        writer.Buffer = new byte[NetSettings.MTU];
        writer.Position = 0;

        return writer;
    }

    /// <summary>
    /// Returns a <see cref="ByteWriter"/> instance to the internal pool after resetting its state.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="ByteWriter"/> instance to be returned to the pool.
    /// The writer must not be null, and its buffer will be reset to match the size defined by <see cref="NetSettings.MTU"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="writer"/> is null.
    /// </exception>
    public void ReturnWriter(ByteWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        if (writer.Buffer == null || writer.Buffer.Length != NetSettings.MTU)
            writer.Buffer = new byte[NetSettings.MTU];

        writer.Position = 0;

        pool.Enqueue(writer);
    }

    private SocketAsyncEventArgs GetArgs()
    {
        if (argsPool.TryDequeue(out var args))
            return args;

        args = new SocketAsyncEventArgs();
        args.Completed += OnCompleted;

        return args;
    }

    private void OnCompleted(object _, SocketAsyncEventArgs args)
    {
        if (args.UserToken is ByteWriter writer)
            ReturnWriter(writer);

        args.UserToken = null;
        
        Interlocked.Add(ref sentBytes, args.BytesTransferred);

        client.Log.Debug($"Sent {args.BytesTransferred} bytes ({args.SocketError}) ({sentBytes} total)");
        
        if (args.SocketError != SocketError.Success)
            client.queue.AddToQueue(() => client.OnSendPipeError(args.SocketError, null!));
        
        argsPool.Enqueue(args);
    }
}