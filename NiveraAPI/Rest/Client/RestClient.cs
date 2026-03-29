using System.Collections.Concurrent;
using System.Net.Http;

using Newtonsoft.Json;

using NiveraAPI.Logs;
using NiveraAPI.Utilities;

namespace NiveraAPI.Rest.Client
{
	/// <summary>
	/// Represents an HTTP client capable of sending and receiving HTTP requests
	/// with support for various payload formats.
	/// </summary>
	public class RestClient
	{
		private volatile LogSink log;
		private volatile System.Net.Http.HttpClient client;

		private volatile Action queueUpdate;
		
		private volatile ActionQueue queue = new();
		private volatile ConcurrentQueue<RestRequest> requestQueue = new();

		/// <summary>
		/// Gets the number of HTTP requests currently in the internal request queue.
		/// </summary>
		/// <remarks>
		/// The <c>RequestCount</c> property provides the current count of pending
		/// HTTP requests that have been queued but not yet processed.
		/// </remarks>
		public int RequestCount => requestQueue.Count;

		/// <summary>
		/// Provides methods to interact with HTTP endpoints, enabling seamless integration with HTTP services.
		/// This includes support for sending HTTP requests with various payload types (e.g., multipart, stream, bytes, text, JSON),
		/// and mechanisms for event handling and asynchronous request processing.
		/// </summary>
		public RestClient(Func<System.Net.Http.HttpClient>? httpClientFactory = null)
		{
			log = LogManager.GetSource("HTTP", "Client");

			if (httpClientFactory == null)
			{
				client = new();
			}
			else
			{
				client = httpClientFactory();
			}

			queueUpdate = () => queue.UpdateQueue();

			LibraryUpdate.Register(queueUpdate);
			
			Task.Run(UpdateAsync);
		}

		/// <summary>
		/// Sends an HTTP POST request with a multipart content payload to the specified URL.
		/// The multipart content is constructed using a provided builder function.
		/// </summary>
		/// <param name="url">The target URL for the HTTP POST request.</param>
		/// <param name="multipartBuilder">A delegate used to construct the multipart content for the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if no state is specified.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="multipartBuilder"/> parameter is null or empty.</exception>
		public void PostWithMultipart(string url, Action<MultipartContent> multipartBuilder,
			Action<RestRequest> callback, object? state = null, IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException("url");

			if (multipartBuilder == null)
				throw new ArgumentNullException("multipartBuilder");

			CreateWithPayload(delegate
			{
				var multipartContent = new MultipartContent();
				
				multipartBuilder(multipartContent);
				return multipartContent;
			}, url, HttpMethod.Post, callback, state, headers);
		}

