using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NiveraAPI.Discord.Embeds;
using NiveraAPI.Discord.Mentions;

namespace NiveraAPI.Discord;

/// <summary>
/// Represents a message that can be sent to a Discord webhook.
/// It allows customization of message content, text-to-speech functionality,
/// embedded content, and allowed mentions.
/// </summary>
public struct DiscordMessage
{
    private static volatile HttpMethod patchMethod = new("PATCH");
    private static volatile MediaTypeHeaderValue jsonHeader = MediaTypeHeaderValue.Parse("application/json");

    /// <summary>
    /// The content of the message.
    /// </summary>
    [JsonProperty("content")] 
    public string Content;

    /// <summary>
    /// Whether the message should be spoken aloud.
    /// </summary>
    [JsonProperty("tts")]
    public bool IsTextToSpeech;

    /// <summary>
    /// An array of embeds to include in the message.
    /// </summary>
    [JsonProperty("embeds")] 
    public DiscordEmbed[]? Embeds;

    /// <summary>
    /// Configuration options for mentions.
    /// </summary>
    [JsonProperty("allowed_mentions")] 
    public AllowedMentions? Mentions;

    /// <summary>
    /// Sets the content of the message and optionally specifies if the message is text-to-speech enabled.
    /// </summary>
    /// <param name="content">The content text of the message.</param>
    /// <param name="isTts">A boolean value indicating whether the message should use text-to-speech. Default is false.</param>
    /// <returns>
    /// A modified <see cref="DiscordMessage"/> instance with the updated content and text-to-speech settings.
    /// </returns>
    public DiscordMessage WithContent(string content, bool isTts = false)
    {
        Content = content;
        IsTextToSpeech = isTts;
        return this;
    }

    /// <summary>
    /// Enables or disables the text-to-speech (TTS) setting for the message.
    /// </summary>
    /// <param name="isTts">A boolean value indicating whether text-to-speech should be enabled for the message. Default is true.</param>
    /// <returns>
    /// A modified <see cref="DiscordMessage"/> instance with the updated text-to-speech setting.
    /// </returns>
    public DiscordMessage WithTextToSpeech(bool isTts = true)
    {
        IsTextToSpeech = isTts;
        return this;
    }

    /// <summary>
    /// Adds one or more embeds to the message.
    /// </summary>
    /// <param name="embeds">An array of <see cref="DiscordEmbed"/> objects to include in the message.</param>
    /// <returns>
    /// A modified <see cref="DiscordMessage"/> instance with the added embeds.
    /// </returns>
    public DiscordMessage WithEmbeds(params DiscordEmbed[] embeds)
    {
        if (Embeds != null && Embeds.Any())
        {
            var list = new List<DiscordEmbed>(Embeds);

            list.AddRange(embeds);

            Embeds = list.ToArray();
            return this;
        }

        Embeds = embeds;
        return this;
    }

    /// <summary>
    /// Configures the mentions that are allowed in the message, such as specific roles or users.
    /// </summary>
    /// <param name="discordMessageAllowedMentions">The mention settings that specify which roles, users, or types of mentions are allowed in the message.</param>
    /// <returns>
    /// A modified <see cref="DiscordMessage"/> instance with the updated allowed mentions configuration.
    /// </returns>
    public DiscordMessage WithMentions(AllowedMentions? discordMessageAllowedMentions)
    {
        Mentions = discordMessageAllowedMentions;
        return this;
    }

