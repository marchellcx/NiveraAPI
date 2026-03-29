using System.Collections.Concurrent;
using System.Net;

using HeyRed.Mime;

using Newtonsoft.Json;

using NiveraAPI.Logs;
using NiveraAPI.Rest.Routes;

namespace NiveraAPI.Rest.Server
{
	/// <summary>
	/// Represents the HTTP context for a request and response, encapsulating information
	/// about the incoming HTTP request, outgoing HTTP response, and any relevant data
	/// or parameters associated with the request.
	/// </summary>
	public class RestServerContext
	{
		private static volatile LogSink log = LogManager.GetSource("HTTP", "SRV_CTX");
		
		private volatile byte[] rawData;
		private volatile string stringData;

		private volatile IPEndPoint origin;
		private volatile HttpListenerContext context;
		private volatile ConcurrentDictionary<string, string> parameters;

		private volatile RestRoute targetRoute;
		
		/// <summary>
		/// Gets the origin of the current HTTP request.
		/// </summary>
		/// <remarks>
		/// The Origin property provides information about the remote endpoint from which the request originated.
		/// It returns an <see cref="IPEndPoint"/> that includes the client's IP address and port number, allowing identification of the source of the request.
		/// This property is particularly useful for logging, security checks, or tracing purposes.
		/// </remarks>
		public IPEndPoint Origin => origin;

		/// <summary>
		/// Gets the underlying HTTP listener context associated with the current request and response.
		/// </summary>
		/// <remarks>
		/// The Context property provides direct access to the underlying <see cref="HttpListenerContext"/> object.
		/// It contains detailed information about the HTTP request, including headers, client information,
		/// and other metadata. Additionally, this property allows manipulation of the HTTP response, such as
		/// setting status codes, headers, and response body content. This is particularly useful for low-level
		/// HTTP handling and custom protocol implementations.
		/// </remarks>
		public HttpListenerContext Context => context;
		
		/// <summary>
		/// Gets the HTTP request object associated with the current context.
		/// </summary>
		/// <remarks>
		/// This property provides access to the underlying <see cref="HttpListenerRequest"/> instance,
		/// which represents the client's request to the server. It allows inspection of request details
		/// such as headers, method, URL, and input stream data.
		/// </remarks>
		public HttpListenerRequest Request => context.Request;

		/// <summary>
		/// Gets the HTTP response object associated with the current context.
		/// </summary>
		/// <remarks>
		/// This property provides access to the underlying <see cref="HttpListenerResponse"/> instance
		/// used to configure and send the server's response to the client. It can be utilized to set headers,
		/// write to the response stream, and manage other properties related to the HTTP response.
		/// </remarks>
		public HttpListenerResponse Response => context.Response;
		
		/// <summary>
		/// Gets a read-only dictionary that contains the parameters associated with the current request.
		/// </summary>
		/// <remarks>
		/// The Parameters property provides access to parsed parameters from the request, typically extracted from the URL or query string.
		/// These key-value pairs can be used to retrieve custom input data sent from the client.
		/// The dictionary ensures data integrity by being read-only.
		/// </remarks>
		public IReadOnlyDictionary<string, string> Parameters => parameters;

		/// <summary>
		/// Gets the raw binary data associated with the current HTTP request.
		/// </summary>
		/// <remarks>
		/// The Data property provides access to the unprocessed byte array representing the body of the HTTP request.
		/// This data can include JSON payloads, file uploads, or any other binary content sent by the client.
		/// It is particularly useful for scenarios where raw binary manipulation or processing is required, such as decoding custom protocols or handling non-textual payloads.
		/// </remarks>
		public byte[] Data => rawData;
		
		/// <summary>
		/// Gets the value of the current string data in the HTTP context.
		/// </summary>
		/// <remarks>
		/// This property provides direct access to a volatile string data member related to the HTTP context.
		/// It can be used for retrieving context-specific string information that might be internally managed.
		/// The value of this property is typically assigned during the processing of an HTTP request.
		/// </remarks>
		public string String => stringData;

