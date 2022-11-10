using System.Text.Json.Serialization;

namespace Content.Server.DiscordWebhooks;

// https://discord.com/developers/docs/resources/channel#message-object-message-structure
public struct WebhookPayload
{
    [JsonPropertyName("username")] public string Username { get; set; } = "";

    [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; } = "";

    [JsonPropertyName("embeds")] public List<Embed>? Embeds { get; set; } = null;

    [JsonPropertyName("content")] public string? Content { get; set; } = null;

    [JsonPropertyName("allowed_mentions")]
    public Dictionary<string, string[]> AllowedMentions { get; set; } =
        new()
        {
            { "parse", Array.Empty<string>() }
        };

    public WebhookPayload()
    {
    }
}

// https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
public struct Embed
{
    [JsonPropertyName("description")] public string Description { get; set; } = "";

    [JsonPropertyName("color")] public int Color { get; set; } = 0;

    [JsonPropertyName("footer")] public EmbedFooter? Footer { get; set; } = null;

    public Embed()
    {
    }
}

// https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
public struct EmbedFooter
{
    [JsonPropertyName("text")] public string Text { get; set; } = "";

    [JsonPropertyName("icon_url")] public string IconUrl { get; set; } = "";

    public EmbedFooter()
    {
    }
}

// https://discord.com/developers/docs/resources/webhook#webhook-object-webhook-structure
public struct WebhookData
{
    [JsonPropertyName("guild_id")] public string? GuildId { get; set; } = null;

    [JsonPropertyName("channel_id")] public string? ChannelId { get; set; } = null;

    public WebhookData()
    {
    }
}
