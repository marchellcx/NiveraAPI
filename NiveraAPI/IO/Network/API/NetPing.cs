using System.Diagnostics;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.API;

/// <summary>
/// A component used to measure round-trip latency between client and server.
/// </summary>
public class NetPing
{
    private volatile Stopwatch sendTimer;
    private volatile Stopwatch watchTimer;

    private long lastPingTicks;
    private long lowestPingTicks;
    private long highestPingTicks;
    private long averagePingTicks;

    /// <summary>
    /// Gets the average round-trip latency, measured in seconds, between the client and server.
    /// </summary>
    public float Average
    {
        get
        {
            if (averagePingTicks == 0)
                return 0f;
            
            return averagePingTicks / (float)TimeSpan.TicksPerSecond;
        }
    }

    /// <summary>
    /// Gets the highest round-trip latency, measured in seconds, between the client and server.
    /// </summary>
    public float Highest
    {
        get
        {
            if (highestPingTicks == 0)
                return 0f;
            
            return highestPingTicks / (float)TimeSpan.TicksPerSecond;
        }
    }

    /// <summary>
    /// Gets the lowest round-trip latency, measured in seconds, between the client and server.
    /// </summary>
    public float Lowest
    {
        get
        {
            if (lowestPingTicks == 0)
                return 0f;
            
            return lowestPingTicks / (float)TimeSpan.TicksPerSecond;       
        }
    }
    
    /// <summary>
    /// Gets the most recent round-trip latency, measured in seconds, between the client and server.
    /// </summary>
    public float Last
    {
        get
        {
            if (lastPingTicks == 0)
                return 0f;
            
            return lastPingTicks / (float)TimeSpan.TicksPerSecond;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the ping timer has elapsed and the ping is considered timed out.
    /// </summary>
    public bool IsTimedOut => watchTimer.IsRunning && watchTimer.ElapsedMilliseconds >= (NetSettings.PING_INT * 2);

    /// <summary>
    /// Creates a new instance of <see cref="NetPing"/>.
    /// </summary>
    public NetPing()
    {
        sendTimer = new();
        watchTimer = new();
    }

    /// <summary>
    /// Starts the ping timer.
    /// </summary>
    public void Start()
    {
        if (NetSettings.PING_INT > 0)
        {
            lastPingTicks = 0;
            lowestPingTicks = 0;
            highestPingTicks = 0;
            averagePingTicks = 0;
            
            sendTimer.Restart();
            watchTimer.Restart();
        }
    }

    /// <summary>
    /// Stops the ping timer and resets it.
    /// </summary>
    public void Stop()
    {
        sendTimer.Stop();
        sendTimer.Reset();
        
        watchTimer.Stop();
        watchTimer.Reset();
    }

    /// <summary>
    /// Determines whether a write operation is needed based on the elapsed time since the ping timer started.
    /// </summary>
    /// <returns>
    /// True if the elapsed time is greater than or equal to the configured ping interval; otherwise, false.
    /// </returns>
    public bool ShouldWrite()
    {
        if (sendTimer.IsRunning)
            return sendTimer.ElapsedMilliseconds >= NetSettings.PING_INT;

        return false;
    }

    /// <summary>
    /// Writes a ping message to the provided byte writer and restarts the internal timer.
    /// </summary>
    /// <param name="writer">The <see cref="ByteWriter"/> used to write the ping data.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/> is null.</exception>
    public void Write(ByteWriter writer)
    {
        writer.WriteByte((byte)NetHeader.Ping);
        writer.WriteInt64(DateTime.UtcNow.Ticks);
        
        sendTimer.Restart();
    }

    /// <summary>
    /// Reads and processes ping data to compute latency metrics such as lowest, highest, and average ping times.
    /// </summary>
    /// <param name="reader">The <see cref="ByteReader"/> instance used to read the ping data from the incoming network stream.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="reader"/> is null.</exception>
    public void Read(ByteReader reader)
    {
        var curTicks = DateTime.UtcNow.Ticks;
        var recvTicks = reader.ReadInt64();
        var ticksDiff = curTicks - recvTicks;
        
        if (ticksDiff < lowestPingTicks || lowestPingTicks == 0)
            lowestPingTicks = ticksDiff;
        
        if (ticksDiff > highestPingTicks)
            highestPingTicks = ticksDiff;
        
        averagePingTicks = (averagePingTicks + ticksDiff) / 2;
    }
    
    /// <summary>
    /// Restarts the ping timer.
    /// </summary>
    public void RestartWatch()
        => watchTimer.Restart();
}