using System.Net;
using System.Net.Http;

using Newtonsoft.Json;

namespace NiveraAPI.Rest.Client
{
	/// <summary>
	/// Represents the context of an HTTP client interaction, encapsulating the
	/// request, response, and related metadata for an HTTP operation.
	/// </summary>
	public class RestClientContext
	{
		private volatile byte[] rawData;
		private volatile string stringData;

		private volatile HttpResponseMessage response;
		private volatile HttpRequestMessage request;
		private volatile Exception error;

		/// <summary>
		/// Retrieves the raw binary data of the HTTP response, if available.
		/// </summary>
		/// <remarks>
		/// This property provides access to the unprocessed byte array representing the HTTP response content.
		/// It is primarily intended for scenarios that require working with the raw response data, such as downloading files
		/// or handling non-textual content. If no response is available or the data has not been initialized, it defaults to null.
		/// </remarks>
		/// <value>
		/// A byte array representing the raw HTTP response data. Returns null if the response data is unavailable or uninitialized.
		/// </value>
		public byte[] Data => rawData;

		/// <summary>
		/// Represents the string content of the HTTP response, if available.
		/// </summary>
		/// <remarks>
		/// This property retrieves the string representation of the raw response data, typically for easier interpretation or processing.
		/// If the response is null or no data is available, it defaults to an empty string.
		/// </remarks>
		/// <value>
		/// A string containing the response data. Returns an empty string if the response data is unavailable or uninitialized.
		/// </value>
		public string String => stringData;

		/// <summary>
		/// Represents the error description or reason phrase provided in the HTTP response, if available.
		/// </summary>
		/// <remarks>
		/// This property retrieves the value of the <c>ReasonPhrase</c> from the HTTP response.
		/// If the response is null, it defaults to an empty string.
		/// </remarks>
		/// <value>
		/// A string that contains the error description or reason phrase of the HTTP response.
		/// Returns an empty string if the response is unavailable or the reason phrase is not provided.
		/// </value>
		public string ErrorString => response?.ReasonPhrase ?? string.Empty;

		/// <summary>
		/// Represents the media type of the HTTP content associated with the response.
		/// </summary>
		/// <remarks>
		/// This property retrieves the media type value from the Content-Type header of the HTTP response.
		/// If the response or its content is null, or if the Content-Type header is not present,
		/// this property returns an empty string.
		/// </remarks>
		/// <value>
		/// A string representing the media type of the HTTP content, such as "application/json" or "text/html".
		/// Returns an empty string if the media type cannot be determined.
		/// </value>
		public string ContentType => response.Content?.Headers?.ContentType?.MediaType ?? string.Empty;

		/// <summary>
		/// Indicates whether the HTTP response status code represents a successful operation.
		/// </summary>
		/// <remarks>
		/// This property evaluates to true if the HTTP response has a status code within the range
		/// of 200 to 299. It also ensures that no exception was encountered during the request.
		/// If an error occurred or the response is null, it returns false.
		/// </remarks>
		/// <value>
		/// A boolean value where true indicates the HTTP response status code is successful (2xx),
		/// and false indicates a failure or absence of a response.
		/// </value>
		public bool IsSuccessStatusCode
		{
			get
			{
				if (error == null)
					return response?.IsSuccessStatusCode ?? false;

				return false;
			}
		}

		/// <summary>
		/// Determines whether the HTTP response contains no content.
		/// </summary>
		/// <remarks>
		/// This property evaluates to true if the <see cref="HttpResponseMessage.Content"/> property
		/// of the response is null, indicating that no content is associated with the HTTP response.
		/// Conversely, it returns false if the response contains content.
		/// </remarks>
		/// <value>
		/// A boolean value where true indicates that the HTTP response has no associated content,
		/// and false indicates that content is present.
		/// </value>
		public bool IsEmpty => response?.Content == null;

		/// <summary>
		/// Indicates whether an HTTP response has been received.
		/// </summary>
		/// <remarks>
		/// This property evaluates to true if the underlying <see cref="HttpResponseMessage"/> object
		/// has been set, signaling that a response has been received for the HTTP request.
		/// Conversely, it will return false if no response is available, such as when the request
		/// has not yet been completed or has encountered an error.
		/// </remarks>
		/// <value>
		/// A boolean value where true indicates that an HTTP response is available, and false
		/// indicates that no response has been received.
		/// </value>
		public bool IsReceived => response != null;

		/// <summary>
		/// Gets the exception associated with the HTTP operation, if any.
		/// </summary>
		/// <remarks>
		/// This property provides access to the <see cref="Exception"/> encountered during the HTTP operation,
		/// such as connection failures or request timeouts. If the operation completed successfully without any errors,
		/// this property will return null.
		/// </remarks>
		/// <value>
		/// An instance of <see cref="Exception"/> representing the error that occurred during the HTTP operation,
		/// or null if no error was encountered.
		/// </value>
		public Exception Error => error;

