using System.Net.Http;

using NiveraAPI.Rest.Routes;
using NiveraAPI.Rest.Server;

namespace NiveraAPI.Networking.Database.Server;

/// <summary>
/// Represents a route in a RESTful API for databases.
/// </summary>
public class DbRoute : RestRoute
{
    /// <summary>
    /// The database server associated with the route.
    /// </summary>
    public DbServer Server { get; }
    
    /// <summary>
    /// Gets the URL pattern for the HTTP route.
    /// </summary>
    /// <remarks>
    /// This property defines the URL structure associated with the route.
    /// The URL is used to match incoming HTTP requests to this route during request processing.
    /// Custom implementation can specify dynamic or static URL patterns based on requirements.
    /// </remarks>
    public override string Url { get; }
    
    /// <summary>
    /// Gets the collection of HTTP methods supported by the route.
    /// </summary>
    /// <remarks>
    /// This property defines the HTTP methods (e.g., GET, POST, PUT, DELETE) supported by the route.
    /// It allows you to specify which methods the route will handle during HTTP request processing.
    /// </remarks>
    public override HttpMethod[] Methods { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DbRoute"/> class.
    /// </summary>
    /// <param name="server">The database server associated with the route.</param>
    /// <param name="methods">The HTTP methods supported by the route.</param>
    /// <param name="url">The URL pattern for the route.</param>
    public DbRoute(DbServer server, HttpMethod[] methods, string url)
    {
        Url = url;
        Server = server;
        Methods = methods;
    }

    /// <summary>
    /// Handles incoming HTTP requests for the defined route.
    /// </summary>
    /// <remarks>
    /// This method is invoked when an HTTP request matches the route defined by the derived class.
    /// It processes the request based on the implementation provided in the subclass.
    /// </remarks>
    public override void OnRequest(RestServerContext ctx)
    {
        Server.HandleRest(ctx);
    }
}