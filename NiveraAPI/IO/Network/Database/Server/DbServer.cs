using System.Net;
using System.Net.Http;
using NiveraAPI.Rest.Routes;
using NiveraAPI.Rest.Server;
using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.IO.Network.Database.Server;

/// <summary>
/// Represents a service responsible for managing a database server.
/// </summary>
public class DbServer : Service
{
    /// <summary>
    /// Gets or sets the URL of the RESTful API.
    /// </summary>
    public string RestUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// The REST route for the database.
    /// </summary>
    /// <remarks>Optionally register this route to enable RESTful access to the database</remarks>
    public RestRoute Route { get; private set; }
    
    /// <summary>
    /// The HTTP methods allowed for the RESTful API.
    /// </summary>
    public HttpMethod[] RestMethods { get; set; } = Array.Empty<HttpMethod>();
    
    /// <summary>
    /// The services required by this service.
    /// </summary>
    public override Type[] RequiredServices { get; } = [typeof(DbConfig)];

    /// <summary>
    /// The server configuration.
    /// </summary>
    public DbConfig Config { get; private set; }
    
    /// <summary>
    /// The database file.
    /// </summary>
    public DbFile File { get; private set; }

    /// <summary>
    /// Determines whether the specified service collection allows a new service to be added.
    /// </summary>
    /// <param name="collection">The service collection to check.</param>
    /// <returns>
    /// True if the service can be added to the collection; otherwise, false.
    /// </returns>
    public override bool CanBeAdded(IServiceCollection collection)
        => collection is NetServer;

    /// <summary>
    /// Starts the service.
    /// </summary>
    public override void Start()
    {
        base.Start();

        Config = Collection.GetService<DbConfig>();

        if (string.IsNullOrEmpty(Config.Directory))
            throw new Exception("Database directory is not set!");

        Route = new DbRoute(this, RestMethods, RestUrl);

        File = new DbFile(Config.Directory);
        File.ReadAll();

        if (Collection is NetServer server)
            server.ProvidedServices.Add(typeof(DbUser));
        else if (Collection is RestServer restServer)
            restServer.AddRoute(Route);
        else
            throw new Exception("Invalid service collection!");
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        
        if (Collection is NetServer server)
            server.ProvidedServices.Remove(typeof(DbUser));
        else if (Collection is RestServer restServer)
            restServer.RemoveRoute(Route);

        Route = null!;
        Config = null!;

        if (File != null)
        {
            File.SaveAll();
            File.Dispose();
        }

        File = null!;
    }

    internal void HandleRest(RestServerContext ctx)
    {
        ctx.RespondError("Not Implemented", HttpStatusCode.NotImplemented);
    }
}