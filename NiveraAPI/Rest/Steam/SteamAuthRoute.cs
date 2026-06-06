using System.Net.Http;

using NiveraAPI.Logs;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

using NiveraAPI.Rest.Routes;
using NiveraAPI.Rest.Server;

namespace NiveraAPI.Rest.Steam;

/// <summary>
/// Represents a route for handling Steam authentication request callbacks.
/// </summary>
public class SteamAuthRoute : RestRoute
{
    private const string claimedIdPrefix = "claimed_id=https://steamcommunity.com/openid/id/";
    private static LogSink log = LogManager.GetSource("HTTP", "STEAM_AUTH");

    private SteamAuthManager manager;
    
    /// <summary>
    /// Creates a new instance of the <see cref="SteamAuthRoute"/> class.
    /// </summary>
    /// <param name="manager">The route's manager.</param>
    /// <exception cref="ArgumentNullException">Thrown if url is null</exception>
    public SteamAuthRoute(SteamAuthManager manager)
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        
        Methods = [HttpMethod.Get];
    }

    /// <summary>
    /// The URL to which the request is sent.
    /// </summary>
    public override string Url => manager.CallbackRoute;
    
    /// <summary>
    /// The HTTP methods supported by the route.
    /// </summary>
    public override HttpMethod[] Methods { get; }
    
    // Kinda bullshit way of handling it but Steam has not changed this in a hundred years so it should be fine
    
    /// <summary>
    /// Handles incoming HTTP requests for the defined route.
    /// </summary>
    /// <param name="ctx">The context for the incoming request.</param>
    public override void OnRequest(RestServerContext ctx)
    {
        var parts = ctx.Context.Request.RawUrl.Split('?');
        var urlPart = parts.FirstOrDefault(part => part.StartsWith("url="));
        var sessionPart = parts.FirstOrDefault(part => part.StartsWith("session="));
        
        if (string.IsNullOrEmpty(urlPart))
        {
            log.Warn($"Received invalid Steam authentication callback: &1{ctx.Context.Request.RawUrl}&r");
            
            ctx.RespondError("Missing URL parameter!");
            return;
        }

        if (string.IsNullOrEmpty(sessionPart))
        {
            log.Warn($"Received invalid Steam authentication callback: &1{ctx.Context.Request.RawUrl}&r");
            
            ctx.RespondError("Missing session parameter!");
            return;
        }
        
        urlPart = urlPart.Substring(4);
        sessionPart = sessionPart.Substring(8);

        if (!int.TryParse(sessionPart, out var sessionId))
        {
            log.Warn("Received invalid Steam authentication callback: Invalid session ID!");
            
            ctx.RespondError($"Invalid session ID: {sessionPart}");
            return;
        }

        var session = manager.Get(sessionId);

        if (session == null)
        {
            log.Warn("Received invalid Steam authentication callback: Unknown session ID!");
            
            ctx.RespondError($"Unknown session ID: {sessionId}");
            return;
        }
        
        var claimedIdIndex = urlPart.IndexOf("claimed_id=");

        if (claimedIdIndex == -1)
        {
            log.Warn("Received invalid Steam authentication callback: Missing claimed ID!");
            
            ctx.RespondError("Missing claimed ID!");
            return;       
        }

        urlPart = UrlEncode.DecodeUrl(urlPart.Substring(claimedIdIndex));
        
        if (!urlPart.StartsWith(claimedIdPrefix))
        {
            log.Warn("Received invalid Steam authentication callback: Invalid claimed ID!");
            
            ctx.RespondError("Invalid claimed ID!");
            return;
        }

        urlPart = urlPart.Substring(claimedIdPrefix.Length);
        
        var lastNumIndex = urlPart.FindIndex(c => !char.IsNumber(c));
        
        urlPart = urlPart.Substring(0, lastNumIndex);
        
        if (!string.IsNullOrEmpty(session.Message))
            ctx.RespondText(session.Message!.Replace("%ID%", urlPart));
        else
            ctx.RespondText($"Successfully authenticated! ({urlPart})");
        
        manager.Validate(session, urlPart);
    }
}