		/// <summary>
		/// Gets or sets the HTTP status code for the response.
		/// </summary>
		/// <remarks>
		/// This property allows managing the status code of the HTTP response, represented as an <see cref="HttpStatusCode"/> enum.
		/// It corresponds to the `StatusCode` property of the underlying <see cref="HttpListenerResponse"/>.
		/// Setting this property updates the response code that the server sends to the client,
		/// which can be used to indicate the result of the request, such as success (e.g., 200 OK), redirection (e.g., 301 Moved Permanently),
		/// or errors (e.g., 404 Not Found, 500 Internal Server Error).
		/// </remarks>
		public HttpStatusCode ResponseCode
		{
			get => (HttpStatusCode)context.Response.StatusCode;
			set => context.Response.StatusCode = (int)value;
		}

		/// <summary>
		/// Gets or sets the content type of the HTTP response.
		/// </summary>
		/// <remarks>
		/// This property provides access to the `ContentType` property of the underlying HTTP response.
		/// It allows specifying the MIME type of the response content that will be sent to the client, such as "text/html", "application/json", or "image/png".
		/// When setting this property, it directly modifies the `ContentType` value of the associated <see cref="HttpListenerResponse"/>.
		/// </remarks>
		public string ResponseContentType
		{
			get => context.Response.ContentType;
			set => context.Response.ContentType = value;
		}

		/// <summary>
		/// Gets or sets the associated <see cref="RestRoute"/> instance for the current HTTP context.
		/// </summary>
		/// <remarks>
		/// This property provides access to the specific route handling the current HTTP request. It enables
		/// servers to determine which route logic should process the request. The value of this property is
		/// typically set internally based on route matching logic within the server.
		/// </remarks>
		public RestRoute TargetRoute => targetRoute;

		/// <summary>
		/// Represents the context of an HTTP request and response, providing access to the
		/// associated request data, response tools, and metadata.
		/// </summary>
		/// <remarks>
		/// The HttpContext class contains methods and properties to handle HTTP requests, including access to the
		/// request's data, headers, parameters, and the ability to respond to the client with various forms of data.
		/// It facilitates communication between a server and a client within the HTTP protocol.
		/// </remarks>
		public RestServerContext()
		{
			
		}

		/// <summary>
		/// Creates an instance of the <see cref="RestServerContext"/> asynchronously by initializing it with the provided parameters
		/// and extracting request data from the HTTP listener.
		/// </summary>
		/// <param name="ctx">The <see cref="HttpListenerContext"/> representing the context of the HTTP request and response.</param>
		/// <param name="server">The <see cref="RestServer"/> instance providing server-level configurations and utilities.</param>
		/// <param name="route">The <see cref="RestRoute"/> associated with the current request, containing route metadata.</param>
		/// <param name="parameters">A thread-safe collection of route parameters obtained from the request URL.</param>
		/// <returns>A task that resolves to a fully initialized <see cref="RestServerContext"/> for the current HTTP request.</returns>
		public static async Task<RestServerContext> GetAsync(HttpListenerContext ctx, RestServer server, RestRoute route,
			ConcurrentDictionary<string, string> parameters)
		{
			var port = 0;

			var httpContext = new RestServerContext()
			{
				context = ctx,
				targetRoute = route,
				parameters = parameters
			};
			
			if (string.IsNullOrWhiteSpace(server.RealIpHeader) 
			    || !httpContext.TryGetHeader(server.RealIpHeader, out var realIp) 
			    || !IPAddress.TryParse(realIp, out var ipAddress))
			{
				httpContext.origin = ctx.Request.RemoteEndPoint ?? new(IPAddress.None, 0);
			}
			else
			{
				if (ctx.Request.RemoteEndPoint != null)
					port = ctx.Request.RemoteEndPoint.Port;
				else
					port = 0;
				
				httpContext.origin = new(ipAddress, port);
			}
			
			using (var memoryStream = new MemoryStream())
			{
				await httpContext.Request.InputStream.CopyToAsync(memoryStream);

				httpContext.rawData = memoryStream.ToArray();
				
				memoryStream.Seek(0, SeekOrigin.Begin);
				
				using (var streamReader = new StreamReader(memoryStream))
					httpContext.stringData = await streamReader.ReadToEndAsync();
			}
			
			return httpContext;
		}

