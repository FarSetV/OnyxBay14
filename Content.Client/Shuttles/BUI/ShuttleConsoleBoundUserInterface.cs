using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class ShuttleConsoleBoundUserInterface : BoundUserInterface
{
    private ShuttleConsoleWindow? _window;
    private RadarTab _radarTab => _window!.ControlRadarTab;
    private OvermapTab _overmapTab => _window!.ControlOvermapTab;

    public ShuttleConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = new ShuttleConsoleWindow();
        _radarTab.UndockPressed += OnUndockPressed;
        _radarTab.StartAutodockPressed += OnAutodockPressed;
        _radarTab.StopAutodockPressed += OnStopAutodockPressed;
        _overmapTab.BluespaceEnterPressed += OnBluespaceEnterPressed;
        _overmapTab.BluespaceExitPressed += OnBluespaceExitPressed;
        _window.OpenCentered();
        _window.OnClose += OnClose;
    }

    private void OnClose()
    {
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }

    private void OnBluespaceEnterPressed()
    {
        SendMessage(new EnterBluespaceMessage());
    }

    private void OnBluespaceExitPressed()
    {
        SendMessage(new ExitBluespaceMessage());
    }

    private void OnStopAutodockPressed(EntityUid obj)
    {
        SendMessage(new StopAutodockRequestMessage {DockEntity = obj});
    }

    private void OnAutodockPressed(EntityUid obj)
    {
        SendMessage(new AutodockRequestMessage {DockEntity = obj});
    }

    private void OnUndockPressed(EntityUid obj)
    {
        SendMessage(new UndockRequestMessage {DockEntity = obj});
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ShuttleNavigatorRadarBoundInterfaceState cState)
            return;

        if (_window is null)
            return;

        _window.UpdateState(cState);
    }
}