		/// <summary>
		/// Gets the HTTP request message associated with the current context.
		/// </summary>
		/// <remarks>
		/// This property provides access to the <see cref="HttpRequestMessage"/> associated
		/// with the HTTP operation. It includes details such as the request URI, HTTP method,
		/// headers, and content. If no request has been sent or initialized, this property will return null.
		/// </remarks>
		/// <value>
		/// An instance of <see cref="HttpRequestMessage"/> representing the HTTP request,
		/// or null if no request is available.
		/// </value>
		public HttpRequestMessage Request => request;

		/// <summary>
		/// Gets the underlying HTTP response message associated with the request.
		/// </summary>
		/// <remarks>
		/// This property provides access to the full <see cref="HttpResponseMessage"/> object returned
		/// by the HTTP client. It contains details such as headers, status code, content, and other
		/// metadata about the HTTP response. If no response is available, the property will return null.
		/// </remarks>
		/// <value>
		/// An instance of <see cref="HttpResponseMessage"/> representing the HTTP response,
		/// or null if no response has been received.
		/// </value>
		public HttpResponseMessage Response => response;

		/// <summary>
		/// Gets the HTTP status code associated with the response.
		/// </summary>
		/// <remarks>
		/// This property retrieves the status code from the HTTP response. If no response is available,
		/// the status code defaults to <see cref="HttpStatusCode.InternalServerError"/>.
		/// </remarks>
		/// <value>
		/// An instance of <see cref="HttpStatusCode"/> representing the status code of the HTTP response,
		/// or <see cref="HttpStatusCode.InternalServerError"/> if the response is null.
		/// </value>
		public HttpStatusCode StatusCode => response?.StatusCode ?? HttpStatusCode.InternalServerError;

		/// <summary>
		/// Ensures that the HTTP response has a successful status code.
		/// </summary>
		/// <exception cref="HttpRequestException">
		/// Thrown if the HTTP response status code is not successful (i.e., it does not fall within the range 200–299).
		/// </exception>
		public void EnsureSuccessStatusCode()
			=> response?.EnsureSuccessStatusCode();

		/// <summary>
		/// Attempts to retrieve the value of a specified header from the HTTP response.
		/// </summary>
		/// <param name="headerKey">The key of the header to retrieve from the response.</param>
		/// <param name="headerValue">
		/// When this method returns, contains the value of the specified header
		/// if it is found in the response; otherwise, null.
		/// </param>
		/// <returns>
		/// True if the specified header is present and its value is successfully retrieved; otherwise, false.
		/// </returns>
		public bool TryGetHeader(string headerKey, out string headerValue)
		{
			var values = response.Headers.GetValues(headerKey);

			if (values != null)
			{
				using IEnumerator<string> enumerator = values.GetEnumerator();
				
				if (enumerator.MoveNext())
				{
					var current = enumerator.Current;
					
					headerValue = current;
					return true;
				}
			}

			headerValue = null;
			return false;
		}

		/// <summary>
		/// Checks if an HTTP response contains the specified header.
		/// </summary>
		/// <param name="headerKey">The key of the header to search for in the response.</param>
		/// <returns>
		/// True if the specified header is present in the HTTP response; otherwise, false.
		/// </returns>
		public bool HasHeader(string headerKey)
			=> TryGetHeader(headerKey, out _);

		/// <summary>
		/// Checks if an HTTP response contains the specified header.
		/// </summary>
		/// <param name="headerKey">The key of the header to search for in the response.</param>
		/// <returns>
		/// True if the specified header is present in the HTTP response; otherwise, false.
		/// </returns>
		public bool HasHeader(string headerKey, string headerValue)
		{
			if (TryGetHeader(headerKey, out string headerValue2))
				return headerValue2 == headerValue;

			return false;
		}

		/// <summary>
		/// Determines whether the response content is a valid JSON representation of the specified type.
		/// </summary>
		/// <typeparam name="T">The type to which the JSON content should be validated and deserialized.</typeparam>
		/// <returns>
		/// True if the response content is a valid JSON representation of the specified type; otherwise, false.
		/// </returns>
		public bool IsJson<T>()
			=> IsJson<T>(out _);

		/// <summary>
		/// Determines whether the content of the HTTP response is a valid JSON representation of the specified type.
		/// </summary>
		/// <param name="jsonType">The type to which the JSON content should be validated and deserialized.</param>
		/// <returns>
		/// True if the content of the HTTP response is a valid JSON representation of the specified type; otherwise, false.
		/// </returns>
		public bool IsJson(Type jsonType)
		{
			if (jsonType != null)
				return IsJson(jsonType, out _);

			return false;
		}

