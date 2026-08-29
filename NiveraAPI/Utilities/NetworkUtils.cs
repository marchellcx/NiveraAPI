using System.Net;

using NiveraAPI.Extensions;

namespace NiveraAPI.Utilities;

/// <summary>
/// Provides utility methods for working with network-related operations such as IP address validation and range checking.
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// Determines whether a given IP address is in the range specified by the provided CIDR notation mask.
    /// </summary>
    /// <param name="ipAddress">The IP address to check, as a string.</param>
    /// <param name="mask">The CIDR notation mask (e.g., "192.168.1.0/24") representing the subnet range.</param>
    /// <returns>True if the IP address is within the specified range; otherwise, false.</returns>
    public static bool IsInRange(string ipAddress, string mask)
    {
        try
        {
            if (!mask.TrySplit('/', true, 2, out var segments))
                return false;

            if (!IPAddress.TryParse(ipAddress, out var address))
                return false;
            
            if (!IPAddress.TryParse(segments[0], out var maskAddress))
                return false;

            if (!int.TryParse(segments[1], out var maskBits))
                return false;
            
            var addressBytes = address.GetAddressBytes();
            var maskBytes = maskAddress.GetAddressBytes();
            
            var num = BitConverter.ToInt32(addressBytes, 0);
            var num2 = BitConverter.ToInt32(maskBytes, 0);
            var num3 = IPAddress.HostToNetworkOrder(-1 << 32 - maskBits);
            
            return (num & num3) == (num2 & num3);
        }
        catch
        {
            return false;
        }
    }
}