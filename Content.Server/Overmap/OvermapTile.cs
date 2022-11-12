using Content.Shared.Overmap;
using Robust.Shared.Map;

namespace Content.Server.Overmap;

public sealed class OvermapTile : SharedOvermapTile
{
    public readonly Matrix3 InvWorldMatrix;
    public readonly MapId MapId;
    public readonly Vector2i Position;
    public readonly Matrix3 WorldMatrix;

    public OvermapTile(Vector2i position, MapId mapId)
    {
        Position = position;
        MapId = mapId;
        WorldMatrix = GetWorldMatrix(Position);
        InvWorldMatrix = GetInvWorldMatrix(Position);
    }
}
