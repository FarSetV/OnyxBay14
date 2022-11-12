using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Overmap.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Overmap.Systems;

public sealed class OvermapSystem : SharedOvermapSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private ISawmill _sawmill = default!;
    private OvermapTiles _tiles = new();

    public override void Initialize()
    {
        base.Initialize();

        DebugTools.Assert(OvermapTilesCount.X > 0);
        DebugTools.Assert(OvermapTilesCount.Y > 0);

        _sawmill = Logger.GetSawmill("overmap");
        _sawmill.Level = LogLevel.Info;

        SubscribeLocalEvent<OvermapObjectComponent, ComponentInit>(OnOvermapPointInitialized);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        CleanupTiles();
    }

    private void OnOvermapPointInitialized(EntityUid uid, OvermapObjectComponent objectComponent, ComponentInit args)
    {
        if (GetTileEntityOn(uid) is not null)
            return;

        var stationPos = new Vector2i(_random.Next(0, OvermapTilesCount.X), _random.Next(0, OvermapTilesCount.Y));
        _sawmill.Info($"trying to place an entity {ToPrettyString(uid)} at {stationPos}");
        AddToOvermap(uid, stationPos);
    }

    private void AddToOvermap(EntityUid entity, Vector2i position)
    {
        var xForm = Transform(entity);

        DebugTools.Assert(position.X < OvermapTilesCount.X || position.Y < OvermapTilesCount.Y,
            $"{position} is out of overmap's bounds");
        DebugTools.Assert(position.X >= 0 || position.Y >= 0, $"{position} is below zero");
        DebugTools.Assert(xForm.MapID != MapId.Nullspace,
            $"trying to place entity which is in nullspace: {ToPrettyString(entity)}");

        var tile = _tiles.GetByMapId(xForm.MapID);

        if (tile is null)
        {
            _sawmill.Info($"binding map {xForm.MapID} to {position.X}, {position.Y}");
            tile = _tiles.AddTile(position, xForm.MapID);
        }

        _sawmill.Info($"entity {ToPrettyString(entity)} placed at {tile.Position.X}, {tile.Position.Y}");
    }

    private void CleanupTiles()
    {
        _tiles = new OvermapTiles();
    }

    public OvermapTile? GetTileEntityOn(EntityUid entityUid)
    {
        var xForm = Transform(entityUid);

        return _tiles.GetByMapId(xForm.MapID);
    }

    public IEnumerable<EntityUid> GetOvermapEntities()
    {
        return GetOvermapObjects().Select(component => component.Owner);
    }

    public IEnumerable<OvermapObjectComponent> GetOvermapObjects()
    {
        return EntityQuery<OvermapObjectComponent>(true);
    }

    public MapId GetMapForTileOrCreate(Vector2i tilePosition)
    {
        DebugTools.Assert(tilePosition.X < OvermapTilesCount.X || tilePosition.Y < OvermapTilesCount.Y,
            $"{tilePosition} is out of overmap's bounds");
        DebugTools.Assert(tilePosition.X >= 0 || tilePosition.Y >= 0, $"{tilePosition} is below zero");

        if (_tiles.TryGetByPosition(tilePosition, out var tile))
            return tile.MapId;

        var mapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(mapId);
        _mapManager.SetMapPaused(mapId, true);
        tile = _tiles.AddTile(tilePosition, mapId);

        return tile.MapId;
    }
}
