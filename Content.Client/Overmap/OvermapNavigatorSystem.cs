using Content.Shared.Overmap;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Overmap;

public sealed class OvermapNavigatorSystem : SharedOvermapNavigatorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvermapNavigatorComponent, ComponentHandleState>(OnComponentHandleState);
    }

    private void OnComponentHandleState(EntityUid uid, OvermapNavigatorComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not OvermapNavigatorComponentState state)
            return;

        component.Points = state.Points;
        component.SignatureRadius = state.SignatureRadius;
        component.IFFRadius = state.FFIRadius;
    }
}
