using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Bluespace.Events;

[Serializable]
[NetSerializable]
public sealed class BluespaceMapUpdatedEvent : EntityEventArgs
{
    public readonly MapId BluespaceMapId;

    public BluespaceMapUpdatedEvent(MapId mapId)
    {
        BluespaceMapId = mapId;
    }
}
