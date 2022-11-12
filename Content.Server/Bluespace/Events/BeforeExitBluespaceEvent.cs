using Robust.Shared.Map;

namespace Content.Server.Bluespace.Events;

public sealed class BeforeExitBluespaceEvent : EntityEventArgs
{
    public readonly Vector2 NewPosition;
    public readonly MapId NewMap;
    public readonly EntityUid EntityUid;

    public BeforeExitBluespaceEvent(EntityUid entityUid, Vector2 newPosition, MapId newMap)
    {
        NewPosition = newPosition;
        NewMap = newMap;
        EntityUid = entityUid;
    }
}
