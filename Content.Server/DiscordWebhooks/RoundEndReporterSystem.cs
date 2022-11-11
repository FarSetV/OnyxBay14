using System.Text;
using Content.Server.DiscordWebhooks.Webhooks;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.DiscordWebhooks;

public sealed class RoundEndReporterSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private RoundEndMessageDiscordWebhook _roundEndWebhook = default!;
    private string _mentionRole = string.Empty;

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.DiscordRoundEndMention, OnMentionRoleChanged);
    }

    public override void Initialize()
    {
        base.Initialize();

        _mentionRole = _cfg.GetCVar(CCVars.DiscordRoundEndMention);
        _cfg.OnValueChanged(CCVars.DiscordRoundEndMention, OnMentionRoleChanged);

        _roundEndWebhook = new RoundEndMessageDiscordWebhook();
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void OnMentionRoleChanged(string value)
    {
        _mentionRole = value;
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        if (!string.IsNullOrEmpty(_mentionRole))
            _roundEndWebhook.SendMention(_mentionRole);

        var message = new StringBuilder();

        message.Append($"\nРаунд #{ev.RoundId} закончился\n");
        message.Append($"Режим: {ev.GamemodeTitle}\n");
        message.Append($"Игроков: {_playerManager.PlayerCount}\n");
        message.Append($"Продолжительность: {ev.RoundDuration}");

        _roundEndWebhook.SendMessage(message.ToString(), false);
    }
}
