using Content.Server.Overmap.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Bluespace;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<ShuttleConsoleComponent, PowerChangedEvent>(OnConsolePowerChange);
        SubscribeLocalEvent<ShuttleConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChange);
        SubscribeLocalEvent<ShuttleConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
        SubscribeLocalEvent<ShuttleConsoleComponent, ShuttleConsoleDestinationMessage>(OnDestinationMessage);
        SubscribeLocalEvent<ShuttleConsoleComponent, BoundUIClosedEvent>(OnConsoleUIClose);

        SubscribeLocalEvent<ShuttleConsoleComponent, EnterBluespaceMessage>(OnEnterBluespaceMessage);
        SubscribeLocalEvent<ShuttleConsoleComponent, ExitBluespaceMessage>(OnExitBluespaceMessage);

        SubscribeLocalEvent<DockEvent>(OnDock);
        SubscribeLocalEvent<UndockEvent>(OnUndock);

        SubscribeLocalEvent<PilotComponent, MoveEvent>(HandlePilotMove);
        SubscribeLocalEvent<PilotComponent, ComponentGetState>(OnGetState);
    }

    private void OnEnterBluespaceMessage(EntityUid uid, ShuttleConsoleComponent component, EnterBluespaceMessage args)
    {
        EntityUid? entity = component.Owner;

        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = uid
        };

        RaiseLocalEvent(entity.Value, ref getShuttleEv);
        entity = getShuttleEv.Console;

        if (entity is null)
            return;

        if (!TryComp<TransformComponent>(entity, out var xform) ||
            !TryComp<ShuttleComponent>(xform.GridUid, out var shuttle))
            return;

        if (HasComp<BluespaceComponent>(xform.GridUid))
        {
            if (args.Session.AttachedEntity != null)
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-in-bluespace"),
                    Filter.Entities(args.Session.AttachedEntity.Value));
            }

            return;
        }

        if (_shuttle.TryEnterBluespace(shuttle, out _, out var reason))
            return;

        if (args.Session.AttachedEntity != null)
            _popup.PopupCursor(reason, Filter.Entities(args.Session.AttachedEntity.Value));
    }

    private void OnExitBluespaceMessage(EntityUid uid, ShuttleConsoleComponent component, ExitBluespaceMessage args)
    {
        EntityUid? entity = component.Owner;

        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = uid
        };

        RaiseLocalEvent(entity.Value, ref getShuttleEv);
        entity = getShuttleEv.Console;

        if (entity is null)
            return;

        if (!TryComp<TransformComponent>(entity, out var xform) ||
            !TryComp<ShuttleComponent>(xform.GridUid, out var shuttle))
            return;

        if (_shuttle.TryExitBluespace(xform.GridUid.Value, shuttle, out var reason))
            return;

        if (args.Session.AttachedEntity != null)
            _popup.PopupCursor(reason, Filter.Entities(args.Session.AttachedEntity.Value));
    }

    [Obsolete("Use OnEnterBluespaceMessage")]
    private void OnDestinationMessage(EntityUid uid, ShuttleConsoleComponent component,
        ShuttleConsoleDestinationMessage args)
    {
        if (!TryComp<FTLDestinationComponent>(args.Destination, out var dest))
            return;

        if (!dest.Enabled)
            return;

        EntityUid? entity = component.Owner;

        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = uid
        };

        RaiseLocalEvent(entity.Value, ref getShuttleEv);
        entity = getShuttleEv.Console;

        if (entity == null || dest.Whitelist?.IsValid(entity.Value, EntityManager) == false)
            return;

        if (!TryComp<TransformComponent>(entity, out var xform) ||
            !TryComp<ShuttleComponent>(xform.GridUid, out var shuttle))
            return;

        if (HasComp<FTLComponent>(xform.GridUid))
        {
            if (args.Session.AttachedEntity != null)
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-in-ftl"),
                    Filter.Entities(args.Session.AttachedEntity.Value));
            }

            return;
        }

        if (!_shuttle.CanFTL(shuttle.Owner, out var reason))
        {
            if (args.Session.AttachedEntity != null)
                _popup.PopupCursor(reason, Filter.Entities(args.Session.AttachedEntity.Value));

            return;
        }

        _shuttle.FTLTravel(shuttle, args.Destination, hyperspaceTime: _shuttle.TransitTime);
    }

    private void OnDock(DockEvent ev)
    {
        RefreshShuttleConsoles();
    }

    private void OnUndock(UndockEvent ev)
    {
        RefreshShuttleConsoles();
    }

    public void RefreshShuttleConsoles(EntityUid uid)
    {
        // TODO: Should really call this per shuttle in some instances.
        RefreshShuttleConsoles();
    }

    /// <summary>
    ///     Refreshes all of the data for shuttle consoles.
    /// </summary>
    public void RefreshShuttleConsoles()
    {
        var docks = GetAllDocks();

        foreach (var comp in EntityQuery<ShuttleConsoleComponent>(true))
        {
            UpdateState(comp, docks);
        }
    }

    /// <summary>
    ///     Stop piloting if the window is closed.
    /// </summary>
    private void OnConsoleUIClose(EntityUid uid, ShuttleConsoleComponent component, BoundUIClosedEvent args)
    {
        if ((ShuttleConsoleUiKey) args.UiKey != ShuttleConsoleUiKey.Key ||
            args.Session.AttachedEntity is not { } user)
            return;

        // In case they D/C should still clean them up.
        foreach (var comp in EntityQuery<AutoDockComponent>(true))
        {
            comp.Requesters.Remove(user);
        }

        RemovePilot(user);
    }

    private void OnConsoleUIOpenAttempt(EntityUid uid, ShuttleConsoleComponent component,
        ActivatableUIOpenAttemptEvent args)
    {
        if (!TryPilot(args.User, uid))
            args.Cancel();
    }

    private void OnConsoleAnchorChange(EntityUid uid, ShuttleConsoleComponent component,
        ref AnchorStateChangedEvent args)
    {
        UpdateState(component);
    }

    private void OnConsolePowerChange(EntityUid uid, ShuttleConsoleComponent component, ref PowerChangedEvent args)
    {
        UpdateState(component);
    }

    private bool TryPilot(EntityUid user, EntityUid uid)
    {
        if (!_tags.HasTag(user, "CanPilot") ||
            !TryComp<ShuttleConsoleComponent>(uid, out var component) ||
            !this.IsPowered(uid, EntityManager) ||
            !Transform(uid).Anchored ||
            !_blocker.CanInteract(user, uid))
            return false;

        var pilotComponent = EntityManager.EnsureComponent<PilotComponent>(user);
        var console = pilotComponent.Console;

        if (console != null)
        {
            RemovePilot(pilotComponent);

            if (console == component)
                return false;
        }

        AddPilot(user, component);
        return true;
    }

    private void OnGetState(EntityUid uid, PilotComponent component, ref ComponentGetState args)
    {
        args.State = new PilotComponentState(component.Console?.Owner);
    }

    /// <summary>
    ///     Returns the position and angle of all dockingcomponents.
    /// </summary>
    private List<DockingInterfaceState> GetAllDocks()
    {
        // TODO: NEED TO MAKE SURE THIS UPDATES ON ANCHORING CHANGES!
        var result = new List<DockingInterfaceState>();

        foreach (var (comp, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
        {
            if (xform.ParentUid != xform.GridUid)
                continue;

            var state = new DockingInterfaceState
            {
                Coordinates = xform.Coordinates,
                Angle = xform.LocalRotation,
                Entity = comp.Owner,
                Connected = comp.Docked,
                Color = comp.RadarColor,
                HighlightedColor = comp.HighlightedRadarColor
            };
            result.Add(state);
        }

        return result;
    }

    private OvermapNavigatorBoundInterfaceState? GetOvermapNavigatorState(EntityUid uid)
    {
        var xForm = Transform(uid);

        if (xForm.GridUid is not { } gridUid)
            return null;

        if (!TryComp<OvermapNavigatorComponent>(uid, out var navigatorComponent))
            return null;

        BluespaceState? bluespaceState = null;

        if (TryComp<BluespaceComponent>(gridUid, out var bluespaceComponent))
            bluespaceState = bluespaceComponent.State;

        var engineCooldown = 0f;

        if (TryComp<ShuttleComponent>(gridUid, out var shuttleComponent))
            engineCooldown = shuttleComponent.EnginesCooldown;

        return new OvermapNavigatorBoundInterfaceState(
            gridUid,
            bluespaceState,
            engineCooldown,
            navigatorComponent.SignatureRadius,
            navigatorComponent.IFFRadius,
            navigatorComponent.Points
        );
    }

    private RadarConsoleBoundInterfaceState? GetRadarConsoleState(EntityUid uid,
        List<DockingInterfaceState>? docks = null)
    {
        if (!TryComp<RadarConsoleComponent>(uid, out var radarComponent))
            return null;

        var xForm = Transform(uid);

        return new RadarConsoleBoundInterfaceState(
            radarComponent.MaxRange,
            xForm.Coordinates,
            radarComponent.Rotation,
            docks ?? GetAllDocks()
        );
    }

    private void UpdateState(ShuttleConsoleComponent component, List<DockingInterfaceState>? docks = null)
    {
        EntityUid? entity = component.Owner;

        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = entity
        };

        RaiseLocalEvent(entity.Value, ref getShuttleEv);
        entity = getShuttleEv.Console;

        if (entity is null)
            return;

        ShuttleNavigatorRadarBoundInterfaceState state = new()
        {
            RadarConsoleState = GetRadarConsoleState(entity.Value, docks),
            NavigatorState = GetOvermapNavigatorState(entity.Value)
        };

        _ui.TrySetUiState(component.Owner, ShuttleConsoleUiKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new RemQueue<PilotComponent>();

        foreach (var comp in EntityManager.EntityQuery<PilotComponent>())
        {
            if (comp.Console == null)
                continue;

            if (!_blocker.CanInteract(comp.Owner, comp.Console.Owner))
                toRemove.Add(comp);
        }

        foreach (var comp in toRemove)
        {
            RemovePilot(comp);
        }
    }

    protected override void OnStateUpdate(EntityUid uid, ShuttleConsoleComponent component)
    {
        UpdateState(component);
    }

    /// <summary>
    ///     If pilot is moved then we'll stop them from piloting.
    /// </summary>
    private void HandlePilotMove(EntityUid uid, PilotComponent component, ref MoveEvent args)
    {
        if (component.Console == null || component.Position == null)
        {
            DebugTools.Assert(component.Position == null && component.Console == null);
            EntityManager.RemoveComponent<PilotComponent>(uid);
            return;
        }

        if (args.NewPosition.TryDistance(EntityManager, component.Position.Value, out var distance) &&
            distance < PilotComponent.BreakDistance)
            return;

        RemovePilot(component);
    }

    protected override void HandlePilotShutdown(EntityUid uid, PilotComponent component, ComponentShutdown args)
    {
        base.HandlePilotShutdown(uid, component, args);
        RemovePilot(component);
    }

    private void OnConsoleShutdown(EntityUid uid, ShuttleConsoleComponent component, ComponentShutdown args)
    {
        ClearPilots(component);
    }

    public void AddPilot(EntityUid entity, ShuttleConsoleComponent component)
    {
        if (!EntityManager.TryGetComponent(entity, out PilotComponent? pilotComponent) ||
            component.SubscribedPilots.Contains(pilotComponent))
            return;

        if (TryComp<SharedEyeComponent>(entity, out var eye))
            eye.Zoom = component.Zoom;

        component.SubscribedPilots.Add(pilotComponent);

        _alertsSystem.ShowAlert(entity, AlertType.PilotingShuttle);

        pilotComponent.Console = component;
        ActionBlockerSystem.UpdateCanMove(entity);
        pilotComponent.Position = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
        Dirty(pilotComponent);
    }

    public void RemovePilot(PilotComponent pilotComponent)
    {
        var console = pilotComponent.Console;

        if (console is not { } helmsman)
            return;

        pilotComponent.Console = null;
        pilotComponent.Position = null;

        if (TryComp<SharedEyeComponent>(pilotComponent.Owner, out var eye))
            eye.Zoom = new Vector2(1.0f, 1.0f);

        if (!helmsman.SubscribedPilots.Remove(pilotComponent))
            return;

        _alertsSystem.ClearAlert(pilotComponent.Owner, AlertType.PilotingShuttle);

        _popupSystem.PopupEntity(Loc.GetString("shuttle-pilot-end"), pilotComponent.Owner,
            Filter.Entities(pilotComponent.Owner));

        if (pilotComponent.LifeStage < ComponentLifeStage.Stopping)
            EntityManager.RemoveComponent<PilotComponent>(pilotComponent.Owner);
    }

    public void RemovePilot(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent(entity, out PilotComponent? pilotComponent))
            return;

        RemovePilot(pilotComponent);
    }

    public void ClearPilots(ShuttleConsoleComponent component)
    {
        while (component.SubscribedPilots.TryGetValue(0, out var pilot))
        {
            RemovePilot(pilot);
        }
    }
}
