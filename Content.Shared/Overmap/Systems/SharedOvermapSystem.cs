namespace Content.Shared.Overmap.Systems;

public abstract class SharedOvermapSystem : EntitySystem
{
    // TODO: Move this to prototypes.
    /// <summary>
    ///     Count of tiles.
    /// </summary>
    public static readonly Vector2i OvermapTilesCount = new(12, 12);
}
