using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Overmap.Prototypes;

[Prototype("overmapLayer")]
public sealed class OvermapLayerPrototype : IPrototype
{
    [DataField("frequency", required: true)]
    public float Frequency;

    [DataField("lacunarity", required: true)]
    public float Lacunarity;

    [DataField("octaves", required: true)] public uint Octaves;

    [DataField("persistence", required: true)]
    public float Persistence;

    [DataField("grids", required: true,
        customTypeSerializer: typeof(PrototypeIdListSerializer<OvermapLayerContentPrototype>))]
    public List<string> Grids { get; } = new();

    [DataField("minDensity", required: true)]
    public uint MinDensity { get; }

    [DataField("minNoise")] public float MinNoise { get; }

    [DataField("noiseType")] public string NoiseType { get; } = "Fbm";

    [IdDataField] public string ID { get; } = default!;
}
