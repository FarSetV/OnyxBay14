using Content.Shared.Overmap;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public class OvermapNavigatorBoundInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    ///     Parent grid of the navigator's entity
    /// </summary>
    public readonly EntityUid ParentGrid;

    public readonly BluespaceState? BluespaceState;
    public readonly float SignatureRadius;
    public readonly float IFFRadius;
    public readonly List<OvermapPointState> OvermapPoints;

    public OvermapNavigatorBoundInterfaceState(EntityUid parentGrid, BluespaceState? bluespaceState, float signatureRadius, float iffRadius, List<OvermapPointState> overmapPoints)
    {
        ParentGrid = parentGrid;
        BluespaceState = bluespaceState;
        SignatureRadius = signatureRadius;
        IFFRadius = iffRadius;
        OvermapPoints = overmapPoints;
    }
}
