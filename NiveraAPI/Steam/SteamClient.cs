using System.Net.Http;
using System.Xml.Linq;

using NiveraAPI.Logs;

namespace NiveraAPI.Steam;

/// <summary>
/// Represents a client for interacting with the Steam API.
/// </summary>
public class SteamClient
{
    private static volatile LogSink log = LogManager.GetSource("Steam", "Client");
    private static volatile HttpClient client = new();

    /// <summary>
    /// Retrieves detailed profile information for a given Steam user based on their 64-bit Steam ID.
    /// </summary>
    /// <param name="steamId64">The 64-bit Steam ID of the user whose profile information is to be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="SteamInfo"/> object
    /// with the profile details, or null if the profile cannot be retrieved.
    /// </returns>
    public static async Task<SteamInfo?> GetProfileInfoAsync(string steamId64)
    {
        try
        {
            log.Debug($"Requesting profile info for Steam ID: &3{steamId64}&r");
            
            var url = $"https://steamcommunity.com/profiles/{steamId64}/?xml=1";
            var xml = await client.GetStringAsync(url);

            var doc = XDocument.Parse(xml);

            if (doc?.Root == null)
            {
                log.Debug($"Returned document root is null");
                return null;
            }

            var steamIdElement = doc.Root.Element("steamID64");

            var onlineStateElement = doc.Root.Element("onlineState");
            var onlineStateMessageElement = doc.Root.Element("stateMessage");

            var privacyStateElement = doc.Root.Element("privacyState");

            var avatarUrlElement = doc.Root.Element("avatarIcon");
            var avatarFullUrlElement = doc.Root.Element("avatarFull");
            var avatarMediumUrlElement = doc.Root.Element("avatarMedium");

            var vacBannedElement = doc.Root.Element("vacBanned");
            var tradeBanStateElement = doc.Root.Element("tradeBanState");
            var isLimitedAccountElement = doc.Root.Element("isLimitedAccount");
            var hoursPlayedTwoWeeksElement = doc.Root.Element("hoursPlayed2Wk");
            var locationElement = doc.Root.Element("location");
            var nameElement = doc.Root.Element("realname");

            if (steamIdElement == null)
            {
                log.Debug($"Returned steamID64 element is null");
                return null;
            }

            if (!ulong.TryParse(steamIdElement.Value, out var steamId))
            {
                log.Debug($"Could not parse steamID64 element value: &3{steamIdElement.Value}&r");
                return null;
            }

            var profile = new SteamInfo();

            Volatile.Write(ref profile.SteamId, steamId);

            profile.Name = nameElement?.Value ?? string.Empty;
            profile.Location = locationElement?.Value ?? string.Empty;

            profile.AvatarUrl = avatarUrlElement?.Value ?? string.Empty;
            profile.AvatarFullUrl = avatarFullUrlElement?.Value ?? string.Empty;
            profile.AvatarMediumUrl = avatarMediumUrlElement?.Value ?? string.Empty;

            if (onlineStateElement != null && !string.IsNullOrEmpty(onlineStateElement.Value))
            {
                if (string.Equals(onlineStateElement.Value, "offline", StringComparison.OrdinalIgnoreCase))
                {
                    profile.OnlineState = SteamOnlineState.Offline;
                }
                else if (string.Equals(onlineStateElement.Value, "online", StringComparison.OrdinalIgnoreCase))
                {
                    profile.OnlineState = SteamOnlineState.Online;
                }
                else if (string.Equals(onlineStateElement.Value, "in-game", StringComparison.OrdinalIgnoreCase))
                {
                    profile.OnlineState = SteamOnlineState.InGame;
                }
                else
                {
                    log.Debug($"Unknown online state: &3{onlineStateElement.Value}&r");
                }

                if (onlineStateMessageElement != null && !string.IsNullOrEmpty(onlineStateMessageElement.Value))
                {
                    if (onlineStateMessageElement.Value.StartsWith("In Game:"))
                    {
                        profile.CurrentGame = onlineStateElement.Value.Replace("In Game: ", string.Empty);
                    }
                }
            }
            else
            {
                log.Debug("Online state element is null or empty.");
            }

            if (privacyStateElement != null && !string.IsNullOrEmpty(privacyStateElement.Value))
            {
                if (string.Equals(privacyStateElement.Value, "private", StringComparison.OrdinalIgnoreCase))
                {
                    profile.PrivacyState = SteamPrivacyState.Private;
                }
                else if (string.Equals(privacyStateElement.Value, "friendsonly", StringComparison.OrdinalIgnoreCase))
                {
                    profile.PrivacyState = SteamPrivacyState.FriendsOnly;
                }
                else if (string.Equals(privacyStateElement.Value, "public", StringComparison.OrdinalIgnoreCase))
                {
                    profile.PrivacyState = SteamPrivacyState.Public;
                }
                else
                {
                    log.Debug($"Unknown privacy state: &3{privacyStateElement.Value}&r");
                }
            }
            else
            {
                log.Debug("Privacy state element is null or empty.");
            }

            if (tradeBanStateElement != null && !string.IsNullOrEmpty(tradeBanStateElement.Value))
            {
                if (string.Equals(tradeBanStateElement.Value, "none", StringComparison.OrdinalIgnoreCase))
                {
                    profile.TradeBanState = SteamTradeBanState.None;
                }
                else if (string.Equals(tradeBanStateElement.Value, "probation", StringComparison.OrdinalIgnoreCase))
                {
                    profile.TradeBanState = SteamTradeBanState.Probation;
                }
                else if (string.Equals(tradeBanStateElement.Value, "banned", StringComparison.OrdinalIgnoreCase))
                {
                    profile.TradeBanState = SteamTradeBanState.Banned;
                }
                else
                {
                    log.Debug($"Unknown trade ban state: &3{tradeBanStateElement.Value}&r");
                }
            }
            else
            {
                log.Debug("Trade ban state element is null or empty.");
            }

            if (vacBannedElement != null && !string.IsNullOrEmpty(vacBannedElement.Value))
            {
                if (int.TryParse(vacBannedElement.Value, out var vacBanned))
                {
                    profile.IsVacBanned = vacBanned == 1;
                }
                else
                {
                    log.Debug($"Could not parse vacBanned element value: &3{vacBannedElement.Value}&r");
                }
            }
            else
            {
                log.Debug("Vac banned element is null or empty.");
            }

            if (isLimitedAccountElement != null && !string.IsNullOrEmpty(isLimitedAccountElement.Value))
            {
                if (int.TryParse(isLimitedAccountElement.Value, out var isLimitedAccount))
                {
                    profile.IsLimitedAccount = isLimitedAccount == 1;
                }
                else
                {
                    log.Debug($"Could not parse isLimitedAccount element value: &3{isLimitedAccountElement.Value}&r");
                }
            }
            else
            {
                log.Debug("Is limited account element is null or empty.");
            }

            if (hoursPlayedTwoWeeksElement != null && !string.IsNullOrEmpty(hoursPlayedTwoWeeksElement.Value))
            {
                if (double.TryParse(hoursPlayedTwoWeeksElement.Value, out var hoursPlayedTwoWeeks))
                {
                    Volatile.Write(ref profile.HoursPlayedTwoWeeks, hoursPlayedTwoWeeks);
                }
                else
                {
                    log.Debug($"Could not parse hoursPlayedTwoWeeks element value: &3{hoursPlayedTwoWeeksElement.Value}&r");
                }
            }
            else
            {
                log.Debug("Hours played two weeks element is null or empty.");
            }

            return profile;
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }
        
        return null;
    }
    
    /// <summary>
    /// Retrieves the avatar URL for a given Steam user based on their 64-bit Steam ID.
    /// </summary>
    /// <param name="steamId64">The 64-bit Steam ID of the user whose avatar URL is to be retrieved.</param>
    /// <param name="size">
    /// The desired size of the avatar image. Valid values are "small" (or "icon"), "medium", or "full".
    /// Defaults to "full" if not specified.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the avatar URL as a string,
    /// or null if the avatar cannot be retrieved.
    /// </returns>
    public static async Task<string?> GetAvatarUrlAsync(string steamId64, string size = "full")
    {
        var url = $"https://steamcommunity.com/profiles/{steamId64}/?xml=1";
        var xml = await client.GetStringAsync(url);

        var doc = XDocument.Parse(xml);

        string elementName = size.ToLower() switch
        {
            "small" or "icon" => "avatarIcon",
            "medium"          => "avatarMedium",
            _                 => "avatarFull"
        };

        return doc.Root?.Element(elementName)?.Value;
    }
}