		/// <summary>
		/// Determines whether the string representation of the response content is a valid JSON representation of the specified type.
		/// </summary>
		/// <typeparam name="T">The type to which the JSON content should be validated and deserialized.</typeparam>
		/// <param name="jsonValue">When this method returns, contains the deserialized object of the specified type if the JSON is valid; otherwise, the default value of <typeparamref name="T"/>.</param>
		/// <returns>
		/// True if the string representation of the response content is a valid JSON representation of the specified type; otherwise, false.
		/// </returns>
		public bool IsJson(Type jsonType, out object jsonValue)
		{
			if (jsonType == null)
				throw new ArgumentNullException("jsonType");

			jsonValue = null!;

			if (string.IsNullOrWhiteSpace(stringData))
				return false;

			try
			{
				jsonValue = JsonConvert.DeserializeObject(stringData, jsonType);
				return jsonValue != null;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Determines whether the string representation of the response content is a valid JSON representation of the specified type.
		/// </summary>
		/// <typeparam name="T">The type to which the JSON content should be validated and deserialized.</typeparam>
		/// <param name="jsonValue">When this method returns, contains the deserialized object of the specified type if the JSON is valid; otherwise, the default value of <typeparamref name="T"/>.</param>
		/// <returns>
		/// True if the string data is a valid JSON representation of the specified type; otherwise, false.
		/// </returns>
		public bool IsJson<T>(out T jsonValue)
		{
			jsonValue = default(T);

			if (string.IsNullOrWhiteSpace(stringData))
				return false;

			try
			{
				jsonValue = JsonConvert.DeserializeObject<T>(stringData);
				return jsonValue != null;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Parses the response content as JSON and returns the deserialized object of the specified generic type.
		/// </summary>
		/// <typeparam name="T">The type to which the JSON content should be deserialized.</typeparam>
		/// <returns>An object representing the deserialized JSON content of the specified type.</returns>
		/// <exception cref="Exception">Thrown if the content cannot be parsed as JSON or the deserialization fails.</exception>
		public T GetJson<T>()
		{
			if (!IsJson(out T jsonValue))
				throw new("Could not parse JSON");

			return jsonValue;
		}

		/// <summary>
		/// Parses the response content as JSON and returns the deserialized object of the specified type.
		/// </summary>
		/// <param name="type">The type to which the JSON content should be deserialized. This parameter cannot be null.</param>
		/// <returns>An object representing the deserialized JSON content.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> parameter is null.</exception>
		/// <exception cref="Exception">Thrown if the content cannot be parsed as JSON or the deserialization fails.</exception>
		public object GetJson(Type type)
		{
			if (!IsJson(type, out object jsonValue))
				throw new("Could not parse JSON");

			return jsonValue;
		}

		/// <summary>
		/// Reads the raw binary data using a provided BinaryReader action.
		/// </summary>
		/// <param name="reader">A delegate that processes the raw data using a BinaryReader. This parameter cannot be null.</param>
		/// <returns>True if the raw data was successfully read and processed; otherwise, false if the data is empty or null.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="reader"/> parameter is null.</exception>
		public bool ReadRaw(Action<BinaryReader> reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			if (rawData == null || rawData.Length == 0)
				return false;

			using (var ms = new MemoryStream(rawData))
			using (var br = new BinaryReader(ms))
			{
				reader(br);
			}

			return true;
		}

		/// <summary>
		/// Handles the HTTP response once it is received, processes its content, and updates internal state.
		/// </summary>
		/// <param name="response">The HTTP response message received. This parameter can be null if an error occurred.</param>
		/// <param name="error">The exception that occurred during the HTTP request. This parameter can be null if no error occurred.</param>
		/// <returns>A task that represents the asynchronous operation of processing the HTTP response.</returns>
		internal async Task OnResponseReceived(HttpResponseMessage response, Exception error)
		{
			this.response = response;
			this.error = error;
			
			if (response?.Content == null)
				return;

			var bytes = new List<byte>((int)(response.Content.Headers.ContentLength.HasValue
				? response.Content.Headers.ContentLength.Value
				: 0));
			
			using var ms = new MemoryStream();
			
			await response.Content.CopyToAsync(ms);

			var num = 0;
			
			while ((num = ms.ReadByte()) != -1)
				bytes.Add((byte)num);

			rawData = bytes.ToArray();
			
			ms.Seek(0L, SeekOrigin.Begin);
			
			using var sr = new StreamReader(ms);
			
			stringData = await sr.ReadToEndAsync();
			
			bytes.Clear();
		}

		/// <summary>
		/// Creates an instance of <see cref="RestClientContext"/> using the specified HTTP request.
		/// </summary>
		/// <param name="request">The HTTP request message to associate with the context. This parameter cannot be null.</param>
		/// <returns>Returns an instance of <see cref="RestClientContext"/> initialized with the provided request.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is null.</exception>
		public static RestClientContext Get(HttpRequestMessage request)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			return new()
			{
				request = request
			};
		}
	}
}