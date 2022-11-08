using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleNavigatorRadarBoundInterfaceState : BoundUserInterfaceState
{
    public OvermapNavigatorBoundInterfaceState? NavigatorState;
    public RadarConsoleBoundInterfaceState? RadarConsoleState;
}
