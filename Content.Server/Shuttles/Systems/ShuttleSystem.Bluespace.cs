using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private readonly SoundSpecifier _arrivalSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_end.ogg");

    private readonly SoundSpecifier _startupSound =
        new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_begin.ogg");

    public bool CanTravelBluespace(EntityUid uid, [NotNullWhen(false)] out string? reason,
        TransformComponent? xform = null)
    {
        if (_overmap.GetTileEntityOn(uid) is null)
        {
            reason = Loc.GetString("shuttle-console-cant-enter-bluespace");
            return false;
        }

        reason = null;

        if (!TryComp<MapGridComponent>(uid, out var grid) ||
            !Resolve(uid, ref xform))
            return true;

        var bounds = xform.WorldMatrix.TransformBox(grid.Grid.LocalAABB).Enlarged(ShuttleFTLRange);
        var bodyQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var other in _mapManager.FindGridsIntersecting(xform.MapID, bounds))
        {
            if (grid.Owner == other.GridEntityId ||
                !bodyQuery.TryGetComponent(other.GridEntityId, out var body) ||
                body.Mass < ShuttleFTLMassThreshold)
                continue;

            reason = Loc.GetString("shuttle-console-proximity");
            return false;
        }

        return true;
    }

    public bool TryEnterBluespace(ShuttleComponent shuttle, [NotNullWhen(true)] out BluespaceComponent? component)
    {
        var uid = shuttle.Owner;
        component = null;

        if (HasComp<BluespaceComponent>(uid))
        {
            _sawmill.Warning($"tried queuing {ToPrettyString(uid)} which already has BluespaceComponent?");
            return false;
        }

        // TODO: Maybe move this to docking instead?
        SetDocks(uid, false);

        component = AddComp<BluespaceComponent>(uid);
        component.State = BluespaceState.Starting;
        component.Accumulator = DefaultStartupTime;
        // TODO: Need BroadcastGrid to not be bad.
        _audio.PlayGlobal(_startupSound,
            Filter.Empty().AddInRange(Transform(uid).MapPosition, GetSoundRange(component.Owner)),
            _startupSound.Params);

        // Make sure the map is setup before we leave to avoid pop-in (e.g. parallax).
        _overmap.SetupBluespaceMap();

        return true;
    }

    private void UpdateBluespace(float frameTime)
    {
        foreach (var comp in EntityQuery<BluespaceComponent>())
        {
            comp.Accumulator -= frameTime;

            if (comp.Accumulator > 0f)
                continue;

            var xform = Transform(comp.Owner);
            PhysicsComponent? body;

            switch (comp.State)
            {
                // Startup time has elapsed and in bluespace.
                case BluespaceState.Starting:
                    DoTheDinosaur(xform);

                    comp.State = BluespaceState.Travelling;
                    xform.Coordinates =
                        new EntityCoordinates(_mapManager.GetMapEntityId(_overmap.BluespaceMapId!.Value),
                            _overmap.LocalPositionToBluespace(comp.Owner)!.Value);

                    if (TryComp(comp.Owner, out body))
                    {
                        body.LinearVelocity = Vector2.Zero;
                        body.AngularVelocity = 0f;
                        body.LinearDamping = 0f;
                        body.AngularDamping = 0f;
                    }

                    if (comp.TravelSound != null)
                    {
                        comp.TravelStream = _audio.PlayGlobal(comp.TravelSound,
                            Filter.Pvs(comp.Owner, 4f, EntityManager), comp.TravelSound.Params);
                    }

                    SetDockBolts(comp.Owner, true);
                    break;
                // Arrived
                case BluespaceState.Arriving:
                    DoTheDinosaur(xform);
                    SetDockBolts(comp.Owner, false);
                    SetDocks(comp.Owner, true);

                    if (TryComp(comp.Owner, out body))
                    {
                        body.LinearVelocity = Vector2.Zero;
                        body.AngularVelocity = 0f;
                        body.LinearDamping = ShuttleLinearDamping;
                        body.AngularDamping = ShuttleAngularDamping;
                    }

                    TryComp(comp.Owner, out ShuttleComponent? shuttle);
                    _overmap.GetExitLocation(comp.Owner, out var localPosition, out var mapId, out var tilePosition);
                    mapId ??= _overmap.GetMapForTileOrCreate(tilePosition);

                    xform.Coordinates = new EntityCoordinates(_mapManager.GetMapEntityId(mapId.Value), localPosition);

                    if (shuttle != null)
                        _thruster.DisableLinearThrusters(shuttle);

                    if (comp.TravelStream != null)
                    {
                        comp.TravelStream?.Stop();
                        comp.TravelStream = null;
                    }

                    _audio.PlayGlobal(_arrivalSound,
                        Filter.Empty().AddInRange(Transform(comp.Owner).MapPosition, GetSoundRange(comp.Owner)),
                        _arrivalSound.Params);

                    comp.State = BluespaceState.Cooldown;
                    comp.Accumulator = FTLCooldown;
                    break;
                case BluespaceState.Cooldown:
                    RemComp<BluespaceComponent>(comp.Owner);
                    break;
            }
        }
    }

    public bool CanExitBluespace(EntityUid uid, [NotNullWhen(false)] out string? reason)
    {
        _overmap.GetExitLocation(uid, out _, out var mapId, out _);

        // The map is empty so why not.
        if (mapId is null)
        {
            reason = null;
            return true;
        }

        var occupied = _mapManager
            .FindGridsIntersecting(mapId.Value, Comp<MapGridComponent>(uid).Grid.LocalAABB)
            .Any();

        if (occupied)
        {
            reason = Loc.GetString("shuttle-console-bluespace-exit-is-occupied");
            return false;
        }

        reason = null;
        return true;
    }

    public bool EnterBluespace(ShuttleComponent component, float startupTime = DefaultStartupTime)
    {
        if (!TryEnterBluespace(component, out var bluespace))
            return false;

        bluespace.StartupTime = startupTime;
        bluespace.Accumulator = bluespace.StartupTime;

        return true;
    }

    public bool ExitBluespace(EntityUid uid, BluespaceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanExitBluespace(uid, out _))
            return false;

        component.Accumulator += DefaultArrivalTime;
        component.State = BluespaceState.Arriving;

        return true;
    }
}
