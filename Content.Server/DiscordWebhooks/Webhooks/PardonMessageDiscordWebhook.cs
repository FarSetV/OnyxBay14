using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.DiscordWebhooks.Webhooks;

public sealed class PardonMessageDiscordWebhook : GenericMessageDiscordWebhook
{
    public override CVarDef<string> Webhook => CCVars.DiscordBanWebhook;
    public override string Prefix => "UNBAN";
    // Green
    public override int Color => 5763719;
}
