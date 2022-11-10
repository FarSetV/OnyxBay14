using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Content.Server.DiscordWebhooks;

public sealed class DiscordWebhooksManager : IDisposable
{
    // Max embed description length is 4096, according to https://discord.com/developers/docs/resources/channel#embed-object-embed-limits
    // Keep small margin, just to be safe
    public static readonly ushort DescriptionMax = 4000;

    // Maximum length a message can be before it is cut off
    // Should be shorter than DescriptionMax
    public static readonly ushort MessageLengthCap = 3000;

    private readonly ISawmill _sawmill;
    private readonly HttpClient _httpClient = new();

    public DiscordWebhooksManager()
    {
        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("discord_webhooks");
    }

    public async Task<HttpResponseMessage> SendAsync(string url, WebhookPayload payload)
    {
        var response = await _httpClient.PostAsync($"{url}?wait=true",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error($"Discord returned bad status code when posting message (perhaps the message is too long?): {response.StatusCode}\nResponse: {response}");
        }

        return response;
    }

    public async Task<HttpResponseMessage> PatchAsync(string url, string id, WebhookPayload payload)
    {
        var response =  await _httpClient.PatchAsync($"{url}/messages/{id}",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error($"Discord returned bad status code when posting message (perhaps the message is too long?): {response.StatusCode}\nResponse: {response}");
        }

        return response;
    }

    public async Task<WebhookData?> GetWebhookData(string url)
    {
        // Basic sanity check and capturing webhook ID and token
        var match = Regex.Match(url, @"^https://discord\.com/api/webhooks/(\d+)/((?!.*/).*)$");

        if (!match.Success)
        {
            // TODO: Ideally, CVar validation during setting should be better integrated
            _sawmill.Warning("Webhook URL does not appear to be valid. Using anyways...");
            return null;
        }

        if (match.Groups.Count <= 2)
        {
            _sawmill.Error("Could not get webhook ID or token.");
            return null;
        }

        var webhookId = match.Groups[1].Value;
        var webhookToken = match.Groups[2].Value;

        var response = await _httpClient.GetAsync($"https://discord.com/api/v10/webhooks/{webhookId}/{webhookToken}");

        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize<WebhookData>(content);

        _sawmill.Error($"Discord returned bad status code when trying to get webhook data (perhaps the webhook URL is invalid?): {response.StatusCode}\nResponse: {content}");
        return null;
    }

    public static async Task<string?> TryGetId(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)?["id"]?.ToString();
    }

    public static string ToDiscordTimeStamp(DateTimeOffset dateTimeOffset, string postfix = "t")
    {
        return $"<t:{dateTimeOffset.ToUnixTimeSeconds()}:{postfix}>";
    }

    public static string ToRoleMention(string roleId)
    {
        return $"<@&{roleId}>";
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
