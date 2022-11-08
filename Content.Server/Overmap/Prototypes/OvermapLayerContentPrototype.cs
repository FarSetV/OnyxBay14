using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Overmap.Prototypes;

[Prototype("overmapLayerContent")]
public sealed class OvermapLayerContentPrototype : IPrototype
{
    [DataField("mapPath", required: true)] public ResourcePath MapPath { get; } = default!;

    [DataField("bounds", required: true)] public Box2 Bounds { get; } = Box2.UnitCentered;

    [DataField("uniquePerTile")] public bool UniquePerTile { get; }

    [DataField("unique")] public bool Unique { get; }

    [DataField("chance", required: true)] public float Chance { get; }

    [DataField("showOnOvermap")] public bool ShowOnOvermap { get; } = true;

    [IdDataField] public string ID { get; } = default!;
}
