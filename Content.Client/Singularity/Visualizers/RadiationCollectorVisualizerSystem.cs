using Content.Client.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Visualizers;

public sealed class RadiationCollectorVisualizerSystem : VisualizerSystem<RadiationCollectorVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RadiationCollectorVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        var entities = IoCManager.Resolve<IEntityManager>();
        if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
            return;
        if (!args.Component.TryGetData(RadiationCollectorVisuals.VisualState, out RadiationCollectorVisualState state))
            state = RadiationCollectorVisualState.Inactive;

        if (!args.Component.TryGetData(RadiationCollectorSlotVisuals.VisualState,
                out RadiationCollectorSlotVisualState slotState))
            slotState = RadiationCollectorSlotVisualState.Free;

        switch (state)
        {
            case RadiationCollectorVisualState.Active:
                sprite.LayerSetState(RadiationCollectorVisualLayers.Main, "ca_active");
                break;
            case RadiationCollectorVisualState.Inactive:
                sprite.LayerSetState(RadiationCollectorVisualLayers.Main, "ca_inactive");
                break;
        }

        switch (slotState)
        {
            case RadiationCollectorSlotVisualState.Occupied:
                sprite.LayerSetVisible(RadiationCollectorVisualLayers.TankSlot, true);
                break;
            case RadiationCollectorSlotVisualState.Free:
                sprite.LayerSetVisible(RadiationCollectorVisualLayers.TankSlot, false);
                break;
        }
    }
}

public enum RadiationCollectorVisualLayers : byte
{
    Main,
    TankSlot
}
