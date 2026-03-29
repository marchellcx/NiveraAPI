using System.Collections.Concurrent;
using System.Net.Http;

using NiveraAPI.Rest.Server;

namespace NiveraAPI.Rest.Routes
{
    /// <summary>
    /// Represents an abstract base class that defines a route for handling HTTP requests.
    /// </summary>
    /// <remarks>
    /// The <see cref="RestRoute"/> class serves as a foundational structure for defining custom HTTP routes.
    /// It provides properties and methods to define supported HTTP methods, the route URL, and the logic
    /// that will handle incoming requests.
    /// </remarks>
    public abstract class RestRoute
    {
        /// <summary>
        /// Represents an abstract base class that defines a route for handling HTTP requests.
        /// </summary>
        /// <remarks>
        /// This class provides the essential structure for creating custom HTTP routes.
        /// It includes methods and properties to specify the behaviors and configurations
        /// required for handling HTTP requests, such as supported HTTP methods and URL patterns.
        /// </remarks>
        protected RestRoute()
        {
        }
        
        internal string FixedUrl;

        /// <summary>
        /// Gets the collection of HTTP methods supported by the route.
        /// </summary>
        /// <remarks>
        /// This property defines the HTTP methods (e.g., GET, POST, PUT, DELETE) supported by the route.
        /// It allows you to specify which methods the route will handle during HTTP request processing.
        /// </remarks>
        public abstract HttpMethod[] Methods { get; }

        /// <summary>
        /// Gets the URL pattern for the HTTP route.
        /// </summary>
        /// <remarks>
        /// This property defines the URL structure associated with the route.
        /// The URL is used to match incoming HTTP requests to this route during request processing.
        /// Custom implementation can specify dynamic or static URL patterns based on requirements.
        /// </remarks>
        public abstract string Url { get; }

        /// <summary>
        /// Handles incoming HTTP requests for the defined route.
        /// </summary>
        /// <remarks>
        /// This method is invoked when an HTTP request matches the route defined by the derived class.
        /// It processes the request based on the implementation provided in the subclass.
        /// </remarks>
        /// <param name="ctx">The <see cref="RestServerContext"/> instance containing contextual information about the HTTP request.</param>
        public abstract void OnRequest(RestServerContext ctx);

        /// <summary>
        /// Determines whether the provided URLs match the route's configuration.
        /// </summary>
        /// <param name="rawUrl">The raw URL received in the HTTP request.</param>
        /// <param name="parsedUrl">The processed or normalized version of the URL.</param>
        /// <returns>
        /// A boolean indicating whether the specified URLs correspond to the route's pattern
        /// and meet the matching criteria.
        /// </returns>
        public virtual bool IsMatch(string rawUrl, string parsedUrl) => false;

        /// <summary>
        /// Extracts and parses parameter values from the provided URLs and stores them in the given dictionary.
        /// </summary>
        /// <param name="rawUrl">The raw URL received in the HTTP request.</param>
        /// <param name="parsedUrl">The processed or normalized version of the URL.</param>
        /// <param name="parameters">
        /// A thread-safe dictionary in which the method stores the extracted parameter names and values.
        /// </param>
        public virtual void ParseParameters(string rawUrl, string parsedUrl,
            ConcurrentDictionary<string, string> parameters)
        {
        }
    }
}