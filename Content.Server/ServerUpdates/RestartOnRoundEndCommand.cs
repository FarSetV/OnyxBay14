using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.ServerUpdates;

[UsedImplicitly]
[AdminCommand(AdminFlags.Server)]
public sealed class RestartOnRoundEndCommand : LocalizedCommands
{
    public override string Command => "restartounroundend";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();
        cfg.SetCVar(CCVars.HardRestart, !cfg.GetCVar(CCVars.HardRestart));
        var willRestart = cfg.GetCVar(CCVars.HardRestart);

        var response =
            Loc.GetString(willRestart
                ? "cmd-restartonroundend-will-restart"
                : "cmd-restartonroundend-will-not-restart");

        shell.WriteLine(response);
    }
}
