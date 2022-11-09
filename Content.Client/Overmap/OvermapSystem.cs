using Content.Shared.Overmap;
using Robust.Client.GameObjects;

namespace Content.Client.Overmap;

public sealed class OvermapSystem : SharedOvermapSystem
{
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BluespaceMapUpdatedMessage>(OnBluespaceMapUpdated);
    }

    private void OnBluespaceMapUpdated(BluespaceMapUpdatedMessage args)
    {
        BluespaceMapId = args.NewId;

        if (BluespaceMapId is not null)
            _mapSystem.SetAmbientLight(BluespaceMapId.Value, new Color(0, 0, 55));
    }
}
