namespace NiveraAPI.IO.Network;

/// <summary>
/// Network settings.
/// </summary>
public static class NetSettings
{
    /// <summary>
    /// Maximum Transmission Unit (MTU) of the network.
    /// </summary>
    public static volatile int MTU = 65535;

    /// <summary>
    /// Ping interval in milliseconds.
    /// </summary>
    public static volatile int PING_INT = 1000;
}