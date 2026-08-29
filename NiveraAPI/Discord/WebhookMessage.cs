namespace NiveraAPI.Discord;

/// <summary>
/// Represents a message to send to a Discord webhook.
/// </summary>
public struct WebhookMessage
{
    /// <summary>
    /// ID of the message to edit.
    /// </summary>
    public ulong EditId;
    
    /// <summary>
    /// Whether or not this message is an edit.
    /// </summary>
    public bool IsEdit;

    /// <summary>
    /// Whether or not the message ID should be captured.
    /// </summary>
    public bool CaptureId;
    
    /// <summary>
    /// The message to send.
    /// </summary>
    public DiscordMessage Message;

    /// <summary>
    /// Callback invoked after attempting to send or edit a message via the webhook.
    /// The first parameter represents any exception encountered during the operation,
    /// while the second parameter is the ID of the successfully sent or edited message, if applicable.
    /// </summary>
    public Action<DiscordMessage, Exception?, ulong?>? Callback;
    
    /// <summary>
    /// Creates a new WebhookMessage instance.
    /// </summary>
    public WebhookMessage(DiscordMessage message, bool captureId, bool isEdit, ulong editId)
    {
        Message = message;
        IsEdit = isEdit;
        EditId = editId;
        CaptureId = captureId;
    }
}