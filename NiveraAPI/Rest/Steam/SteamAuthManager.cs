using System.Diagnostics;
using NiveraAPI.Rest.Server;

using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;

using NiveraAPI.Utilities;

namespace NiveraAPI.Rest.Steam;

/// <summary>
/// Manages Steam authentication sessions, including session creation, cancellation, and lifecycle management.
/// </summary>
public class SteamAuthManager : Service
{
    private const string steamOpenIdUrl = "https://steamcommunity.com/openid/loginform/?goto=%2Fopenid%2Flogin%3Fopenid.identity%3Dhttp%253A%252F%252Fspecs.openid.net%252Fauth%252F2.0%252Fidentifier_select%26openid.claimed_id%3Dhttp%253A%252F%252Fspecs.openid.net%252Fauth%252F2.0%252Fidentifier_select%26openid.ns%3Dhttp%253A%252F%252Fspecs.openid.net%252Fauth%252F2.0%26openid.mode%3Dcheckid_setup%26openid.return_to%3D";
    
    internal RestServer server;
    
    private int id = 0;
    private List<SteamAuthSession> sessions = new();

    private Stopwatch watch;
    private SteamAuthRoute route;

    /// <summary>
    /// The amount of time in milliseconds that a session will be valid for - defaults to 10 seconds.
    /// </summary>
    public int SessionLife { get; set; } = 10000;
    
    /// <summary>
    /// The amount of active sessions.
    /// </summary>
    public int SessionCount => sessions.Count;
    
    /// <summary>
    /// The amount of time in milliseconds between session checks.
    /// </summary>
    public int SessionCheckDelay { get; set; } = 1000;
    
    /// <summary>
    /// The route that will be used for Steam callback requests.
    /// </summary>
    public string CallbackRoute { get; set; } = "/steamcallback";

    /// <summary>
    /// Determines whether the SteamAuthManager service can be added to the given service collection.
    /// </summary>
    /// <param name="collection">The service collection to which the service is being added.</param>
    /// <returns>True if the service can be added; otherwise, false.</returns>
    public override bool CanBeAdded(IServiceCollection collection)
        => collection is RestServer;

    /// <summary>
    /// Creates a new Steam authentication session and initializes its properties.
    /// </summary>
    /// <param name="callback">The callback function to be executed when the session is complete.</param>
    /// <param name="data">Optional data to associate with the session.</param>
    /// <param name="message">An optional message to associate with the session.</param>
    /// <returns>The newly created <see cref="SteamAuthSession"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="callback"/> parameter is null.</exception>
    public SteamAuthSession Create(Action<SteamAuthSession> callback, object? data = null, string? message = null)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        var session = new SteamAuthSession();
        
        session.Id = id++;

        session.Data = data;
        session.Message = message;
        session.Callback = callback;

        session.ExpiresAtUtc = SessionLife > 0 ? session.CreatedAtUtc.AddMilliseconds(SessionLife) : null;

        var callbackUrl = string.Concat(server.Prefix.TrimEnd('/'), CallbackRoute, "?session=", session.Id, "?url=");

        session.Url = string.Concat(steamOpenIdUrl, UrlEncode.EncodeUrl(callbackUrl));
        
        sessions.Add(session);
        return session;
    }

    /// <summary>
    /// Cancels the specified Steam authentication session and removes it from the session list.
    /// </summary>
    /// <param name="session">The Steam authentication session to be canceled.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided session is null.</exception>
    public void Cancel(SteamAuthSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        if (session.Error == SteamAuthError.None)
        {
            session.Invalidate(SteamAuthError.Cancelled);
            sessions.Remove(session);
        }
    }

    /// <summary>
    /// Retrieves a Steam authentication session by its unique identifier if it exists and has no associated errors.
    /// </summary>
    /// <param name="id">The unique identifier of the Steam authentication session to retrieve.</param>
    /// <returns>
    /// The matching <see cref="SteamAuthSession"/> if found and without errors; otherwise, null.
    /// </returns>
    public SteamAuthSession? Get(int id)
        => sessions.Find(s => s.Id == id && s.Error == SteamAuthError.None);

    /// <summary>
    /// Starts the SteamAuthManager service.
    /// </summary>
    public override void Start()
    {
        base.Start();

        if (Collection is not RestServer)
            throw new InvalidOperationException("SteamAuthManager must be added to a RestServer");

        id = 0;
        
        sessions.ForEach(s => s.Invalidate(SteamAuthError.ServerClosed));
        sessions.Clear();
        
        route = new(this);

        server = (RestServer)Collection;
        server.AddRoute(route);
        
        watch = Stopwatch.StartNew();
        
        LibraryUpdate.Register(Update);
    }

    /// <summary>
    /// Stops the SteamAuthManager service.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        
        watch.Stop();
        watch.Reset();
        
        LibraryUpdate.Unregister(Update);

        id = 0;
        
        sessions.ForEach(s => s.Invalidate(SteamAuthError.ServerClosed));
        sessions.Clear();
        
        server?.RemoveRoute(route);
    }

    internal void Validate(SteamAuthSession session, string id)
    {
        session.Validate(id);
        
        sessions.Remove(session);
    }

    private void Update()
    {
        if (SessionCheckDelay > 0 && watch.ElapsedMilliseconds < SessionCheckDelay)
            return;
        
        watch.Restart();

        sessions.RemoveAll(s => s.ShouldRemove());
    }

    /// <summary>
    /// Creates a new SteamAuthSession instance for handling Steam authentication on a dedicated server
    /// and returns the session with an optional callback for session completion.
    /// </summary>
    /// <param name="ipOrDomain">The IP address or domain name of the server to host the authentication process.</param>
    /// <param name="callback">The action to be invoked when the session is completed or cancelled.</param>
    /// <param name="sessionLife">The lifespan, in milliseconds, of the session before it expires. Defaults to 10000.</param>
    /// <param name="sessionLifeCheckDelay">The delay, in milliseconds, between session checks. Defaults to 1000.</param>
    /// <param name="data">Optional user-defined data associated with the session.</param>
    /// <param name="message">An optional message to include in the session data.</param>
    /// <returns>A new instance of <see cref="SteamAuthSession"/> representing the active authentication session.</returns>
    public static SteamAuthSession AuthOnce(string ipOrDomain, Action<SteamAuthSession> callback,
        int sessionLife = 10000, int sessionLifeCheckDelay = 1000,
        object? data = null, string? message = null)
    {
        var server = new RestServer(ipOrDomain);
        var manager = new SteamAuthManager();

        server.Start();
        server.AddService(manager);
        
        manager.SessionLife = sessionLife;
        manager.SessionCheckDelay = sessionLifeCheckDelay;

        void Callback(SteamAuthSession s)
        {
            callback(s);
            
            try { server.Stop(); } catch { /* ignored */ }
            try { server.Dispose(); } catch { /* ignored */ }
        }
        
        return manager.Create(Callback, data, message);
    }
}