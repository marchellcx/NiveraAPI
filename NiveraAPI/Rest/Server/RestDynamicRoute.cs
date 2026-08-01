using System.Net.Http;
using NiveraAPI.Rest.Routes;

namespace NiveraAPI.Rest.Server;

/// <summary>
/// Represents a dynamic REST route that processes HTTP requests based on a specified URL, supported HTTP methods,
/// and a custom request handling mechanism.
/// </summary>
/// <remarks>
/// The <see cref="RestDynamicRoute"/> class is used to define a custom dynamic route in the REST server.
/// It allows you to define the URL and HTTP methods the route responds to, and specify a delegate to handle
/// the behavior when a request is received at the defined route.
/// </remarks>
public class RestDynamicRoute : RestRoute
{
    /// <summary>
    /// Gets the URL associated with the route.
    /// This property represents the endpoint path where the route logic is applied,
    /// allowing the server to match incoming requests to the appropriate handler.
    /// </summary>
    public override string Url { get; }

    /// <summary>
    /// Gets the collection of HTTP methods supported by the route.
    /// This property defines which HTTP verbs (e.g., GET, POST, PUT, DELETE) the route accepts,
    /// allowing the server to validate requests and correctly route them to their handler logic.
    /// </summary>
    public override HttpMethod[] Methods { get; }

    /// <summary>
    /// Gets the delegate that defines the logic for handling HTTP requests to the current dynamic route.
    /// This property represents the core functionality for processing incoming requests, enabling users
    /// to specify custom behaviors for each route.
    /// </summary>
    public Action<RestServerContext> HandleRequest { get; }
    
    /// <summary>
    /// Creates a new instance of <see cref="RestDynamicRoute"/> with the specified URL, HTTP methods, and request handling logic.
    /// </summary>
    public RestDynamicRoute(string url, HttpMethod[] methods, Action<RestServerContext> handleRequest)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Methods = methods ?? throw new ArgumentNullException(nameof(methods));
        HandleRequest = handleRequest ?? throw new ArgumentNullException(nameof(handleRequest));
    }

    /// <summary>
    /// Handles the HTTP request for the current dynamic route.
    /// Executes the specified request handling logic defined in the <see cref="HandleRequest"/> delegate.
    /// </summary>
    /// <param name="ctx">The <see cref="RestServerContext"/> object that provides context information for the HTTP request.</param>
    public override void OnRequest(RestServerContext ctx)
    {
        HandleRequest(ctx);
    }
}