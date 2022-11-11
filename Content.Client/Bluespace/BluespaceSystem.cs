using System.Diagnostics.CodeAnalysis;
using Content.Shared.Bluespace;
using Content.Shared.Bluespace.Events;
using Robust.Shared.Map;

namespace Content.Client.Bluespace;

public sealed class BluespaceSystem : SharedBluespaceSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BluespaceMapUpdatedEvent>(OnBluespaceMapUpdated);
    }

    private void OnBluespaceMapUpdated(BluespaceMapUpdatedEvent ev)
    {
        BluespaceMapId = ev.BluespaceMapId;
    }

    public bool TryGetBluespaceMap([NotNullWhen(true)] out MapId? mapId)
    {
        if (BluespaceMapId is null)
        {
            mapId = null;
            return false;
        }

        mapId = BluespaceMapId.Value;
        return true;
    }
}
