namespace NiveraAPI.Discord.Mentions;

/// <summary>
/// Represents the type of mention to parse from the content.
/// </summary>
public class AllowedMentionsType
{
    private volatile string value = string.Empty;

    /// <summary>
    /// Represents a role mention.
    /// </summary>
    public static volatile AllowedMentionsType Role = new("Role");
    
    /// <summary>
    /// Represents a user mention.
    /// </summary>
    public static volatile AllowedMentionsType User = new("Users");

    /// <summary>
    /// Represents an @everyone mention.
    /// </summary>
    public static volatile AllowedMentionsType Everyone = new("Everyone");

    private AllowedMentionsType(string value)
        => this.value = value ?? throw new ArgumentNullException(nameof(value));

    /// <summary>
    /// Returns a string representation of the current instance. The string
    /// corresponds to the underlying value associated with the mention type.
    /// </summary>
    /// <returns>A string that represents the mention type.</returns>
    public override string ToString()
        => value;
}