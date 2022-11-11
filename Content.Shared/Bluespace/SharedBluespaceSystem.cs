using Robust.Shared.Map;

namespace Content.Shared.Bluespace;

public abstract class SharedBluespaceSystem : EntitySystem
{
    protected MapId? BluespaceMapId { get; set; }

    public bool IsEntityInBluespace(EntityUid entity, TransformComponent? xForm = null)
    {
        if (!Resolve(entity, ref xForm))
            return false;

        return xForm.MapID == BluespaceMapId;
    }
}