    /// <summary>
    /// Converts the current <see cref="DiscordMessage"/> instance to its JSON string representation.
    /// Truncates the content and embed descriptions if their lengths exceed a predefined limit.
    /// </summary>
    /// <returns>
    /// A JSON string representing the serialized <see cref="DiscordMessage"/> instance.
    /// </returns>
    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(Content) && Content.Length >= 1900)
            Content = Content.Substring(0, 1900) + " ...";

        if (Embeds != null && Embeds.Any())
        {
            var embeds = Embeds;

            for (int i = 0; i < embeds.Length; i++)
            {
                var discordEmbed = embeds[i];

                if (!string.IsNullOrWhiteSpace(discordEmbed.Description) && discordEmbed.Description.Length >= 1900)
                {
                    var description = discordEmbed.Description;

                    description = description.Substring(0, 1900) + " ...";
                    discordEmbed.WithDescription(description);
                }
            }
        }

        return JsonConvert.SerializeObject(this);
    }

    /// <summary>
    /// Converts the current <see cref="DiscordMessage"/> instance into an <see cref="HttpContent"/> object
    /// suitable for sending as a payload in a webhook request.
    /// </summary>
    /// <returns>
    /// A <see cref="MultipartFormDataContent"/> object containing the serialized JSON representation of the
    /// <see cref="DiscordMessage"/> instance with the appropriate content type headers.
    /// </returns>
    public HttpContent ToWebhookHttpContent()
    {
        var boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
        var multipart = new MultipartFormDataContent(boundary);
        var json = new StringContent(ToString());

        json.Headers.ContentType = jsonHeader;

        multipart.Add(json, "payload_json");
        return multipart;
    }

    /// <summary>
    /// Sends the Discord message to the specified webhook endpoint asynchronously.
    /// </summary>
    /// <param name="webhookToken">The token of the Discord webhook to post the message to.</param>
    /// <param name="webhookId">The unique identifier of the Discord webhook.</param>
    /// <param name="client">
    /// An optional <see cref="HttpClient"/> instance used for the HTTP request.
    /// If not provided, a default <see cref="HttpClient"/> instance will be used.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation,
    /// containing the <see cref="HttpResponseMessage"/> from the webhook submission.
    /// </returns>
    public async Task<HttpResponseMessage> PostToWebhookAsync(string webhookToken, ulong webhookId, HttpClient? client = null)
        => await PostToWebhookAsync(GetPostUrl(webhookToken, webhookId), client);

    /// <summary>
    /// Posts the current message to a Discord webhook and waits for the completion of the request.
    /// </summary>
    /// <param name="webhookToken">The token of the Discord webhook.</param>
    /// <param name="webhookId">The unique identifier of the Discord webhook.</param>
    /// <param name="client">An optional instance of <see cref="HttpClient"/> to use for the request. If not provided, a default client will be used.</param>
    /// <returns>
    /// An awaitable task that represents the HTTP response received after the webhook is posted.
    /// </returns>
    public async Task<HttpResponseMessage> PostToWebhookAndWaitAsync(string webhookToken, ulong webhookId, HttpClient? client = null)
        => await PostToWebhookAndWaitAsync(GetPostUrl(webhookToken, webhookId), client);

    /// <summary>
    /// Posts a message to a Discord webhook and retrieves the unique message ID generated by Discord.
    /// </summary>
    /// <param name="webhookUrl">The full URL of the Discord webhook to post the message to.</param>
    /// <param name="client">Optional. The <see cref="HttpClient"/> instance to use for sending the HTTP request. If null, a new instance will be created.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the unique ID of the posted message.
    /// </returns>
    public async Task<ulong> PostToWebhookAndGetMessageIdAsync(string webhookUrl, HttpClient? client = null)
        => await ExtractMessageIdAsync(await PostToWebhookAndWaitAsync(webhookUrl, client));

    /// <summary>
    /// Posts a message to a Discord webhook and retrieves the message ID of the created message.
    /// </summary>
    /// <param name="webhookToken">The token of the Discord webhook.</param>
    /// <param name="webhookId">The unique identifier of the Discord webhook.</param>
    /// <param name="client">An optional HTTP client instance to be used for the request. If null, a default instance is created.</param>
    /// <returns>
    /// The unique identifier of the message created by the webhook.
    /// </returns>
    public async Task<ulong> PostToWebhookAndGetMessageIdAsync(string webhookToken, ulong webhookId, HttpClient? client = null)
        => await ExtractMessageIdAsync(await PostToWebhookAndWaitAsync(GetPostUrl(webhookToken, webhookId), client));

    /// <summary>
    /// Edits an existing Discord webhook message using the specified webhook token, webhook ID, and message ID.
    /// </summary>
    /// <param name="webhookToken">The token of the Discord webhook to authenticate the request.</param>
    /// <param name="webhookId">The unique identifier of the Discord webhook.</param>
    /// <param name="messageId">The unique identifier of the message to be edited.</param>
    /// <param name="client">Optional <see cref="HttpClient"/> instance to send the request. If not provided, a new instance will be created.</param>
    /// <returns>
    /// An HTTP response containing the result of the edit request.
    /// </returns>
    public async Task<HttpResponseMessage> EditWebhookAsync(string webhookToken, ulong webhookId, ulong messageId, HttpClient? client = null)
        => await EditWebhookAsync(GetEditUrl(webhookToken, webhookId, messageId), client);

    /// <summary>
    /// Sends the current Discord message to a specified webhook URL using an HTTP POST request.
    /// </summary>
    /// <param name="webhookUrl">The full URL of the webhook to which the message should be posted.</param>
    /// <param name="client">
    /// An optional <see cref="HttpClient"/> instance to use for the request. If not provided, a new one will be instantiated.
    /// </param>
    /// <returns>
    /// A <see cref="HttpResponseMessage"/> representing the response from the webhook.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="webhookUrl"/> is null or contains only whitespace.
    /// </exception>
    public async Task<HttpResponseMessage> PostToWebhookAsync(string webhookUrl, HttpClient? client = null)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentNullException(nameof(webhookUrl));

        if (client is null)
            client = new HttpClient();

        return await client.PostAsync(webhookUrl, ToWebhookHttpContent());
    }

    /// <summary>
    /// Sends the current message to a Discord webhook URL and waits for the server's response.
    /// </summary>
    /// <param name="webhookUrl">The URL of the Discord webhook to which the message will be posted.</param>
    /// <param name="client">An optional instance of <see cref="HttpClient"/> to be used for the request. If null, a new instance will be created.</param>
    /// <returns>
    /// An <see cref="HttpResponseMessage"/> instance containing the response from the Discord server.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="webhookUrl"/> is null, empty, or consists only of whitespace.</exception>
    public async Task<HttpResponseMessage> PostToWebhookAndWaitAsync(string webhookUrl, HttpClient? client = null)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentNullException(nameof(webhookUrl));

        if (client is null)
            client = new HttpClient();

        return await client.PostAsync($"{webhookUrl}?wait=true", ToWebhookHttpContent());
    }

    /// <summary>
    /// Edits a specific message sent to a Discord webhook using the specified webhook URL.
    /// </summary>
    public async Task<HttpResponseMessage> EditWebhookAsync(string webhookUrl, HttpClient? client = null)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentNullException(nameof(webhookUrl));

        if (client is null)
            client = new HttpClient();

        using (var request = new HttpRequestMessage(patchMethod, webhookUrl))
        {
            request.Content = ToWebhookHttpContent();
            return await client.SendAsync(request);
        }
    }

    /// <summary>
    /// Generates the URL required to edit a specific message sent to a Discord webhook.
    /// </summary>
    /// <param name="webhookToken">The unique token associated with the webhook.</param>
    /// <param name="webhookId">The unique ID of the webhook.</param>
    /// <param name="messageId">The unique ID of the message to be edited.</param>
    /// <returns>
    /// A string representing the URL used to edit the specified message via the Discord webhook API.
    /// </returns>
    public static string GetEditUrl(string webhookToken, ulong webhookId, ulong messageId) => $"https://discord.com/api/webhooks/{webhookId}/{webhookToken}/messages/{messageId}";

    /// <summary>
    /// Constructs the URL used to send a message to a Discord webhook.
    /// </summary>
    /// <param name="webhookToken">The token of the Discord webhook.</param>
    /// <param name="webhookId">The unique identifier of the Discord webhook.</param>
    /// <returns>
    /// A string representing the fully constructed webhook URL.
    /// </returns>
    public static string GetPostUrl(string webhookToken, ulong webhookId) => $"https://discord.com/api/webhooks/{webhookId}/{webhookToken}";

    /// <summary>
    /// Extracts the message ID from an HTTP response received after posting a Discord webhook message.
    /// </summary>
    /// <param name="responseMessage">The HTTP response message returned from the webhook post operation.</param>
    /// <returns>
    /// A ulong representing the extracted message ID from the response.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="responseMessage"/> is null.</exception>
    /// <exception cref="Exception">Thrown when the message ID could not be found in the response content.</exception>
    public static async Task<ulong> ExtractMessageIdAsync(HttpResponseMessage responseMessage)
    {
        if (responseMessage is null)
            throw new ArgumentNullException(nameof(responseMessage));

        responseMessage.EnsureSuccessStatusCode();

        var data = await responseMessage.Content.ReadAsStringAsync();
        var message = JsonConvert.DeserializeObject<JObject>(data);

        foreach (var pair in message)
        {
            if (pair.Key == "id")
            {
                return ulong.Parse(pair.Value.ToObject<string>());
            }
        }

        throw new Exception("Failed to find message ID node!");
    }

    /// <summary>
    /// Deserializes a JSON string into an instance of the <see cref="DiscordMessage"/> struct.
    /// </summary>
    /// <param name="json">The JSON string that represents a Discord message.</param>
    /// <returns>
    /// A <see cref="DiscordMessage"/> instance populated with the data from the provided JSON string.
    /// </returns>
    public static DiscordMessage? FromJson(string json) => JsonConvert.DeserializeObject<DiscordMessage>(json);
}