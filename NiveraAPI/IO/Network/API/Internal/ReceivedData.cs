using System.Net;
using System.Net.Sockets;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API.Internal;

/// <summary>
/// Represents the data received by a client through a single receive thread.
/// </summary>
public class ReceivedData
{
    /// <summary>
    /// The buffer containing the received data.
    /// </summary>
    public volatile byte[] Buffer;
        
    /// <summary>
    /// The reader used to access the data in the buffer.
    /// </summary>
    public volatile ByteReader Reader;

    /// <summary>
    /// The socket asynchronous event arguments used for receiving data.
    /// </summary>
    public volatile SocketAsyncEventArgs Args;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReceivedData"/> class.
    /// </summary>
    public ReceivedData(bool isServer)
    {
        Buffer = new byte[NetSettings.MTU];

        Reader = new();
        Reader.Buffer = Buffer;

        Args = new() { UserToken = this };
        Args.SetBuffer(Buffer, 0, Buffer.Length);

        if (isServer)
            Args.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    }
}