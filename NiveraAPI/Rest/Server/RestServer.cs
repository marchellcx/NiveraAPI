using System.Collections.Concurrent;

using System.Net;
using System.Reflection;

using NiveraAPI.Rest.Routes;

using NiveraAPI.Logs;
using NiveraAPI.Services;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

namespace NiveraAPI.Rest.Server
{
	/// <summary>
	/// Represents an HTTP server that can manage and serve HTTP and HTTPS routes while enabling synchronized event handling.
	/// </summary>
	/// <remarks>
	/// This server provides functionalities to add, remove, and manage HTTP/HTTPS routes,
	/// configure URL prefixes, and handle client requests. It integrates with synchronized
	/// event handling through the <c>SyncedEventManager</c>.
	/// </remarks>
	public class RestServer : ServiceCollection
	{
		private volatile int id;

		private volatile bool http;
		private volatile bool https;

		private volatile string prefix;
		private volatile string realIpHeader = "X-Real-IP";

		private static Action queueUpdate;
		
		private volatile LogSink log;
		private volatile HttpListener listener;

		private volatile ActionQueue queue = new();
		private volatile ConcurrentDictionary<int, RestRoute> routes = new();

		/// <summary>
		/// Gets the HTTP listener instance used to receive and process HTTP requests.
		/// </summary>
		/// <remarks>
		/// The <c>Listener</c> property provides access to the underlying <see cref="HttpListener"/>
		/// object, enabling the server to listen for incoming connections and process HTTP requests.
		/// This property is primarily utilized internally to manage the HTTP server's lifecycle
		/// and handle requests, but it can also be accessed for advanced configuration or
		/// inspection purposes.
		/// </remarks>
		public HttpListener Listener => listener;

		/// <summary>
		/// Provides access to the collection of HTTP routes managed by the server.
		/// </summary>
		/// <remarks>
		/// The <c>Routes</c> property represents a read-only dictionary of HTTP routes,
		/// keyed by their unique integer identifiers. It is used internally to store
		/// and manage the registered <see cref="RestRoute"/> instances. These routes
		/// handle incoming HTTP requests based on their URL patterns and methods.
		/// </remarks>
		public IReadOnlyDictionary<int, RestRoute> Routes => routes;

