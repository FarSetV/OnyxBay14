﻿using System.Text;
using Content.Server.Database;
using Content.Server.DiscordWebhooks.Webhooks;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class RoleUnbanCommand : IConsoleCommand
{
    public string Command => "roleunban";
    public string Description => Loc.GetString("cmd-roleunban-desc");
    public string Help => Loc.GetString("cmd-roleunban-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player as IPlayerSession;
        var dbMan = IoCManager.Resolve<IServerDbManager>();
        var locator = IoCManager.Resolve<IPlayerLocator>();

        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!int.TryParse(args[0], out var banId))
        {
            shell.WriteLine($"Unable to parse {args[1]} as a ban id integer.\n{Help}");
            return;
        }

        var ban = await dbMan.GetServerRoleBanAsync(banId);

        if (ban == null)
        {
            shell.WriteLine($"No ban found with id {banId}");
            return;
        }

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");

            shell.WriteLine(response.ToString());
            return;
        }

        await dbMan.AddServerRoleUnbanAsync(new ServerRoleUnbanDef(banId, player?.UserId, DateTimeOffset.Now));

        shell.WriteLine($"Pardoned ban with id {banId}");

        var target = await locator.LookupIdAsync(ban.UserId!.Value);
        SendWebhookMessage(player, banId, target?.Username);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // Can't think of good way to do hint options for this
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-roleunban-hint-1")),
            _ => CompletionResult.Empty
        };
    }

    private void SendWebhookMessage(IPlayerSession? admin, int banIdm, string? victim)
    {
        var banWebhook = new PardonMessageDiscordWebhook();
        var author = admin is not null ? admin.Name : "SERVER";

        var message = new StringBuilder();

        message.Append("\nСнят джоббан\n");
        message.Append($"**От:** {author}");

        if (victim is not null)
            message.Append($"**Игрок:** {victim}\n");

        message.Append($"**ID:** {banIdm}");

        banWebhook.SendMessage(message.ToString());
    }
}
