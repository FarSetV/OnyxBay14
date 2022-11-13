using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Bluespace;
using Content.Server.Bluespace.Events;
using Content.Server.Overmap.Systems;
using Content.Shared.Bluespace;
using Content.Shared.Overmap;
using Content.Shared.Overmap.Systems;
using Robust.Shared.Map;

namespace Content.Server.BluespaceOvermapTransition;

public sealed class BluespaceOvermapTransitionSystem : EntitySystem
{
    [Dependency] private readonly BluespaceSystem _bluespace = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly OvermapSystem _overmap = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("transition");

        SubscribeLocalEvent<EnterBluespaceEvent>(OnEnterBluespace);
        SubscribeLocalEvent<ExitBluespaceEvent>(OnExitBluespace);
        SubscribeLocalEvent<AttemptEnterBluespaceEvent>(OnAttemptEnterBluespace);
        SubscribeLocalEvent<AttemptExitBluespaceEvent>(OnAttemptExitBluespace);
    }

    private void OnAttemptEnterBluespace(AttemptEnterBluespaceEvent ev)
    {
        _sawmill.Info($"{ToPrettyString(ev.EntityUid)} attempts enter bluespace");

        if (_overmap.GetTileEntityOn(ev.EntityUid) is not null)
            return;

        ev.Reason = Loc.GetString("bluespace-cant-enter-bluespace-from-here");
        ev.Cancel();
    }

    private void OnAttemptExitBluespace(AttemptExitBluespaceEvent ev)
    {
        _sawmill.Info($"{ToPrettyString(ev.EntityUid)} attempts exit bluespace");
        GetExitLocation(ev.EntityUid, out var localPosition, out var mapId, out _);

        // The map is empty so why not.
        if (mapId is null)
            return;

        var aabb = Comp<MapGridComponent>(ev.EntityUid).Grid.LocalAABB;

        var occupied = _mapManager
            .FindGridsIntersecting(mapId.Value, Box2.CenteredAround(localPosition, aabb.Size)).Any();

        if (!occupied)
            return;

        ev.Reason = Loc.GetString("bluespace-exit-is-occupied");
        ev.Cancel();
    }

    private void OnExitBluespace(ExitBluespaceEvent ev)
    {
        if (ev.Handled)
            return;

        var xForm = Transform(ev.EntityUid);
        GetExitLocation(ev.EntityUid, out var localPosition, out var mapId, out var tilePosition);
        mapId ??= _overmap.GetMapForTileOrCreate(tilePosition);

        if (!_mapManager.IsMapInitialized(mapId.Value))
            _mapManager.DoMapInitialize(mapId.Value);

        if (_mapManager.IsMapPaused(mapId.Value))
            _mapManager.SetMapPaused(mapId.Value, false);

        RaiseLocalEvent(ev.EntityUid, new BeforeExitBluespaceEvent(ev.EntityUid, localPosition, mapId.Value));

        xForm.Coordinates = new EntityCoordinates(_mapManager.GetMapEntityId(mapId.Value), localPosition);

        _sawmill.Info($"{ToPrettyString(ev.EntityUid)} new coordinates: {xForm.Coordinates}");
        RaiseLocalEvent(ev.EntityUid, new AfterExitBluespaceEvent(ev.EntityUid), true);

        ev.Handled = true;
    }

    private void OnEnterBluespace(EnterBluespaceEvent ev)
    {
        if (ev.Handled)
            return;

        RaiseLocalEvent(ev.EntityUid, new BeforeEnterBluespaceEvent(ev.EntityUid), true);

        var xForm = Transform(ev.EntityUid);
        var bluespacePosition = LocalPositionToBluespace(ev.EntityUid);
        xForm.Coordinates =
            new EntityCoordinates(
                _mapManager.GetMapEntityId(_bluespace.GetBluespace()),
                bluespacePosition!.Value);

        RaiseLocalEvent(ev.EntityUid, new AfterEnterBluespaceEvent(ev.EntityUid), true);

        ev.Handled = true;
    }

    public float? GetDistance(EntityUid a, EntityUid b)
    {
        var xFormQuery = GetEntityQuery<TransformComponent>();

        var xFormA = xFormQuery.GetComponent(a);
        var xFormB = xFormQuery.GetComponent(b);

        var positionA = _bluespace.IsEntityInBluespace(a, xFormA) ? xFormA.WorldPosition : LocalPositionToBluespace(a);
        var positionB = _bluespace.IsEntityInBluespace(b, xFormB) ? xFormB.WorldPosition : LocalPositionToBluespace(b);

        if (positionA is null || positionB is null)
            return null;

        return (positionA.Value - positionB.Value).Length;
    }

    public Vector2? LocalPositionToBluespace(EntityUid entity)
    {
        var tile = _overmap.GetTileEntityOn(entity);

        if (tile is null)
            return null;

        var xForm = Transform(entity);
        var halfSize = SharedOvermapTile.TileSize / 2f;
        var localPosition = new Vector2(
            Math.Clamp(xForm.WorldPosition.X, -halfSize, halfSize),
            Math.Clamp(xForm.WorldPosition.Y, -halfSize, halfSize)
        );
        var worldPosition = tile.WorldMatrix.Transform(localPosition);

        return SharedBluespaceSystem.ScaleMatrix.Transform(worldPosition);
    }

    public Vector2 BluespacePositionToTilePosition(EntityUid entity)
    {
        var xForm = Transform(entity);
        return BluespacePositionToTilePosition(xForm.WorldPosition);
    }

    public Vector2 BluespacePositionToTilePosition(Vector2 position)
    {
        var bluespaceSize = SharedBluespaceSystem.OvermapBluespaceSize;
        var worldPosition = new Vector2(
            Math.Clamp(position.X, 0, bluespaceSize.X),
            Math.Clamp(position.Y, 0, bluespaceSize.Y)
        );
        var tilePosition = SharedBluespaceSystem.InvertScaleMatrix.Transform(worldPosition) /
                           SharedOvermapTile.TileSize;

        tilePosition.X = Math.Clamp(tilePosition.X, 0, SharedOvermapSystem.OvermapTilesCount.X);
        tilePosition.Y = Math.Clamp(tilePosition.Y, 0, SharedOvermapSystem.OvermapTilesCount.Y);

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
        var scaledPosition = SharedBluespaceSystem.InvertScaleMatrix.Transform(position);

        return SharedOvermapTile.GetInvWorldMatrix(tilePosition.Value.Floored()).Transform(scaledPosition);
    }

    public void GetExitLocation(EntityUid uid, out Vector2 localPosition, [NotNullWhen(true)] out MapId? mapId,
        out Vector2i tilePosition)
    {
        mapId = null;
        var tilePositionF = BluespacePositionToTilePosition(uid);
        localPosition = BluespacePositionToLocalPosition(uid, tilePositionF);
        tilePosition = tilePositionF.Floored();

        mapId = _overmap.GetMapForTileOrCreate(tilePosition);
    }
}
