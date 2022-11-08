using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Overmap;

public abstract class SharedOvermapSystem : EntitySystem
{
    // TODO: Move this to prototypes.
    /// <summary>
    ///     Count of tiles.
    /// </summary>
    public static readonly Vector2i OvermapTilesCount = new(12, 12);

    /// <summary>
    ///     Tile's size.
    /// </summary>
    public static readonly float OvermapTileSize = 4_000f;

    public static readonly float ScaleFactor = 0.5f;

    public static Vector2 OvermapBluespaceSize =>
        new(OvermapTilesCount.X * OvermapTileSize * ScaleFactor, OvermapTilesCount.Y * OvermapTileSize * ScaleFactor);

    public MapId? BluespaceMapId { get; protected set; }
}

[Serializable]
[NetSerializable]
public sealed class BluespaceMapUpdatedMessage : EntityEventArgs
{
    public readonly MapId NewId;

    public BluespaceMapUpdatedMessage(MapId mapId)
    {
        NewId = mapId;
    }
}
