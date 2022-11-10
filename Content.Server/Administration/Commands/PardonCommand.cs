using System.Text;
using Content.Server.Database;
using Content.Server.DiscordWebhooks.Webhooks;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;


namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class PardonCommand : IConsoleCommand
    {
        public string Command => "pardon";
        public string Description => "Pardons somebody's ban";
        public string Help => $"Usage: {Command} <ban id>";

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

            var ban = await dbMan.GetServerBanAsync(banId);

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

            await dbMan.AddServerUnbanAsync(new ServerUnbanDef(banId, player?.UserId, DateTimeOffset.Now));

            shell.WriteLine($"Pardoned ban with id {banId}");

            if (ban.UserId is not { } userId)
                return;

            var target = await locator.LookupIdAsync(userId);
            SendWebhookMessage(player, banId, target?.Username);
        }

        private void SendWebhookMessage(IPlayerSession? admin, int banIdm, string? victim)
        {
            var banWebhook = new PardonMessageDiscordWebhook();
            var author = admin is not null ? admin.Name : "SERVER";
            var victimFormatted = victim is null ? "" : $" с {victim}";

            banWebhook.SendMessage(author, $"Снял бан #{banIdm}{victimFormatted}");
        }
    }
}
