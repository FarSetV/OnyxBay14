using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.DiscordWebhooks.Webhooks;

public sealed class EmoteMessageDiscordWebhook : GenericMessageDiscordWebhook
{
    public override CVarDef<string> Webhook => CCVars.DiscordEmoteWebhook;
    public override string Prefix => "EMOTE:";

    // Green
    public override int Color => 5763719;
}