		/// <summary>
		/// Parses the JSON content from the HTTP request and converts it into an object of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the object to which the JSON content should be deserialized.</typeparam>
		/// <returns>
		/// An instance of type <typeparamref name="T"/> containing the data obtained from the JSON content.
		/// </returns>
		/// <exception cref="Exception">Thrown when the JSON content cannot be parsed into the specified type.</exception>
		public T GetJson<T>()
		{
			if (!this.IsJson<T>(out var t))
				throw new("Could not parse JSON");

			return t;
		}

		/// <summary>
		/// Deserializes the JSON content from the HTTP request into an object of the specified type.
		/// </summary>
		/// <returns>
		/// An object containing the deserialized JSON data.
		/// </returns>
		public object GetJson(Type type)
		{
			if (!this.IsJson(type, out var obj))
				throw new("Could not parse JSON");

			return obj;
		}

		/// <summary>
		/// Determines if an HTTP request contains a header with the specified key.
		/// </summary>
		/// <param name="headerKey">The key of the header to check for existence in the HTTP request.</param>
		/// <returns>
		/// A boolean indicating whether the specified header is present in the HTTP request.
		/// Returns <c>true</c> if the header exists; otherwise, <c>false</c>.
		/// </returns>
		public bool HasHeader(string headerKey)
			=> this.context.Request.Headers.Get(headerKey) != null;


		/// <summary>
		/// Checks if the HTTP request contains a specific header with the specified value.
		/// </summary>
		/// <param name="headerKey">The key of the HTTP header to check.</param>
		/// <param name="headerValue">The value of the HTTP header to compare against.</param>
		/// <returns>
		/// <c>true</c> if the header exists and its value matches the specified <paramref name="headerValue"/>.
		/// Otherwise, <c>false</c>.
		/// </returns>
		public bool HasHeader(string headerKey, string headerValue)
			=> this.context.Request.Headers.Get(headerKey) == headerValue;

		/// <summary>
		/// Checks whether a parameter with the specified key exists in the current HTTP context.
		/// </summary>
		/// <param name="parameterKey">The key of the parameter to check for existence.</param>
		/// <returns>
		/// A boolean value indicating whether the parameter with the specified key exists.
		/// Returns <c>true</c> if the parameter exists; otherwise, <c>false</c>.
		/// </returns>
		public bool HasParameter(string parameterKey)
			=> this.parameters.ContainsKey(parameterKey);

		/// <summary>
		/// Determines whether the specified parameter exists and matches the given value in the current HTTP context.
		/// </summary>
		/// <param name="parameterKey">The key of the parameter to check.</param>
		/// <param name="parameterValue">The value to compare against the parameter's value.</param>
		/// <returns>
		/// A boolean value indicating whether the parameter exists and its value matches the specified value.
		/// Returns <c>true</c> if the parameter exists and its value matches; otherwise, <c>false</c>.
		/// </returns>
		public bool HasParameter(string parameterKey, string parameterValue)
		{
			if (!this.parameters.TryGetValue(parameterKey, out var parameter))
				return false;

			return parameter == parameterValue;
		}

		/// <summary>
		/// Determines whether the current string data is valid JSON that can be deserialized into the specified type.
		/// </summary>
		/// <typeparam name="T">The type to which the JSON string should be deserialized.</typeparam>
		/// <returns>
		/// A boolean value indicating whether the string data is valid JSON and can be deserialized into the specified type.
		/// Returns <c>true</c> if the string data is not null or whitespace and was successfully deserialized; otherwise, <c>false</c>.
		/// </returns>
		public bool IsJson<T>()
			=> this.IsJson<T>(out _);

		/// <summary>
		/// Determines whether the current string data is valid JSON that can be deserialized into the specified type.
		/// </summary>
		/// <param name="jsonType">The type to which the JSON string should be deserialized.</param>
		/// <returns>
		/// A boolean value indicating whether the string data is valid JSON that can be deserialized into the specified type.
		/// Returns <c>true</c> if the string data is not null or whitespace and was successfully deserialized; otherwise, <c>false</c>.
		/// </returns>
		public bool IsJson(Type jsonType)
		{
			if (jsonType == null)
				return false;

			return this.IsJson(jsonType, out _);
		}

