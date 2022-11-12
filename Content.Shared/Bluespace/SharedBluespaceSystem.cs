using Content.Shared.Overmap;
using Content.Shared.Overmap.Systems;
using Robust.Shared.Map;

namespace Content.Shared.Bluespace;

public abstract class SharedBluespaceSystem : EntitySystem
{
    private const float Scale = 0.5f;
    public static Matrix3 ScaleMatrix = Matrix3.CreateScale(Scale, Scale);
    public static Matrix3 InvertScaleMatrix = ScaleMatrix.Invert();
    public static Vector2 OvermapBluespaceSize =>
        new(SharedOvermapSystem.OvermapTilesCount.X * SharedOvermapTile.TileSize * Scale, SharedOvermapSystem.OvermapTilesCount.Y * SharedOvermapTile.TileSize * Scale);
    protected MapId? BluespaceMapId { get; set; }

    public bool IsEntityInBluespace(EntityUid entity, TransformComponent? xForm = null)
    {
        if (!Resolve(entity, ref xForm))
            return false;

        return xForm.MapID == BluespaceMapId;
    }
}
