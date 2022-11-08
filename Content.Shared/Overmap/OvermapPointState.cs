using Robust.Shared.Serialization;

namespace Content.Shared.Overmap;

[Serializable, NetSerializable]
public sealed class OvermapPointState
{
    public string? VisibleName;
    public EntityUid EntityUid;
    public Vector2i TilePosition;
    public bool InBluespace;
    public Color Color;
}
