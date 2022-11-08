using Content.Shared.Overmap;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedOvermapNavigatorSystem))]
public sealed class OvermapNavigatorComponent : Component
{
    [DataField("signatureRadius")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float SignatureRadius = 4_000f;

    [DataField("IFFRadius")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float IFFRadius = 1_000f;

    [DataField("points")]
    public List<OvermapPointState> Points = new();
}

[Serializable, NetSerializable]
public sealed class OvermapNavigatorComponentState : ComponentState
{
    public float SignatureRadius;
    public float FFIRadius;
    public List<OvermapPointState> Points = new();
}

public sealed class OvermapNavigatorUpdated : EventArgs
{
    public readonly OvermapNavigatorComponent Component;

    public OvermapNavigatorUpdated(OvermapNavigatorComponent component)
    {
        Component = component;
    }
}
