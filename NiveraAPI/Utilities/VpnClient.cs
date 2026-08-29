using System.Net.Http;

using Newtonsoft.Json;

namespace NiveraAPI.Utilities;

/// <summary>
/// Provides functionality to interact with a VPN API for retrieving
/// information about VPN-related activity associated with specific IP addresses.
/// </summary>
public class VpnClient
{
    private static volatile HttpClient client = new();
    
    /// <summary>
    /// Base URL for the VPN API.
    /// </summary>
    public const string BaseUrl = "http://v2.api.iphub.info/ip";
    
    /// <summary>
    /// Represents the response from a VPN client.
    /// </summary>
    public class VpnResponse
    {
        /// <summary>
        /// Represents the IP address associated with the VPN response.
        /// This property is populated from the JSON response using the "ip" field.
        /// </summary>
        [JsonProperty("ip")]
        public volatile string Ip = string.Empty;

        /// <summary>
        /// Represents the country code associated with the VPN response.
        /// This property is populated from the JSON response using the "countryCode" field.
        /// </summary>
        [JsonProperty("countryCode")]
        public volatile string CountryCode = string.Empty;

        /// <summary>
        /// Represents the name of the country associated with the VPN response.
        /// This property is populated from the JSON response using the "countryName" field.
        /// </summary>
        [JsonProperty("countryName")] 
        public volatile string CountryName = string.Empty;

        /// <summary>
        /// Represents the Internet Service Provider (ISP) associated with the VPN response.
        /// This property is populated from the JSON response using the "isp" field.
        /// </summary>
        [JsonProperty("isp")] 
        public volatile string Provider = string.Empty;

        /// <summary>
        /// Represents the Autonomous System Number (ASN) associated with the VPN response.
        /// This property is populated from the JSON response using the "asn" field.
        /// </summary>
        [JsonProperty("asn")] 
        public volatile int Asn = -1;

        /// <summary>
        /// Represents the block level associated with the VPN response.
        /// This property is populated from the JSON response using the "block" field.
        /// </summary>
        [JsonProperty("block")] 
        public volatile int BlockLevel = -1;
    }

    /// <summary>
    /// Retrieves VPN information for a specific IP address from a remote API.
    /// </summary>
    /// <param name="ip">
    /// The IP address for which VPN information is to be fetched.
    /// This parameter cannot be null or empty.
    /// </param>
    /// <param name="apiKey">
    /// The API key used to authenticate the request to the VPN API.
    /// This parameter cannot be null or empty.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a <see cref="VpnResponse"/> object with information
    /// about the VPN, or null if deserialization fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="ip"/> or <paramref name="apiKey"/> is null or empty.
    /// </exception>
    public static async Task<VpnResponse?> GetVpnInfoAsync(string ip, string apiKey)
    {
        if (string.IsNullOrEmpty(ip))
            throw new ArgumentNullException(nameof(ip));

        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{ip}");
        
        request.Headers.Add("X-Key", apiKey);
        
        using var response = await client.SendAsync(request);
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<VpnResponse>(content);
    }
}