using Content.Shared.CCVar;
using Robust.Server;
using Robust.Shared.Configuration;

namespace Content.Server.ServerUpdates;

/// <summary>
/// Responsible for restarting the server for update, when not disruptive.
/// </summary>
public sealed class ServerUpdateManager
{
    [Dependency] private readonly IBaseServer _server = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _restartOnRoundEnd;
    private bool _loopCheck;

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
        if (!_loopCheck)
        {
            _loopCheck = true;
            return false;
        }

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
