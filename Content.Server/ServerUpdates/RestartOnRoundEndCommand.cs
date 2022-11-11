using Content.Server.Administration;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.ServerUpdates;

[UsedImplicitly]
[AdminCommand(AdminFlags.Server)]
public sealed class RestartOnRoundEndCommand : LocalizedCommands
{
    public override string Command => "restartounroundend";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var willRestart = IoCManager.Resolve<ServerUpdateManager>().ToggleRestartOnRoundEnd();

        var response =
            Loc.GetString(willRestart
                ? "cmd-restartonroundend-will-restart"
                : "cmd-restartonroundend-will-not-restart");

        shell.WriteLine(response);
    }
}
