using System.Linq;
using Content.Server.Bluespace;
using Content.Server.Bluespace.Events;
using Content.Server.Overmap.Systems;
using Robust.Shared.Map;

namespace Content.Server.BluespaceOvermapTransition;

public sealed class BluespaceOvermapTransitionSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly OvermapSystem _overmap = default!;
    [Dependency] private readonly BluespaceSystem _bluespace = default!;
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
        _overmap.GetExitLocation(ev.EntityUid, out _, out var mapId, out _);

        // The map is empty so why not.
        if (mapId is null)
        {
            return;
        }

        var occupied = _mapManager
            .FindGridsIntersecting(mapId.Value, Comp<MapGridComponent>(ev.EntityUid).Grid.LocalAABB)
            .Any();

        if (!occupied)
            return;

        ev.Reason = Loc.GetString("bluespace-exit-is-occupied");
        ev.Cancel();
    }

    private void OnExitBluespace(ExitBluespaceEvent ev)
    {
        if (ev.Handled)
            return;

        RaiseLocalEvent(ev.EntityUid, new BeforeExitBluespaceEvent(ev.EntityUid));

        var xForm = Transform(ev.EntityUid);

        _overmap.GetExitLocation(ev.EntityUid, out var localPosition, out var mapId, out var tilePosition);
        mapId ??= _overmap.GetMapForTileOrCreate(tilePosition);
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
        var bluespacePosition = _overmap.LocalPositionToBluespace(ev.EntityUid);
        xForm.Coordinates =
            new EntityCoordinates(
                _mapManager.GetMapEntityId(_bluespace.GetBluespace()),
                bluespacePosition!.Value);

        RaiseLocalEvent(ev.EntityUid, new AfterEnterBluespaceEvent(ev.EntityUid), true);

        ev.Handled = true;
    }
}
