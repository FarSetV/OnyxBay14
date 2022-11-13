using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Server.ServerUpdates;

/// <summary>
/// Responsible for restarting the server for update, when not disruptive.
/// </summary>
public sealed class ServerUpdateManager
{
    [Dependency] private readonly IBaseServer _server = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [ViewVariables]
    private bool _restartOnRoundEnd;

    public void Initialize()
    {
        _restartOnRoundEnd = _cfg.GetCVar(CCVars.HardRestart);
        _cfg.OnValueChanged(CCVars.HardRestart, OnValueChanged);
    }

    private void OnValueChanged(bool value)
    {
        _restartOnRoundEnd = value;
    }

    /// <summary>
    /// Notify that the round just ended, which is a great time to restart if necessary!
    /// </summary>
    /// <returns>True if the server is going to restart.</returns>
    public bool RoundEnded()
    {
        if (!_restartOnRoundEnd)
            return false;

        DoShutdown();
        return true;
    }


    private void DoShutdown()
    {
        _server.Shutdown(Loc.GetString("server-updates-shutdown"));
    }
}