		/// <summary>
		/// Determines whether the current string data can be interpreted as valid JSON for the specified type.
		/// </summary>
		/// <param name="jsonType">The type to which the JSON string should be deserialized.</param>
		/// <param name="jsonValue">When this method returns, contains the deserialized object of the specified type if successful; otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
		/// <returns>
		/// A boolean value indicating whether the string data is valid JSON that can be deserialized into the specified type.
		/// Returns <c>true</c> if the string data is not null or whitespace and was successfully deserialized; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="jsonType"/> is <c>null</c>.</exception>
		public bool IsJson(Type jsonType, out object? jsonValue)
		{
			if (jsonType == null)
				throw new ArgumentNullException(nameof(jsonType));

			jsonValue = null;

			if (string.IsNullOrWhiteSpace(this.stringData))
				return false;
			
			try
			{
				jsonValue = JsonConvert.DeserializeObject(this.stringData, jsonType);
				return jsonValue != null;
			}
			catch (Exception ex)
			{
				log.Error("IsJson", ex);
			}
			
			return false;
		}

		/// <summary>
		/// Determines whether the current string data can be successfully deserialized into the specified type.
		/// </summary>
		/// <typeparam name="T">The type to which the JSON string should be deserialized.</typeparam>
		/// <param name="jsonValue">When this method returns, contains the deserialized value of type <typeparamref name="T"/>, if successful; otherwise, the default value for the type. This parameter is passed uninitialized.</param>
		/// <returns>
		/// A boolean value indicating whether the string data is valid JSON that can be deserialized into the specified type.
		/// Returns <c>true</c> if the string data is not null or whitespace and was successfully deserialized; otherwise, <c>false</c>.
		/// </returns>
		public bool IsJson<T>(out T jsonValue)
		{
			jsonValue = default(T);

			if (string.IsNullOrWhiteSpace(this.stringData))
				return false;
			
			try
			{
				jsonValue = JsonConvert.DeserializeObject<T>(this.stringData);
				return jsonValue != null;
			}
			catch (Exception ex)
			{
				log.Error("IsJson", ex);
			}
			
			return false;
		}

		/// <summary>
		/// Processes raw data using a user-provided binary reader action.
		/// </summary>
		/// <param name="reader">An action that processes the binary data using a <see cref="BinaryReader"/>.</param>
		/// <returns>
		/// A boolean value indicating whether the raw data was successfully processed.
		/// Returns <c>true</c> if the raw data exists and the reader action was invoked successfully; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="reader"/> argument is null.</exception>
		public bool ReadRaw(Action<BinaryReader> reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			if (this.rawData == null || this.rawData.Length == 0)
				return false;
			
			using (var memoryStream = new MemoryStream(this.rawData))
			{
				using (var binaryReader = new BinaryReader(memoryStream))
				{
					reader(binaryReader);
				}
			}
			
			return true;
		}

		/// <summary>
		/// Sends a byte array as a response to the client with the specified content type.
		/// </summary>
		/// <param name="content">The byte array to be sent as the response body.</param>
		/// <param name="contentType">The content type of the response. Defaults to "application/octet-stream".</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="content"/> is null or <paramref name="contentType"/> is null,
		/// empty, or consists only of whitespace.</exception>
		public void RespondBytes(byte[] content, string contentType = "application/octet-stream")
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			if (string.IsNullOrWhiteSpace(contentType))
				throw new ArgumentNullException(nameof(contentType));
			
			this.context.Response.StatusCode = 200;
			this.context.Response.StatusDescription = "OK";
			
			this.context.Response.ContentType = contentType;
			this.context.Response.ContentLength64 = content.Length;
			
