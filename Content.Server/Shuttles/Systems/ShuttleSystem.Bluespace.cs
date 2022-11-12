using System.Diagnostics.CodeAnalysis;
using Content.Server.Bluespace.Events;
using Content.Server.Shuttles.Components;
using Content.Shared.Bluespace;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private readonly SoundSpecifier _arrivalSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_end.ogg");

    private readonly SoundSpecifier _startupSound =
        new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_begin.ogg");

    private void UpdateBluespace(float frameTime)
    {
        foreach (var component in EntityQuery<ShuttleComponent>())
        {
            component.EnginesCooldown = Math.Max(component.EnginesCooldown - frameTime, 0f);
        }
    }

    private void OnAfterExitBluespace(EntityUid uid, ShuttleComponent component, AfterExitBluespaceEvent args)
    {
        _thruster.DisableLinearThrusters(component);

        if (component.TravelStream != null)
        {
            component.TravelStream?.Stop();
            component.TravelStream = null;
        }

        _audio.PlayGlobal(_arrivalSound,
            Filter.Empty().AddInRange(Transform(uid).MapPosition, GetSoundRange(uid)),
            _arrivalSound.Params);

        component.EnginesCooldown = FTLCooldown;
    }

    private void OnBeforeExitBluespace(EntityUid uid, ShuttleComponent component, BeforeExitBluespaceEvent args)
    {
        var xForm = Transform(uid);

        DoTheDinosaur(xForm);
        SetDockBolts(component.Owner, false);
        SetDocks(component.Owner, true);

        if (!TryComp(component.Owner, out PhysicsComponent? body))
            return;

        body.LinearVelocity = Vector2.Zero;
        body.AngularVelocity = 0f;
        body.LinearDamping = ShuttleLinearDamping;
        body.AngularDamping = ShuttleAngularDamping;
    }

    private void OnBeforeEnterBluespace(EntityUid uid, ShuttleComponent component, BeforeEnterBluespaceEvent ev)
    {
        DoTheDinosaur(Transform(uid));
    }

    private void OnAfterEnterBluespace(EntityUid uid, ShuttleComponent component, AfterEnterBluespaceEvent args)
    {
        if (TryComp(component.Owner, out PhysicsComponent? body))
        {
            body.LinearVelocity = Vector2.Zero;
            body.AngularVelocity = 0f;
            body.LinearDamping = 0f;
            body.AngularDamping = 0f;
        }

        if (component.TravelSound != null)
        {
            component.TravelStream = _audio.PlayGlobal(component.TravelSound,
                Filter.Pvs(component.Owner, 4f, EntityManager), component.TravelSound.Params);
        }

        SetDockBolts(component.Owner, true);
    }

    public bool TryEnterBluespace(
        ShuttleComponent shuttle,
        [NotNullWhen(true)] out BluespaceComponent? component,
        [NotNullWhen(false)] out string? reason
    )
    {
        var uid = shuttle.Owner;

        if (shuttle.EnginesCooldown > float.Epsilon)
        {
            reason = Loc.GetString("shuttle-engines-on-cooldown");
            component = null;
            return false;
        }

        if (!TryComp<MapGridComponent>(uid, out var grid))
        {
            reason = Loc.GetString("shuttle-cant-enter-bluespace");
            component = null;
            return false;
        }

        var xForm = Transform(grid.Owner);
        var bounds = xForm.WorldMatrix.TransformBox(grid.Grid.LocalAABB).Enlarged(ShuttleFTLRange);
        var bodyQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var other in _mapManager.FindGridsIntersecting(xForm.MapID, bounds))
        {
            if (grid.Owner == other.GridEntityId ||
                !bodyQuery.TryGetComponent(other.GridEntityId, out var body) ||
                body.Mass < ShuttleFTLMassThreshold)
                continue;

            reason = Loc.GetString("shuttle-console-proximity");
            component = null;
            return false;
        }

        if (!_bluespace.TryEnterBluespace(grid.Owner, DefaultStartupTime, out component, out reason))
        {
            component = null;
            return false;
        }

        // TODO: Maybe move this to docking instead?
        SetDocks(uid, false);

        // TODO: Need BroadcastGrid to not be bad.
        _audio.PlayGlobal(_startupSound,
            Filter.Empty().AddInRange(Transform(uid).MapPosition, GetSoundRange(component.Owner)),
            _startupSound.Params);

        return true;
    }

    public bool TryExitBluespace(EntityUid uid, ShuttleComponent? component, [NotNullWhen(false)] out string? reason)
    {
        if (Resolve(uid, ref component) && TryComp<MapGridComponent>(uid, out var grid))
            return _bluespace.TryExitBluespace(grid.Owner, 0f, null, out reason);

        reason = Loc.GetString("shuttle-cant-enter-bluespace");
        return false;
    }
}
