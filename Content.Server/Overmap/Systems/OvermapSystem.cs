using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Overmap;
using Content.Shared.Parallax;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Overmap.Systems;

public sealed class OvermapSystem : SharedOvermapSystem
{
    // TODO: Pause maps?
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private readonly Dictionary<Vector2i, OvermapTile> _tiles = new();
    private ISawmill _sawmill = default!;

    public IEnumerable<OvermapTile> Tiles => _tiles.Values;

    public override void Initialize()
    {
        base.Initialize();

        DebugTools.Assert(OvermapTilesCount.X > 0);
        DebugTools.Assert(OvermapTilesCount.Y > 0);

        _sawmill = Logger.GetSawmill("overmap");
        _sawmill.Level = LogLevel.Info;

        SubscribeLocalEvent<OvermapObjectComponent, ComponentInit>(OnOvermapPointInitialized);
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

        var tile = Tiles.FirstOrDefault(t => t.MapId == xForm.MapID);

        if (tile is null)
        {
            _sawmill.Info($"binding map {xForm.MapID} to {position.X}, {position.Y}");
            tile = new OvermapTile(position, xForm.MapID);
            _tiles[position] = tile;
        }

        _sawmill.Info($"entity {ToPrettyString(entity)} placed at {tile.Position.X}, {tile.Position.Y}");
    }

    public void SetupBluespaceMap()
    {
        if (BluespaceMapId is not null)
            return;

        BluespaceMapId = _mapManager.CreateMap();
        _sawmill.Info($"created bluespace map: {BluespaceMapId}");
        DebugTools.Assert(!_mapManager.IsMapPaused(BluespaceMapId.Value));

        var parallax = EnsureComp<ParallaxComponent>(_mapManager.GetMapEntityId(BluespaceMapId.Value));
        parallax.Parallax = "FastSpace";

        var msg = new BluespaceMapUpdatedMessage(BluespaceMapId.Value);
        RaiseNetworkEvent(msg);
    }

    public OvermapTile? MapIdToTile(MapId mapId)
    {
        return Tiles.FirstOrDefault(tile => tile.MapId == mapId);
    }

    public OvermapTile? GetTileEntityOn(EntityUid entityUid)
    {
        var xForm = Transform(entityUid);

        return MapIdToTile(xForm.MapID);
    }

    public IEnumerable<EntityUid> GetOvermapEntities()
    {
        return GetOvermapObjects().Select(component => component.Owner);
    }

    public IEnumerable<OvermapObjectComponent> GetOvermapObjects()
    {
        return EntityQuery<OvermapObjectComponent>();
    }

    public Vector2? LocalPositionToBluespace(EntityUid entity)
    {
        var tile = GetTileEntityOn(entity);

        if (tile is null)
            return null;

        var xForm = Transform(entity);
        var halfSize = OvermapTileSize / 2f;
        var localPosition = new Vector2(
            Math.Clamp(xForm.WorldPosition.X, -halfSize, halfSize),
            Math.Clamp(xForm.WorldPosition.Y, -halfSize, halfSize)
        );

        return new Vector2(
            (OvermapTileSize * tile.Position.X + halfSize + localPosition.X) * ScaleFactor,
            (OvermapTileSize * tile.Position.Y + halfSize + localPosition.Y) * ScaleFactor
        );
    }

    public Vector2 BluespacePositionToTilePosition(EntityUid entity)
    {
        var xForm = Transform(entity);
        return BluespacePositionToTilePosition(xForm.WorldPosition);
    }

    public Vector2 BluespacePositionToTilePosition(Vector2 position)
    {
        var bluespaceSize = OvermapBluespaceSize;
        var worldPosition = new Vector2(
            Math.Clamp(position.X, 0, bluespaceSize.X),
            Math.Clamp(position.Y, 0, bluespaceSize.Y)
        );
        var tilePosition = worldPosition / ScaleFactor / OvermapTileSize;

        tilePosition.X = Math.Clamp(tilePosition.X, 0, OvermapTilesCount.X);
        tilePosition.Y = Math.Clamp(tilePosition.Y, 0, OvermapTilesCount.Y);

        return tilePosition;
    }

    public Vector2 BluespacePositionToLocalPosition(EntityUid entity, Vector2? tilePosition = null)
    {
        var xForm = Comp<TransformComponent>(entity);

        return BluespacePositionToLocalPosition(xForm.WorldPosition, tilePosition);
    }

    public Vector2 BluespacePositionToLocalPosition(Vector2 position, Vector2? tilePosition = null)
    {
        tilePosition ??= BluespacePositionToTilePosition(position);
        var halfSize = OvermapTileSize / 2f;
        var normalized = new Vector2(
            (float) (tilePosition.Value.X - Math.Truncate(tilePosition.Value.X)),
            (float) (tilePosition.Value.Y - Math.Truncate(tilePosition.Value.Y))
        );

        return new Vector2(OvermapTileSize * normalized.X - halfSize, OvermapTileSize * normalized.Y - halfSize);
    }

    public MapId GetMapForTileOrCreate(Vector2i tilePosition)
    {
        DebugTools.Assert(tilePosition.X < OvermapTilesCount.X || tilePosition.Y < OvermapTilesCount.Y,
            $"{tilePosition} is out of overmap's bounds");
        DebugTools.Assert(tilePosition.X >= 0 || tilePosition.Y >= 0, $"{tilePosition} is below zero");

        if (!_tiles.TryGetValue(tilePosition, out var tile))
        {
            _tiles[tilePosition] = new OvermapTile(tilePosition, _mapManager.CreateMap());
            tile = _tiles[tilePosition];
        }

        return tile.MapId;
    }

    public bool IsEntityInBluespace(EntityUid entity, TransformComponent? xForm = null)
    {
        if (!Resolve(entity, ref xForm))
            return false;

        return xForm.MapID == BluespaceMapId;
    }

    /// <summary>
    ///     Returns useful information about the exit point.
    /// </summary>
    /// <param name="uid">The entity that wants to exit the bluespace.</param>
    /// <param name="localPosition">The new position of the entity when it exit the bluespace.</param>
    /// <param name="mapId">
    ///     The new map where the entity will be when it exit the bluespace. Maybe null if the map is not
    ///     exists yet. You can create the map by <see cref="GetMapForTileOrCreate" /> using the next out parameter.
    /// </param>
    /// <param name="tilePosition">Position of the entity on overmap.</param>
    public void GetExitLocation(EntityUid uid, out Vector2 localPosition, [NotNullWhen(true)] out MapId? mapId,
        out Vector2i tilePosition)
    {
        mapId = null;
        var tilePositionF = BluespacePositionToTilePosition(uid);
        localPosition = BluespacePositionToLocalPosition(uid, tilePositionF);
        tilePosition = tilePositionF.Floored();

        if (_tiles.ContainsKey(tilePosition))
        {
            mapId = _tiles[tilePosition].MapId;
            return;
        }

        mapId = GetMapForTileOrCreate(tilePosition);
    }

    public float? GetDistance(EntityUid a, EntityUid b)
    {
        var xFormQuery = GetEntityQuery<TransformComponent>();

        var xFormA = xFormQuery.GetComponent(a);
        var xFormB = xFormQuery.GetComponent(b);

        var positionA = IsEntityInBluespace(a, xFormA) ? xFormA.WorldPosition : LocalPositionToBluespace(a);
        var positionB = IsEntityInBluespace(b, xFormB) ? xFormB.WorldPosition : LocalPositionToBluespace(b);

        if (positionA is null || positionB is null)
            return null;

        return (positionA.Value - positionB.Value).Length;
    }
}