		/// <summary>
		/// Sends an HTTP POST request with a stream-based payload to the specified URL.
		/// The payload is included in the request body as a stream.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">The stream providing the content to be included in the body of the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null or empty.</exception>
		public void PostWithStream(string url, Stream payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			CreateWithPayload((_) => new StreamContent(payload), url, HttpMethod.Post, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP POST request with a binary payload to the specified URL.
		/// The payload is included as the body of the request in binary format.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">The binary content to be included in the body of the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null or empty.</exception>
		public void PostWithBytes(string url, byte[] payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");
			
			CreateWithPayload((_) => new ByteArrayContent(payload), url, HttpMethod.Post, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP POST request with a plain text payload to the specified URL.
		/// The payload is included as the body of the request in plain text format.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">The text content to be included in the body of the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null or empty.</exception>
		public void PostWithText(string url, string payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (string.IsNullOrWhiteSpace(payload))
				throw new ArgumentNullException("payload");
				
			CreateWithPayload((_) => new StringContent(payload), url, HttpMethod.Post, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP POST request with a JSON payload to the specified URL.
		/// The payload is serialized to JSON format before being sent.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">The object to be serialized as JSON and included as the payload of the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null or empty.</exception>
		public void PostWithJson(string url, object payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			CreateWithPayload(
				(_) =>
					CreateContentWithHeader(() => new StringContent(JsonConvert.SerializeObject(payload)),
						"application/json"), url, HttpMethod.Post, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP GET request with a multipart payload to the specified URL.
		/// This method uses a builder function to configure the multipart content.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="multipartBuilder">A builder action used to configure the multipart content of the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="multipartBuilder"/> parameter is null or empty.</exception>
		public void GetWithMultipart(string url, Action<MultipartContent> multipartBuilder,
			Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException("url");

			if (multipartBuilder == null)
				throw new ArgumentNullException("multipartBuilder");

			CreateWithPayload(delegate
			{
				var multipartContent = new MultipartContent();
				
				multipartBuilder(multipartContent);
				return multipartContent;
			}, url, HttpMethod.Get, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP GET request with a stream-based payload to the specified URL.
		/// This method sets the given stream as the body of the request.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">The stream content to include in the request body.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null, empty, or invalid.</exception>
		public void GetWithStream(string url, Stream payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			CreateWithPayload((_) => new StreamContent(payload), url, HttpMethod.Get, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP GET request with a byte-array payload to the specified URL.
		/// This method sets the given byte array as the body of the request.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">The byte-array content to include in the request body.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null, empty, or invalid.</exception>
		public void GetWithBytes(string url, byte[] payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			CreateWithPayload((_) => new ByteArrayContent(payload), url, HttpMethod.Get, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP GET request with a plain text payload to the specified URL.
		/// This method sets the given text as the body of the request.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">The plain text content to include in the request body.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null or empty.</exception>
		public void GetWithText(string url, string payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (string.IsNullOrWhiteSpace(payload))
				throw new ArgumentNullException("payload");

			CreateWithPayload((_) => new StringContent(payload), url, HttpMethod.Get, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP GET request with a JSON payload to the specified URL.
		/// This method serializes the given object into JSON format and sets it as the body of the request.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">An object to be serialized into JSON format and included in the request body.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object associated with the request, or null if not required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are specified.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> or <paramref name="payload"/> parameter is null or empty.</exception>
		public void GetWithJson(string url, object payload, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");
				
			CreateWithPayload(
				(_) =>
					CreateContentWithHeader(() => new StringContent(JsonConvert.SerializeObject(payload)),
						"application/json"), url, HttpMethod.Get, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP request with a stream-based payload using the specified HTTP method.
		/// This method is used to transmit data from a <see cref="Stream"/> object to a specified URL, with optional custom headers, state, and completion handling.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">A <see cref="Stream"/> object containing the data to be included in the request body.</param>
		/// <param name="method">The HTTP method (e.g., GET, POST) to be used for the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object to include with the request, or null if no state is required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are provided.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/>, <paramref name="payload"/>, or <paramref name="method"/> parameter is null or empty.</exception>
		public void WithStream(string url, Stream payload, HttpMethod method, Action<RestRequest> callback,
			object? state = null, IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			if (method == null)
				throw new ArgumentNullException("method");

			CreateWithPayload((_) => new StreamContent(payload), url, method, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP request with a binary payload using the specified HTTP method.
		/// This method is used for transmitting byte array data to a specified URL with optional custom headers, state, and completion handling.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">A byte array containing the data to be included in the request body.</param>
		/// <param name="method">The HTTP method (e.g., GET, POST) to be used for the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object to include with the request, or null if no state is required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are provided.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/>, <paramref name="payload"/>, or <paramref name="method"/> parameter is null or empty.</exception>
		public void WithBytes(string url, byte[] payload, HttpMethod method, Action<RestRequest> callback,
			object? state = null, IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			if (method == null)
				throw new ArgumentNullException("method");

			CreateWithPayload((_) => new ByteArrayContent(payload), url, method, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP request with a plain text payload using the specified HTTP method.
		/// This method enables the transmission of string data to a given URL, with support for customizable headers, state information, and completion handling.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">A string representing the content to be included in the request body.</param>
		/// <param name="method">The HTTP method (e.g., GET, POST) to be used for the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object to include with the request, or null if no state is required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are provided.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/>, <paramref name="payload"/>, or <paramref name="method"/> parameter is null or empty.</exception>
		public void WithText(string url, string payload, HttpMethod method, Action<RestRequest> callback,
			object? state = null, IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			if (method == null)
				throw new ArgumentNullException("method");

			CreateWithPayload((_) => new StringContent(payload), url, method, callback, state, headers);
		}

		/// <summary>
		/// Constructs and sends an HTTP request with a JSON payload using the specified HTTP method.
		/// This method facilitates the transmission of structured JSON data to a specified URL, allowing for customizable headers, state, and completion handling.
		/// </summary>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="payload">An object representing the JSON content to be serialized and included in the request body.</param>
		/// <param name="method">The HTTP method (e.g., GET, POST) to be used for the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object to include with the request, or null if no state is required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are provided.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/>, <paramref name="payload"/>, or <paramref name="method"/> parameter is null or empty.</exception>
		public void WithJson(string url, object payload, HttpMethod method, Action<RestRequest> callback,
			object? state = null, IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			if (payload == null)
				throw new ArgumentNullException("payload");

			if (method == null)
				throw new ArgumentNullException("method");
			
			CreateWithPayload(
				(_) =>
					CreateContentWithHeader(() => new StringContent(JsonConvert.SerializeObject(payload)),
						"application/json"), url, method, callback, state, headers);
		}

		/// <summary>
		/// Initiates an HTTP GET request to the specified URL with an optional callback, state, and headers.
		/// This method allows for simple retrieval of resources through HTTP GET, with the ability to include additional metadata or processing logic.
		/// </summary>
		/// <param name="url">The target URL for the HTTP GET request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the GET request.</param>
		/// <param name="state">An optional user-defined object to include with the request, or null if no state is required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP GET request, or null if no headers are provided.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="url"/> parameter is null or empty.</exception>
		public void Get(string url, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");

			Create(new(HttpMethod.Get, url), callback, state, headers);
		}

		/// <summary>
		/// Creates an HTTP request message with the specified payload content, URL, HTTP method, callback, and optional parameters.
		/// This method enables construction of requests with dynamically generated content that allows for flexible HTTP interactions.
		/// </summary>
		/// <param name="contentFactory">A factory delegate to generate the HTTP content for the request, given the associated request message.</param>
		/// <param name="url">The target URL for the HTTP request.</param>
		/// <param name="method">The HTTP method to use for the request, such as GET or POST.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object to pass information with the request, or null if no state is required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are provided.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="contentFactory"/>, <paramref name="url"/>, or <paramref name="method"/> parameters are null or empty.</exception>
		public void CreateWithPayload(Func<HttpRequestMessage, HttpContent> contentFactory, string url, HttpMethod method, Action<RestRequest> callback, object? state = null,
			IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (string.IsNullOrEmpty(url))
				throw new ArgumentNullException("url");
			
			if (contentFactory == null)
				throw new ArgumentNullException("contentFactory");

			if (method == null)
				throw new ArgumentNullException("method");

			var httpRequestMessage = new HttpRequestMessage(method, url);
			var httpContent = contentFactory(httpRequestMessage);
			
			if (httpContent != null)
				httpRequestMessage.Content = httpContent;

			Create(httpRequestMessage, callback, state, headers);
		}

		/// <summary>
		/// Enqueues an HTTP request for execution and associates it with a specified callback and optional state.
		/// This method prepares the request by including the provided headers and ensures that the request is properly
		/// encapsulated within the HTTP client context for processing.
		/// </summary>
		/// <param name="message">The HTTP request message that specifies the details of the request.</param>
		/// <param name="callback">A callback action that is invoked upon completion of the request.</param>
		/// <param name="state">An optional user-defined object to pass information with the request, or null if no state is required.</param>
		/// <param name="headers">An optional collection of headers to include in the HTTP request, or null if no headers are provided.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="message"/> parameter is null.</exception>
		public void Create(HttpRequestMessage message, Action<RestRequest> callback, object? state = null, IEnumerable<KeyValuePair<string, string>>? headers = null)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var httpContext = RestClientContext.Get(message);
			
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> header in headers)
				{
					httpContext.Request.Headers.Add(header.Key, header.Value);
				}
			}

			log.Debug("HTTP_REQ_QUEUE", $"{message.Method}: {message.RequestUri} ({httpContext.Request.Headers.Count()} headers)");

			requestQueue.Enqueue(new()
			{
				Context = httpContext,
				Callback = callback,
				State = state
			});
		}

		/// <summary>
		/// Releases all resources used by the current instance of the RestClient class.
		/// This includes disposing of the underlying RestClient instance to free up network and memory resources.
		/// </summary>
		public void Dispose()
		{
			if (client != null)
			{
				client.Dispose();
				client = null!;
			}
			
			if (queueUpdate != null)
				LibraryUpdate.Unregister(queueUpdate);

			queue.ClearQueue();
			queueUpdate = null!;
		}

		private void OnResponse(RestClientContext ctx, RestRequest req)
		{
			if (req?.Callback != null)
			{
				try
				{
					req.Callback(req);
				}
				catch (Exception ex)
				{
					log.Error(ex);
				}
			}
		}

		private async Task UpdateAsync()
		{
			while (client != null)
			{
				try
				{
					while (requestQueue.TryDequeue(out var request))
					{
						try
						{
							if (request != null)
							{
								log.Debug("HTTP_REQ_UPDATE", $"Sending {request.Context.Request.Method}: {request.Context.Request.RequestUri}");

								var httpResponseMessage = await client.SendAsync(request.Context.Request);
								
								if (httpResponseMessage != null)
								{
									log.Debug("HTTP_REQ_UPDATE", $"Received: {httpResponseMessage.StatusCode}");

									await request.Context.OnResponseReceived(httpResponseMessage, null!);
									
									queue.AddToQueue(() => OnResponse(request.Context, request));
								}
							}
						}
						catch (Exception error)
						{
							await request.Context?.OnResponseReceived(null!, error);
							
							queue.AddToQueue(() => OnResponse(request.Context, request));

							log.Error("HTTP_REQ_UPDATE", error);
						}
					}
				}
				catch
				{
					// ignored
				}
			}
		}

		private static HttpContent CreateContentWithHeader(Func<HttpContent> builder, string header, long? length = null)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");

			if (string.IsNullOrWhiteSpace(header))
				throw new ArgumentNullException("header");

			var httpContent = builder();
			
			httpContent.Headers.ContentType = new(header);
			
			if (length.HasValue)
				httpContent.Headers.ContentLength = length.Value;

			return httpContent;
		}
	}
}