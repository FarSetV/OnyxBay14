using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Overmap.Prototypes;

[Prototype("overmapPreset")]
public sealed class OvermapPresetPrototype : IPrototype
{
    [DataField("layers", required: true,
        customTypeSerializer: typeof(PrototypeIdListSerializer<OvermapLayerPrototype>))]
    public List<string> Layers { get; } = new();

    [IdDataField] public string ID { get; } = default!;
}
