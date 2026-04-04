using System.Diagnostics;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API;

/// <summary>
/// Represents a component for tracking network time related to a specific connection.
/// Provides functionality to manage time synchronization, start/stop time tracking,
/// and read/write time state to/from a network stream.
/// </summary>
public class NetTime
{
    private long offsetTicks;
    
    private Stopwatch timer;
    private NetConnection conn;
    
    /// <summary>
    /// Gets the number of elapsed ticks since the time component was started.
    /// </summary>
    public long Ticks => timer.ElapsedTicks + offsetTicks;

    /// <summary>
    /// Gets the current time in seconds since the time component was started.
    /// </summary>
    /// <remarks>This value should be in range of 0.1s of difference to the remote time.</remarks>
    public float Time
    {
        get
        {
            var ticks = Ticks;
            return ticks / (float)TimeSpan.TicksPerSecond;
        }
    }

    /// <summary>
    /// Creates a new time component for the specified connection.
    /// </summary>
    /// <param name="conn">The network connection to associate with the time component.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided connection is null.</exception>
    public NetTime(NetConnection conn)
    {
        this.conn = conn ?? throw new ArgumentNullException(nameof(conn));
        this.timer = new();
    }

    /// <summary>
    /// Starts the time component.
    /// </summary>
    public void Start()
    {
        offsetTicks = 0;
    }

    /// <summary>
    /// Stops the time component.
    /// </summary>
    public void Stop()
    {
        timer.Stop();
        timer.Reset();

        offsetTicks = 0;
    }

    /// <summary>
    /// Determines whether the time component should write data.
    /// </summary>
    /// <returns>
    /// True if the time component is associated with a server and the internal timer is not running, otherwise false.
    /// </returns>
    public bool ShouldWrite()
    {
        if (conn.IsServer)
        {
            if (!timer.IsRunning)
            {
                timer.Restart();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Writes the current network time to the provided byte writer.
    /// </summary>
    /// <param name="writer">The byte writer to which the time data will be written.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided writer is null.</exception>
    public void Write(ByteWriter writer)
    {
        var curTicks = DateTime.UtcNow.Ticks;
        var timerTicks = timer.ElapsedTicks;
        
        writer.WriteByte((byte)NetHeader.Time);
        writer.WriteInt64(curTicks + timerTicks);
    }

    /// <summary>
    /// Reads the offset ticks value from the provided <see cref="ByteReader"/>
    /// and restarts the internal timer.
    /// </summary>
    /// <param name="reader">The <see cref="ByteReader"/> instance from which to read the offset ticks value.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided reader is null.</exception>
    public void Read(ByteReader reader)
    {
        offsetTicks = reader.ReadInt64();
        
        timer.Restart();
    }
}