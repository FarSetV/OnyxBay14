using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Radiation.Events;
using Content.Shared.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Singularity.EntitySystems;

public sealed class RadiationCollectorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationCollectorComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RadiationCollectorComponent, OnIrradiatedEvent>(OnRadiation);
        SubscribeLocalEvent<RadiationCollectorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RadiationCollectorComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<RadiationCollectorComponent, EntRemovedFromContainerMessage>(OnContainerModified);
    }

    public override void Update(float frameTime)
    {
        if (_gameTiming.CurTime < _nextUpdate)
            return;

        _nextUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(5);

        foreach (var collector in EntityQuery<RadiationCollectorComponent>())
        {
            if (!collector.Enabled)
                continue;

            var xForm = Transform(collector.Owner);

            if (xForm.GridUid is null || xForm.MapUid is null)
                continue;

            var tileMixture = _atmosphereSystem.GetTileMixture(xForm.GridUid.Value, xForm.MapUid.Value,
                _transformSystem.GetGridOrMapTilePosition(collector.Owner, xForm), true);

            if (tileMixture is null)
                continue;

            if (tileMixture.Temperature < collector.TemperatureThreshold)
                continue;

            collector.Integrity -=
                (tileMixture.Temperature - collector.TemperatureThreshold) * frameTime;

            if (collector.Integrity <= 0)
                _explosionSystem.TriggerExplosive(collector.Owner);

            Dirty(collector);
        }
    }

    private void OnInteractHand(EntityUid uid, RadiationCollectorComponent component, InteractHandEvent args)
    {
        var curTime = _gameTiming.CurTime;

        if (curTime < component.CoolDownEnd)
            return;

        ToggleCollector(uid, args.User, component);
        component.CoolDownEnd = curTime + component.Cooldown;
    }

    private void OnContainerModified(EntityUid uid, RadiationCollectorComponent component,
        ContainerModifiedMessage args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnRadiation(EntityUid uid, RadiationCollectorComponent component, OnIrradiatedEvent args)
    {
        if (!component.Enabled)
            return;

        // No idea if this is even vaguely accurate to the previous logic.
        // The maths is copied from that logic even though it works differently.
        // But the previous logic would also make the radiation collectors never ever stop providing energy.
        // And since frameTime was used there, I'm assuming that this is what the intent was.
        // This still won't stop things being potentially hilariously unbalanced though.
        if (!TryComp<BatteryComponent>(uid, out var batteryComponent))
            return;

        var charge = CalculateMultipliedRadiation(uid, component, args.TotalRads) * component.ChargeModifier;
        component.LastProducedPower = charge;
        batteryComponent.CurrentCharge += charge;

        var exposed = ExposeHeat(uid, component, charge);
        component.LastProducedHeat = exposed;

        DrainTankMoles(uid, component, RadiationCollectorComponent.DrainMoles);
    }

    public void ToggleCollector(EntityUid uid, EntityUid? user = null, RadiationCollectorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        SetCollectorEnabled(uid, !component.Enabled, user, component);
    }

    public void SetCollectorEnabled(EntityUid uid, bool enabled, EntityUid? user = null,
        RadiationCollectorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        component.Enabled = enabled;

        // Show message to the player
        if (user != null)
        {
            var msg = component.Enabled
                ? "radiation-collector-component-use-on"
                : "radiation-collector-component-use-off";
            _popupSystem.PopupEntity(Loc.GetString(msg), uid, Filter.Pvs(user.Value));
        }

        // Update appearance
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, RadiationCollectorComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = component.Enabled ? RadiationCollectorVisualState.Active : RadiationCollectorVisualState.Inactive;
        _appearance.SetData(uid, RadiationCollectorVisuals.VisualState, state);

        var tank = GetGasTank(uid, component);

        var tankState = tank is null
            ? RadiationCollectorSlotVisualState.Free
            : RadiationCollectorSlotVisualState.Occupied;
        _appearance.SetData(uid, RadiationCollectorSlotVisuals.VisualState, tankState);
    }

    private float CalculateMultipliedRadiation(EntityUid uid, RadiationCollectorComponent? component, float radiation)
    {
        if (!Resolve(uid, ref component))
            return radiation;

        var tankComponent = GetGasTank(uid, component);

        if (tankComponent is null || tankComponent.Air.TotalMoles < float.Epsilon)
            return 0.0f;

        var air = tankComponent.Air;
        var result = radiation;

        foreach (var gas in (sbyte[]) Enum.GetValues(typeof(Gas)))
        {
            if (air.Moles[gas] < float.Epsilon)
                continue;

            var multiplier = air.Moles[gas] * component.GasMultiplier[gas];
            result *= multiplier;
        }

        return result;
    }

    private float ExposeHeat(EntityUid uid, RadiationCollectorComponent? component, float powerProduced)
    {
        if (!Resolve(uid, ref component))
            return 0.0f;

        var xForm = Transform(uid);

        if (xForm.GridUid is null)
            return 0.0f;

        var tankComponent = GetGasTank(uid, component);

        if (tankComponent is null)
            return 0.0f;

        var temp = component.KelvinsPerJoule * powerProduced;

        var heatCapacity = _atmosphereSystem.GetHeatCapacity(tankComponent.Air);

        if (heatCapacity <= float.Epsilon)
            return 0.0f;

        var heat = temp / heatCapacity;
        tankComponent.Air.Temperature += heat;

        var tileMixture = _atmosphereSystem.GetTileMixture(xForm.GridUid.Value, xForm.MapUid!.Value,
            _transformSystem.GetGridOrMapTilePosition(uid, xForm), true);

        if (tileMixture is null)
            return heat;

        if (tankComponent.Air.Temperature > tileMixture.Temperature)
            tileMixture.Temperature += heat;

        return heat;
    }

    private void DrainTankMoles(EntityUid uid, RadiationCollectorComponent? component, float amount)
    {
        if (!Resolve(uid, ref component))
            return;

        var tank = GetGasTank(uid, component);
        tank?.Air.Remove(amount);
    }

    private GasTankComponent? GetGasTank(EntityUid uid, RadiationCollectorComponent? component)
    {
        if (!Resolve(uid, ref component))
            return null;

        var tank = _itemSlotsSystem.GetItemOrNull(uid, component.TankSlot);

        return tank is null ? null : CompOrNull<GasTankComponent>(tank.Value);
    }

    private void OnExamine(EntityUid uid, RadiationCollectorComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("radiation-collector-integrity",
            ("integrity", Math.Round(component.Integrity, 0))));
        args.PushMarkup(Loc.GetString("radiation-collector-produced-power", ("power", component.LastProducedPower)));
        args.PushMarkup(Loc.GetString("radiation-collector-produces-heat",
            ("temperature", Math.Round(component.LastProducedHeat, 1))));

        var tankComponent = GetGasTank(uid, component);

        if (tankComponent is null)
        {
            args.PushMarkup(Loc.GetString("radiation-collector-tank-empty"));
            return;
        }

        args.PushMarkup(Loc.GetString("radiation-collector-tank-occupied"));
        args.PushMarkup(Loc.GetString("radiation-collector-tank-moles",
            ("moles", Math.Round(tankComponent.Air.TotalMoles, 1))));
        args.PushMarkup(Loc.GetString("radiation-collector-tank-temperature",
            ("temperature", Math.Round(tankComponent.Air.Temperature, 1))));
    }
}
