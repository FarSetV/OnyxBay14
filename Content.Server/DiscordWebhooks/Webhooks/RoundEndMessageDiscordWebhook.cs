using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.DiscordWebhooks.Webhooks;

public sealed class RoundEndMessageDiscordWebhook : GenericMessageDiscordWebhook
{
    public override CVarDef<string> Webhook => CCVars.DiscordRoundEndWebhook;
}
