using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Overmap.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Overmap;

public sealed class OvermapTiles
{
    private readonly Dictionary<Vector2i, OvermapTile> _tiles = new();

    public OvermapTile AddTile(Vector2i position, MapId mapId)
    {
        DebugTools.Assert(!_tiles.ContainsKey(position));

        if (_tiles.TryGetValue(position, out var tile))
            return tile;

        tile = new OvermapTile(position, mapId);
        _tiles.Add(position, tile);

        return tile;
    }

    public OvermapTile? GetByMapId(MapId mapId)
    {
        return _tiles.Values.FirstOrDefault(tile => tile.MapId == mapId);
    }

    public OvermapTile? GetByPosition(Vector2i position)
    {
        return _tiles.GetValueOrDefault(position);
    }

    public bool TryGetByPosition(Vector2i position, [NotNullWhen(true)] out OvermapTile? tile)
    {
        return _tiles.TryGetValue(position, out tile);
    }

    public bool TryGetByMapId(MapId mapId, [NotNullWhen(true)] out OvermapTile? tile)
    {
        tile = GetByMapId(mapId);

        return tile is not null;
    }
}
