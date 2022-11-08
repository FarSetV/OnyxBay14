using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Overmap;

public sealed class OvermapTile
{
    public readonly MapId MapId;
    public readonly Vector2i Position;

    public OvermapTile(Vector2i position, MapId mapId)
    {
        Position = position;
        MapId = mapId;
    }
}
