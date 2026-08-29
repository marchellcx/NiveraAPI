using NiveraAPI.Logs;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

namespace NiveraAPI.Discord;

/// <summary>
/// Represents a client for sending and managing messages through a Discord webhook.
/// </summary>
/// <remarks>
/// The <see cref="WebhookClient"/> class provides functionality to post new messages,
/// edit existing ones, and manage webhook credentials. It supports internal
/// message queuing and ensures proper disposal of resources.
/// </remarks>
public class WebhookClient : DisposableBase
{
    private LogSink log = LogManager.GetSource("Discord", "WebhookClient");
    private WebhookCredentials creds;

    /// <summary>
    /// The credentials used to authenticate the webhook.
    /// </summary>
    public WebhookCredentials Credentials => creds;
    
    /// <summary>
    /// The queue of messages waiting to be sent through the webhook.
    /// </summary>
    public Queue<WebhookMessage> Queue { get; } = new();

    /// <summary>
    /// Provides access to the logging sink used for logging messages and events
    /// related to the Discord webhook client.
    /// </summary>
    public LogSink Log => log;

    /// <summary>
    /// Creates a new instance of the <see cref="WebhookClient"/> class.
    /// </summary>
    public WebhookClient(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentNullException(nameof(webhookUrl));

        creds = ExtractWebhookCredentials(webhookUrl);
        
        log.Info($"Created a new WebhookClient for webhook &1{creds.Id}&r (token &6{creds.Token}&r)");
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="WebhookClient"/> class.
    /// </summary>
    /// <remarks>
    /// This method ensures that the internal HTTP client and the message queue are properly disposed.
    /// If an internally owned HTTP client was created, it will also be disposed during this process.
    /// </remarks>
    public override void Dispose()
    {
        CheckDisposed();
        
        Queue.Clear();
    }

    /// <summary>
    /// Edits an existing message sent through the webhook.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message to be edited.</param>
    /// <param name="newMessage">The updated content of the message as a <see cref="DiscordMessage"/>.</param>
    /// <param name="captureId">Indicates whether to capture the message ID after editing.</param>
    /// <param name="callback">An optional callback to handle the result of the operation, providing the edited message, an exception if any, and the message ID if captured.</param>
    public void Edit(ulong messageId, DiscordMessage newMessage, bool captureId, Action<DiscordMessage, Exception?, ulong?>? callback = null)
        => Queue.Enqueue(new WebhookMessage(newMessage, captureId, true, messageId) { Callback = callback });

    /// <summary>
    /// Posts a new message to the Discord webhook queue for processing.
    /// </summary>
    /// <param name="message">The <see cref="DiscordMessage"/> object containing the content and any associated metadata to send.</param>
    /// <param name="captureId">Indicates whether the message ID should be captured after the message is sent.</param>
    /// <param name="callback">An optional callback to be executed after the message is processed, providing the sent <see cref="DiscordMessage"/>, any exception encountered, and the message ID if <paramref name="captureId"/> is true.</param>
    public void Post(DiscordMessage message, bool captureId, Action<DiscordMessage, Exception?, ulong?>? callback = null)
        => Queue.Enqueue(new WebhookMessage(message, captureId, false, 0) { Callback = callback });
    
    /// <summary>
    /// Attempts to extract both the webhook ID and token from a Discord webhook URL.
    /// </summary>
    /// <param name="webhookUrl">The full Discord webhook URL.</param>
    /// <param name="credentials">
    /// When this method returns <see langword="true"/>, contains the extracted ID and token.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if both the ID and token were successfully extracted; otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryExtractWebhookCredentials(string? webhookUrl, out WebhookCredentials credentials)
    {
        credentials = default!;

        if (string.IsNullOrWhiteSpace(webhookUrl))
            return false;

        try
        {
            credentials = ExtractWebhookCredentials(webhookUrl!);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Extracts both the webhook ID (UInt64) and token from a Discord webhook URL.
    /// Supported formats:
    ///   https://discord.com/api/webhooks/{id}/{token}
    ///   https://discordapp.com/api/webhooks/{id}/{token}
    ///   https://discord.com/api/v10/webhooks/{id}/{token}
    ///   (query parameters such as ?wait=true or ?thread_id=... are ignored)
    /// </summary>
    /// <param name="webhookUrl">The full Discord webhook URL.</param>
    /// <returns>A <see cref="WebhookCredentials"/> containing the ID and token.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the URL is null/empty, not a valid absolute URI,
    /// or does not contain a valid /webhooks/{id}/{token} structure.
    /// </exception>
    public static WebhookCredentials ExtractWebhookCredentials(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentException("Webhook URL cannot be null or empty.", nameof(webhookUrl));

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("The provided string is not a valid absolute URL.", nameof(webhookUrl));
        
        // Split path into segments, ignoring empty entries caused by leading/trailing slashes
        if (!uri.AbsolutePath.TrySplit('/', true, null, out var segments))
            throw new ArgumentException("The URL does not appear to be a valid Discord webhook URL " + "(expected /webhooks/{id}/{token}).", nameof(webhookUrl));

        // Find the "webhooks" segment (case-insensitive)
        var webhooksIndex = Array.FindIndex(segments, s => s.Equals("webhooks", StringComparison.OrdinalIgnoreCase));

        if (webhooksIndex < 0 || webhooksIndex + 2 >= segments.Length)
            throw new ArgumentException("The URL does not appear to be a valid Discord webhook URL " + "(expected /webhooks/{id}/{token}).", nameof(webhookUrl));

        var idSegment = segments[webhooksIndex + 1];
        var tokenSegment = segments[webhooksIndex + 2];

        if (!ulong.TryParse(idSegment, out var webhookId))
            throw new ArgumentException(
                $"The webhook ID segment '{idSegment}' is not a valid UInt64.",
                nameof(webhookUrl));

        if (string.IsNullOrWhiteSpace(tokenSegment))
            throw new ArgumentException("The webhook token segment is empty.", nameof(webhookUrl));

        var creds = new WebhookCredentials();
        
        Volatile.Write(ref creds.Id, webhookId);

        creds.Token = tokenSegment;
        return creds;
    }
}