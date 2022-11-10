using System.Text;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.DiscordWebhooks.Webhooks;

public abstract class GenericMessageDiscordWebhook
{
    public virtual CVarDef<string> Webhook => default!;
    public virtual int Color => 0;
    public virtual string Prefix => string.Empty;

    [Dependency] private readonly DiscordWebhooksManager _discord = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private string _webhookUrl = string.Empty;
    private TimeSpan _lastMessageTime = TimeSpan.Zero;
    private readonly TimeSpan _maxPatchTime = TimeSpan.FromSeconds(10);
    private readonly StringBuilder _lastMessage = new();
    private string? _lastPatchId;

    protected GenericMessageDiscordWebhook()
    {
        Initialize();
    }

    private void Initialize()
    {
        IoCManager.InjectDependencies(this);

        _webhookUrl = _cfg.GetCVar(Webhook);
        _cfg.OnValueChanged(Webhook, OnWebhookChanged);
    }

    private void OnWebhookChanged(string value)
    {
        _webhookUrl = value;
    }

    public async void SendMessage(string sender, string message)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        var doPatch = _timing.CurTime - _lastMessageTime <= _maxPatchTime;
        _lastMessageTime = _timing.CurTime;

        var messageBuilder = new StringBuilder();
        var prefix = Prefix;

        if (!string.IsNullOrEmpty(prefix))
        {
            messageBuilder.Append($"{prefix}: ");
        }

        messageBuilder.Append(DiscordWebhooksManager.ToDiscordTimeStamp(DateTimeOffset.Now));
        messageBuilder.Append($" **{sender}:** ");
        messageBuilder.Append(message);

        var formattedMessage = messageBuilder.ToString();
        messageBuilder.Clear();

        {
            bool tryAgain;
            do
            {
                if (_lastMessage.Length + formattedMessage.Length <= DiscordWebhooksManager.MessageLengthCap)
                {
                    break;
                }

                var cap = _lastMessage.Length + formattedMessage.Length - DiscordWebhooksManager.MessageLengthCap;

                if (formattedMessage.Length - cap <= 0)
                {
                    _lastMessage.Clear();
                    doPatch = false;
                    tryAgain = true;
                    continue;
                }

                formattedMessage = formattedMessage[0..cap];
                break;
            } while (tryAgain);
        }

        if (!doPatch)
        {
            _lastMessage.Clear();
            _lastMessage.Append(formattedMessage);
        }
        else
        {
            _lastMessage.Append('\n');
            _lastMessage.Append(formattedMessage);
        }

        var payload = new WebhookPayload
        {
            Embeds = new()
            {
                new Embed
                {
                    Description = _lastMessage.ToString(),
                    Color = Color
                }
            }
        };

        if (doPatch && _lastPatchId is not null)
        {
            await _discord.PatchAsync(_webhookUrl, _lastPatchId, payload);
        }
        else
        {
            var response = await _discord.SendAsync(_webhookUrl, payload);
            _lastPatchId = await DiscordWebhooksManager.TryGetId(response);
        }
    }
}