			for (var i = 0; i < content.Length; i++)
				this.context.Response.OutputStream.WriteByte(content[i]);
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Sends an error response to the client with a specified message and HTTP status code.
		/// </summary>
		/// <param name="message">The error message to be included in the response body and status description.</param>
		/// <param name="errorCode">The HTTP status code representing the error. Defaults to <see cref="HttpStatusCode.BadRequest"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided message is null, empty, or consists only of whitespace.</exception>
		/// <exception cref="Exception">Thrown when attempting to send an error response with an <see cref="HttpStatusCode.OK"/> status code.</exception>
		public void RespondError(string message, HttpStatusCode errorCode = HttpStatusCode.BadRequest)
		{
			if (string.IsNullOrWhiteSpace(message))
				throw new ArgumentNullException(nameof(message));

			if (errorCode == HttpStatusCode.OK)
				throw new("Cannot send error response with OK status code");
			
			this.context.Response.StatusCode = (int)errorCode;
			this.context.Response.StatusDescription = message;
			
			this.context.Response.ContentType = "text/plain";
			
			using (var streamWriter = new StreamWriter(this.context.Response.OutputStream))
				streamWriter.Write(message);
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Sends a file to the client as a response.
		/// </summary>
		/// <param name="filePath">The full path of the file to be sent to the client.</param>
		/// <param name="sendBuffered">Specifies if the file should be sent using a buffered approach. Defaults to false.</param>
		/// <param name="sendChunked">Specifies if the file should be sent in chunks. Defaults to false.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided file path is null, empty, or whitespace.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the specified file is not found at the given path.</exception>
		public void RespondFile(string filePath, bool sendBuffered = false, bool sendChunked = false)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

			if (!File.Exists(filePath))
				throw new FileNotFoundException("File not found", filePath);
			
			var fileName = Path.GetFileName(filePath);
			
			using (var fileStream = File.OpenRead(filePath))
			{
				this.context.Response.AddHeader("Content-disposition", string.Concat("attachment; filename=", fileName));
				
				this.context.Response.ContentLength64 = fileStream.Length;
				this.context.Response.ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(filePath));
				
				this.context.Response.SendChunked = sendChunked;
				
				if (!sendBuffered)
				{
					fileStream.CopyTo(this.context.Response.OutputStream);
				}
				else
				{
					using (var binaryWriter = new BinaryWriter(this.context.Response.OutputStream))
					{
						var numArray = new byte[0x10000];
						
						while (true)
						{
							var num = fileStream.Read(numArray, 0, numArray.Length);
							
							if (num <= 0)
								break;
							
							binaryWriter.Write(numArray, 0, num);
							binaryWriter.Flush();
						}
						
						binaryWriter.Close();
					}
				}
				
				this.context.Response.StatusCode = 200;
				this.context.Response.StatusDescription = "OK";
			}
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Asynchronously sends a file to the client as a response.
		/// </summary>
		/// <param name="filePath">The full path of the file to be sent to the client.</param>
		/// <param name="sendBuffered">Indicates whether the file should be sent using a buffered approach. Defaults to false.</param>
		/// <param name="sendChunked">Indicates whether the file should be sent in chunks. Defaults to false.</param>
		/// <returns>A task that represents the asynchronous file response operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided file path is null, empty, or whitespace.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the specified file is not found at the given path.</exception>
		public async Task RespondFileAsync(string filePath, bool sendBuffered = false, bool sendChunked = false)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

			if (!File.Exists(filePath))
				throw new FileNotFoundException("File not found", filePath);
			
			var fileName = Path.GetFileName(filePath);
			
			using (var fileStream = File.OpenRead(filePath))
			{
				this.context.Response.AddHeader("Content-disposition", string.Concat("attachment; filename=", fileName));
				
				this.context.Response.ContentLength64 = fileStream.Length;
				this.context.Response.ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(filePath));
				
				this.context.Response.SendChunked = sendChunked;
				
				if (!sendBuffered)
				{
					await fileStream.CopyToAsync(this.context.Response.OutputStream);
				}
				else
				{
					using (var binaryWriter = new BinaryWriter(this.context.Response.OutputStream))
					{
						var numArray = new byte[0x10000];
						
						while (true)
						{
							var num = fileStream.Read(numArray, 0, numArray.Length);
							
							if (num <= 0)
								break;
							
							binaryWriter.Write(numArray, 0, num);
							binaryWriter.Flush();
						}
						
						binaryWriter.Close();
					}
				}
				
				this.context.Response.StatusCode = 200;
				this.context.Response.StatusDescription = "OK";
			}

