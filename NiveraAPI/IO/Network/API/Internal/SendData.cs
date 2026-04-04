using System.Net.Sockets;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API.Internal;

/// <summary>
/// Represents the data to be sent to a client through a single send thread.
/// </summary>
public class SendData
{
    /// <summary>
    /// The buffer containing the received data.
    /// </summary>
    public volatile byte[] Buffer;
    
    /// <summary>
    /// The writer used to write data to the buffer.
    /// </summary>
    public volatile ByteWriter Writer;

    /// <summary>
    /// The socket asynchronous event arguments used for receiving data.
    /// </summary>
    public volatile SocketAsyncEventArgs Args;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendData"/> class.
    /// </summary>
    public SendData()
    {
        Buffer = new byte[NetSettings.MTU];

        Writer = ByteWriter.Get();
        Writer.Buffer = Buffer;

        Args = new() { UserToken = this };
        Args.SetBuffer(Buffer, 0, Buffer.Length);
    }
}