		/// <summary>
		/// Gets or sets a value indicating whether the HTTP server should listen for HTTP traffic.
		/// </summary>
		/// <remarks>
		/// When set to <c>true</c>, the server is configured to handle incoming HTTP requests.
		/// This property must be enabled before starting the server if HTTP traffic is required.
		/// If both HTTP and HTTPS are disabled, an exception will be thrown upon initialization.
		/// </remarks>
		public bool EnableHttp
		{
			get => http;
			set => http = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the HTTP server should listen for HTTPS traffic.
		/// </summary>
		/// <remarks>
		/// When set to <c>true</c>, the server is configured to handle incoming HTTPS requests.
		/// This property must be enabled before starting the server if HTTPS traffic is required.
		/// If both HTTP and HTTPS are disabled, an exception will be thrown upon initialization.
		/// </remarks>
		public bool EnableHttps
		{
			get => https;
			set => https = value;
		}

		/// <summary>
		/// Gets the HTTP URL prefix used by the server for handling HTTP requests.
		/// </summary>
		/// <remarks>
		/// The <c>HttpPrefix</c> property generates the full HTTP URL prefix by combining the "http://" scheme
		/// with the configured <c>Prefix</c> value, followed by a trailing slash ("/").
		/// This property is primarily used internally to construct the base address for incoming HTTP connections,
		/// ensuring that all requests are routed to the correct handler. The prefix is dynamically constructed
		/// to reflect the current configuration of the server.
		/// </remarks>
		public string HttpPrefix => string.Concat("http://", prefix, "/");

		/// <summary>
		/// Gets the HTTPS URL prefix used by the server to listen for secure HTTPS connections.
		/// </summary>
		/// <remarks>
		/// The <c>HttpsPrefix</c> property constructs and returns the full URL prefix for HTTPS connections
		/// based on the server's configured <c>Prefix</c>. This is primarily used internally by the server
		/// when enabling HTTPS functionality to define the base address for incoming secure requests.
		/// </remarks>
		public string HttpsPrefix => string.Concat("https://", prefix, "/");

		/// <summary>
		/// Indicates whether the HTTP server is currently active and listening for incoming requests.
		/// </summary>
		/// <remarks>
		/// The <c>IsListening</c> property is a read-only boolean that reflects the state of the underlying
		/// <see cref="HttpListener"/> instance. It returns <see langword="true"/> if the <see cref="HttpListener"/> is initialized
		/// and actively listening for incoming connections; otherwise, it returns <see langword="false"/>.
		/// </remarks>
		public bool IsListening => listener != null && listener.IsListening;

		/// <summary>
		/// Gets or sets the base prefix for the HTTP and HTTPS server URLs.
		/// </summary>
		/// <remarks>
		/// The <c>Prefix</c> property specifies the root URL path that the server will use to construct
		/// the <see cref="HttpPrefix"/> and <see cref="HttpsPrefix"/> properties, which define the full
		/// URL prefixes for HTTP and HTTPS, respectively. The value is automatically sanitized by removing
		/// protocol declarations (e.g., "http://", "https://") and trailing slashes.
		/// Setting this property to a null or empty value will throw an <see cref="ArgumentNullException"/>.
		/// </remarks>
		public string Prefix
		{
			get => prefix;
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException(nameof(value));

				value = value.Replace("http://", "")
					.Replace("https://", "");

				if (value.EndsWith("/"))
					value = value.Substring(0, value.Length - 1);

				prefix = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the HTTP request header used to determine the client's real IP address.
		/// </summary>
		/// <remarks>
		/// The <c>RealIpHeader</c> property specifies the name of the HTTP header
		/// that provides the actual IP address of the client, especially when the server
		/// is operating behind a reverse proxy or load balancer. This header is often
		/// added by such intermediaries to communicate the original client's IP address
		/// to the server.
		/// If the property is not set or is null/empty, the server falls back to using
		/// the <see cref="HttpListenerContext.Request.RemoteEndPoint"/> to determine
		/// the client's IP address. This property ensures more accurate logging and
		/// IP-based processing in scenarios involving intermediaries.
		/// Assigning a null value to this property will throw an <see cref="ArgumentNullException"/>.
		/// </remarks>
		public string RealIpHeader
		{
			get => realIpHeader;
			set => realIpHeader = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Represents an HTTP server that can manage and serve HTTP/HTTPS routes and handle synchronized events.
		/// </summary>
		public RestServer(string prefix, bool listenHttp, bool listenHttps)
		{
			if (!listenHttp && !listenHttps)
				throw new("You must listen on HTTP or HTTPS");

			log = LogManager.GetSource("HTTP", "Server");

			Prefix = prefix;

			EnableHttp = listenHttp;
			EnableHttps = listenHttps;
		}

		/// <summary>
		/// Adds a route to the HTTP server based on a generic type of <see cref="RestRoute"/>.
		/// </summary>
		/// <typeparam name="T">The type of the route to add, which must inherit from <see cref="RestRoute"/> and have a parameterless constructor.</typeparam>
		/// <returns>The unique identifier of the added route.</returns>
		public int AddRoute<T>() where T : RestRoute, new()
			=> AddRoute(Activator.CreateInstance<T>());

		/// <summary>
		/// Adds a specified route to the server.
		/// </summary>
		/// <param name="routeType">
		/// The type of the route to be added. It must be a class derived from <see cref="RestRoute"/>.
		/// </param>
		/// <returns>
		/// A unique identifier for the newly added route.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the <paramref name="routeType"/> parameter is null.
		/// </exception>
		/// <exception cref="Exception">
		/// Thrown when the specified <paramref name="routeType"/> is not assignable from <see cref="RestRoute"/>.
		/// </exception>
		public int AddRoute(Type routeType)
		{
			if (routeType == null)
				throw new ArgumentNullException("routeType");

			if (!typeof(RestRoute).IsAssignableFrom(routeType))
				throw new Exception(string.Concat("Route type ", routeType.FullName, " is not a RestRoute"));

			return AddRoute(Activator.CreateInstance(routeType) as RestRoute);
		}

		/// <summary>
		/// Adds a new HTTP route to the server and assigns it a unique identifier.
		/// </summary>
		/// <param name="route">The HTTP route to be added. This cannot be null and must have a valid URL.</param>
		/// <returns>The unique identifier assigned to the added route.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="route"/> is null or its URL is not provided.</exception>
		/// <exception cref="Exception">Thrown when another route with the same URL is already registered.</exception>
		public int AddRoute(RestRoute route)
		{
			if (route == null)
				throw new ArgumentNullException("route");

			if (string.IsNullOrWhiteSpace(route.Url))
				throw new ArgumentNullException("Url");

			route.FixedUrl = route.Url;

			foreach (var keyValuePair in routes)
			{
				if (keyValuePair.Value.FixedUrl != route.FixedUrl)
					continue;

				throw new(string.Concat("Another route with the same URL has already been registered (", route.Url, " / ", route.FixedUrl, ")"));
			}

			id++;

			routes.TryAdd(id, route);
			return id;
		}

		/// <summary>
		/// Adds all HTTP routes defined in the specified assembly to the server's route collection.
		/// </summary>
		/// <param name="assembly">The assembly containing types that inherit from <see cref="RestRoute"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="assembly"/> is null.</exception>
		public void AddRoutes(Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			var types = assembly.GetTypes();

			for (var i = 0; i < types.Length; i++)
			{
				var type = types[i];

				if (typeof(RestRoute).IsAssignableFrom(type)
					&& Activator.CreateInstance(type) is RestRoute httpRoute)
				{
					try
					{
						AddRoute(httpRoute);
					}
					catch (Exception ex)
					{
						log.Error(ex);
					}
				}
			}
		}

		/// <summary>
		/// Removes all registered HTTP routes from the server.
		/// </summary>
		/// <remarks>
		/// This method clears all the routes currently associated with the server,
		/// effectively resetting its routing table. It is typically used when you need
		/// to reconfigure the server with a fresh set of routes.
		/// </remarks>
		public void RemoveAllRoutes()
		{
			routes.Clear();
		}

		/// <summary>
		/// Removes the specified HTTP route from the server's active route collection.
		/// </summary>
		/// <param name="route">The <see cref="RestRoute"/> instance to be removed.</param>
		/// <returns>A boolean indicating whether the route was successfully removed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="route"/> or its required properties are null or empty.</exception>
		/// <exception cref="Exception">Thrown when attempting to remove an unregistered route.</exception>
		public bool RemoveRoute(RestRoute route)
		{
			if (route == null)
				throw new ArgumentNullException("route");

			if (string.IsNullOrWhiteSpace(route.Url))
				throw new ArgumentNullException("Url");

			if (string.IsNullOrWhiteSpace(route.FixedUrl))
				throw new("Attempted to remove an unregistered route");

			var flag = false;

			foreach (var keyValuePair in routes)
			{
				if (keyValuePair.Value.FixedUrl != route.FixedUrl)
					continue;

				flag |= routes.TryRemove(keyValuePair.Key, out _);
			}

			route.FixedUrl = null!;
			return flag;
		}

		/// <summary>
		/// Removes all routes of the specified type from the server's route collection.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the route to be removed. Must inherit from <see cref="RestRoute"/> and have a parameterless constructor.
		/// </typeparam>
		/// <returns>
		/// The number of routes successfully removed.
		/// </returns>
		public int RemoveRoutes<T>() where T : RestRoute, new()
			=> RemoveRoutes(typeof(T));

		/// <summary>
		/// Removes all HTTP routes associated with the provided assembly and returns the count of removed routes.
		/// </summary>
		/// <param name="assembly">The assembly containing the routes to be removed.</param>
		/// <returns>The number of routes removed from the HTTP server.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided assembly is null.</exception>
		public int RemoveRoutes(Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			var num = 0;

			foreach (var route in routes)
			{
				if (!(route.Value.GetType().Assembly == assembly)
				    || !routes.TryRemove(route.Key, out _))
					continue;

				num++;
			}

			return num;
		}

		/// <summary>
		/// Removes all routes of a specified type from the HTTP server.
		/// </summary>
		/// <param name="routeType">The type of the routes to be removed.</param>
		/// <returns>The number of routes that were successfully removed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="routeType"/> is null.</exception>
		public int RemoveRoutes(Type routeType)
		{
			if (routeType == null)
				throw new ArgumentNullException("routeType");

			var num = 0;

			foreach (var route in routes)
			{
				if (!(route.Value.GetType() == routeType) || !routes.TryRemove(route.Key, out _))
					continue;

				num++;
			}

			return num;
		}

		/// <summary>
		/// Starts the HTTP server and begins listening for incoming HTTP/HTTPS requests.
		/// </summary>
		/// <remarks>
		/// This method initializes the HTTP listener, configures it with the specified
		/// HTTP and HTTPS prefixes, and begins processing client requests. If the HTTP
		/// or HTTPS configuration is invalid, it will throw an exception.
		/// </remarks>
		/// <exception cref="Exception">
		/// Thrown if the server prefix is not specified or if neither HTTP nor HTTPS is enabled.
		/// </exception>
		public override void Start()
		{
			if (string.IsNullOrWhiteSpace(prefix))
				throw new("You must provide a listening prefix");

			if (!EnableHttp && !EnableHttps)
				throw new("You must listen on HTTP or HTTPS");

			listener = new();

			if (EnableHttp)
				listener.Prefixes.Add(HttpPrefix);

			if (EnableHttps)
				listener.Prefixes.Add(HttpsPrefix);

			listener.Start();

			queueUpdate = () => queue.UpdateQueue();
			
			LibraryUpdate.Register(queueUpdate);

			Task.Run(UpdateAsync);
		}

		/// <summary>
		/// Releases resources used by the RestServer instance, including stopping and closing the HTTP listener
		/// and clearing the registered routes.
		/// </summary>
		/// <remarks>
		/// This method ensures proper disposal of resources, stopping the listener if it is still running
		/// and removing all associated routes from the internal collection. It should be called to
		/// gracefully shut down the server and free up resources.
		/// </remarks>
		public void Dispose()
		{
			if (listener != null)
			{
				if (listener.IsListening)
					listener.Stop();

				listener.Close();
				listener = null!;
			}

			if (queueUpdate != null)
				LibraryUpdate.Unregister(queueUpdate);

			queue.ClearQueue();
			queueUpdate = null!;

			routes.Clear();
		}

		internal void OnContextReceived(RestServerContext ctx)
		{
			ctx.TargetRoute?.OnRequest(ctx);
		}

		private async Task UpdateAsync()
		{
			while (IsListening)
			{
				HttpListenerContext? context = null;
				
				try
				{
					context = await listener.GetContextAsync();
					
					if (context == null)
						continue;

					log.Debug(
						$"RECV_CTX: {context.Request.RemoteEndPoint} ({context.Request.HttpMethod}) -> {context.Request.RawUrl}");
					
					var rawUrl = context.Request.RawUrl;
					var parameters = new ConcurrentDictionary<string, string>();
					var num = rawUrl.IndexOf('?');
					var text = rawUrl;
					
					if (num > -1)
					{
						text = "";
						
						for (var i = 0; i < rawUrl.Length && i != num; i++)
							text += rawUrl[i];

						var array = rawUrl.Split(new char[1] { '?' })
							.Skip(1)
							.ToArray();
						
						foreach (string text2 in array)
						{
							var parts = text2.Split('=').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

							if (parts.Length != 2)
							{
								parameters.TryAdd(text2, string.Empty);
							}
							else
							{
								parameters.TryAdd(parts[0].Trim(), parts[1].Trim());
							}
						}
					}

					RestRoute? httpRoute = null;
					
					var method = context.Request.HttpMethod;
					
					if (parameters.TryRemove("methodOverride", out var value))
						method = value.ToUpper();

					foreach (var route in Routes)
					{
						var methods = route.Value.Methods;
						
						if (methods != null 
						    && methods.Length > 0 
						    && route.Value.Methods.All(x => !string.Equals(x.Method, method, StringComparison.CurrentCultureIgnoreCase)))
							continue;

						var text3 = text;
						
						if (route.Value.FixedUrl.Contains("/{") && route.Value.FixedUrl.Contains("}"))
						{
							route.Value.FixedUrl.TrySplit('/', null, true, true, out var splits2);
							rawUrl.TrySplit('/', null, true, true, out string[] splits3);
							
							if (splits2 != null && splits3 != null)
							{
								if (splits3.Length != splits2.Length)
									continue;

								var flag = false;
								
								for (var num2 = 0; num2 < splits3.Length; num2++)
								{
									var text4 = splits2[num2];
									var text5 = splits3[num2];
									
									if (text4.StartsWith("{") && text4.EndsWith("}"))
									{
										var text6 = text4.Replace("{", "").Replace("}", "");
										var value3 = WebUtility.UrlDecode(text5);
										
										if (!string.IsNullOrWhiteSpace(value3) 
										    && !string.IsNullOrWhiteSpace(text6)
										    && !parameters.ContainsKey(text6))
										{
											parameters.TryAdd(text6, value3);
											
											flag = true;
										}
									}
									else if (text4 != text5)
									{
										flag = false;
										break;
									}
								}

								if (!flag)
									continue;

								text3 = route.Value.FixedUrl;
							}
						}

						if (route.Value.FixedUrl == text3 || route.Value.IsMatch(rawUrl, text))
						{
							route.Value.ParseParameters(rawUrl, text3, parameters);
							
							httpRoute = route.Value;
							break;
						}
					}

					if (httpRoute == null)
					{
						context.Response.StatusCode = 404;
						context.Response.StatusDescription = "Not Found";
						
						context.Response.ContentType = "text/plain";
						
						using (var sw = new StreamWriter(context.Response.OutputStream))
							await sw.WriteLineAsync("Could not find route " + context.Request.RawUrl);

						context.Response.Close();
						continue;
					}

					var httpContext = await RestServerContext.GetAsync(context, this, httpRoute, parameters);
					
					if (httpContext == null)
					{
						context.Response.StatusCode = 500;
						context.Response.StatusDescription = "Null Wrapper";
						
						context.Response.ContentType = "text/plain";
						
						using (var sw = new StreamWriter(context.Response.OutputStream))
							await sw.WriteLineAsync("Failed to create a context wrapper");

						context.Response.Close();
					}
					else
					{
						queue.AddToQueue(() => OnContextReceived(httpContext));
					}
				}
				catch (Exception ex)
				{
					try
					{
						if (context != null)
						{
							context.Response.StatusCode = 500;
							context.Response.StatusDescription = "Server Error";
							
							context.Response.ContentType = "text/plain";
							
							using (var sw = new StreamWriter(context.Response.OutputStream))
								await sw.WriteLineAsync(ex.ToString());

							context.Response.Close();
						}
					}
					catch
					{
						// ignored	
					}

					log.Error("UpdateAsync", ex);
				}
			}
		}
	}
}