			this.context.Response.Close();
		}

		/// <summary>
		/// Sends a JSON response to the client.
		/// </summary>
		/// <param name="json">The object to serialize and send as a JSON response.</param>
		/// <param name="indented">Indicates whether the JSON should be formatted with indentation for readability. Defaults to false.</param>
		public void RespondJson(object json, bool indented = false)
		{
			var str = JsonConvert.SerializeObject(json, (indented ? Formatting.Indented : Formatting.None));
			
			this.context.Response.StatusCode = 200;
			this.context.Response.StatusDescription = "OK";
			
			this.context.Response.ContentType = "application/json";
			
			using (var streamWriter = new StreamWriter(this.context.Response.OutputStream))
				streamWriter.Write(str);
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Redirects the client to a specified URL.
		/// </summary>
		/// <param name="url">The target URL to which the client will be redirected.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided URL is null or whitespace.</exception>
		public void RespondRedirect(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException(nameof(url));
			
			this.context.Response.Redirect(url);
			this.context.Response.Close();
		}

		/// <summary>
		/// Sends the content of a stream as a response to the client.
		/// </summary>
		/// <param name="stream">The stream containing data to be sent in the response.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided stream is null.</exception>
		public void RespondStream(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			this.context.Response.ContentType = "application/octet-stream";
			
			this.context.Response.StatusCode = 200;
			this.context.Response.StatusDescription = "OK";
			
			stream.CopyTo(this.context.Response.OutputStream);
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Asynchronously sends the content of a stream as a response to the client.
		/// </summary>
		/// <param name="stream">The stream containing data to be sent in the response.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided stream is null.</exception>
		public async Task RespondStreamAsync(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			
			this.context.Response.StatusCode = 200;
			this.context.Response.StatusDescription = "OK";
			
			await stream.CopyToAsync(this.context.Response.OutputStream);
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Sends a plain text response to the client using the specified content and MIME type.
		/// </summary>
		/// <param name="content">The text content to send in the response.</param>
		/// <param name="contentType">The MIME type of the response content. Defaults to "text/plain".</param>
		/// <exception cref="ArgumentNullException">Thrown when the content or contentType is null or whitespace.</exception>
		public void RespondText(string content, string contentType = "text/plain")
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			if (string.IsNullOrWhiteSpace(contentType))
				throw new ArgumentNullException(nameof(contentType));
			
			this.context.Response.StatusCode = 200;
			this.context.Response.StatusDescription = "OK";
			
			this.context.Response.ContentType = contentType;
			
			using (var streamWriter = new StreamWriter(this.context.Response.OutputStream))
				streamWriter.Write(content);
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Sends a response to the client by writing binary data using the provided writer function.
		/// </summary>
		/// <param name="writer">An action that writes the binary data to the provided BinaryWriter.</param>
		/// <param name="contentType">The MIME type of the response content. Defaults to "application/octet-stream".</param>
		/// <exception cref="ArgumentNullException">Thrown when the writer or contentType is null or whitespace.</exception>
		public void RespondWrite(Action<BinaryWriter> writer, string contentType = "application/octet-stream")
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			
			if (string.IsNullOrWhiteSpace(contentType))
				throw new ArgumentNullException(nameof(contentType));
			
			using (var memoryStream = new MemoryStream())
			using (var binaryWriter = new BinaryWriter(memoryStream))
			{
				writer(binaryWriter);

				memoryStream.CopyTo(this.context.Response.OutputStream);

				this.context.Response.ContentLength64 = memoryStream.Length;
			}

			this.context.Response.StatusCode = 200;
			this.context.Response.StatusDescription = "OK";
			
			this.context.Response.ContentType = contentType;
			
			this.context.Response.Close();
		}

		/// <summary>
		/// Attempts to retrieve the value of a specified header from the HTTP request.
		/// </summary>
		/// <param name="headerKey">The key of the header to retrieve.</param>
		/// <param name="headerValue">The output variable where the value of the header will be stored, if found.</param>
		/// <returns>Returns true if the header exists, otherwise false.</returns>
		public bool TryGetHeader(string headerKey, out string headerValue)
		{
			headerValue = this.context.Request.Headers.Get(headerKey);
			return headerValue != null;
		}

		/// <summary>
		/// Attempts to retrieve the value of a parameter from the context based on the specified parameter key.
		/// </summary>
		/// <param name="parameterKey">The key of the parameter to retrieve.</param>
		/// <param name="parameterValue">The output variable where the value of the parameter will be stored, if found.</param>
		/// <returns>Returns true if the parameter exists, otherwise false.</returns>
		public bool TryGetParameter(string parameterKey, out string parameterValue)
			=> this.parameters.TryGetValue(parameterKey, out parameterValue);
	}
}