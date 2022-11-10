using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.DiscordWebhooks.Webhooks;

public sealed class OOCMessageDiscordWebhook : GenericMessageDiscordWebhook
{
    public override CVarDef<string> Webhook => CCVars.DiscordOOCWebhook;
    public override string Prefix => "OOC";
    // Blue
    public override int Color => 3447003;
}
