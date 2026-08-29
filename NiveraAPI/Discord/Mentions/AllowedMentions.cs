using Newtonsoft.Json;

namespace NiveraAPI.Discord.Mentions;

/// <summary>
/// Represents the allowed mentions for a Discord message.
/// </summary>
public class AllowedMentions
{
    /// <summary>
    /// The types of mentions to parse from the content.
    /// </summary>
    [JsonProperty("parse")] 
    public volatile string[] Parsable = [];

    /// <summary>
    /// The IDs of roles to mention.
    /// </summary>
    [JsonProperty("roles")] 
    public volatile ulong[] RoleIds = [];

    /// <summary>
    /// The IDs of users to mention.
    /// </summary>
    [JsonProperty("users")] 
    public volatile ulong[] UserIds = [];

    /// <summary>
    /// Whether to mention the author of the message being replied to.
    /// </summary>
    [JsonProperty("replied_user")]
    public volatile bool MentionRepliedUser;

    /// <summary>
    /// Adds the specified role IDs to the list of roles to mention.
    /// </summary>
    /// <param name="roles">The IDs of the roles to add to the mention list.</param>
    /// <returns>The updated <see cref="AllowedMentions"/> instance.</returns>
    public AllowedMentions WithRoles(params ulong[] roles)
    {
        if (RoleIds != null && RoleIds.Any())
        {
            var list = new List<ulong>(RoleIds);

            list.AddRange(roles);

            RoleIds = list.ToArray();
            return this;
        }

        RoleIds = roles;
        return this;
    }

    /// <summary>
    /// Adds the specified user IDs to the list of users to mention.
    /// </summary>
    /// <param name="users">The IDs of the users to add to the mention list.</param>
    /// <returns>The updated <see cref="AllowedMentions"/> instance.</returns>
    public AllowedMentions WithUsers(params ulong[] users)
    {
        if (UserIds != null && UserIds.Any())
        {
            var list = new List<ulong>(UserIds);

            list.AddRange(users);

            RoleIds = list.ToArray();
            return this;
        }

        UserIds = users;
        return this;
    }

    /// <summary>
    /// Specifies whether to mention the author of the message being replied to.
    /// </summary>
    /// <param name="repliedUser">A boolean value indicating whether to mention the author of the replied message. Defaults to true.</param>
    /// <returns>The updated <see cref="AllowedMentions"/> instance.</returns>
    public AllowedMentions WithRepliedUser(bool repliedUser = true)
    {
        MentionRepliedUser = repliedUser;
        return this;
    }

    /// <summary>
    /// Adds the specified mention type to the list of parsable mention types.
    /// </summary>
    /// <param name="discordMessageAllowedMentionsType">The mention type to add to the parsable list.</param>
    /// <returns>The updated <see cref="AllowedMentions"/> instance.</returns>
    public AllowedMentions WithParsable(AllowedMentionsType discordMessageAllowedMentionsType)
    {
        Parsable = (Parsable ?? []).Concat([discordMessageAllowedMentionsType.ToString()]).ToArray();
        return this;
    }

    /// <summary>
    /// Clears the list of mention types that are being parsed from the message content.
    /// </summary>
    /// <returns>The updated <see cref="AllowedMentions"/> instance with an empty list of parsable mention types.</returns>
    public AllowedMentions ClearParsable()
    {
        Parsable = Array.Empty<string>();
        return this;
    }

    /// <summary>
    /// Removes all user IDs from the list of users to mention.
    /// </summary>
    /// <returns>The updated <see cref="AllowedMentions"/> instance.</returns>
    public AllowedMentions ClearUsers()
    {
        UserIds = Array.Empty<ulong>();
        return this;
    }

    /// <summary>
    /// Removes all role IDs from the list of roles to mention.
    /// </summary>
    /// <returns>The updated <see cref="AllowedMentions"/> instance.</returns>
    public AllowedMentions ClearRoles()
    {
        RoleIds = Array.Empty<ulong>();
        return this;
    }
}