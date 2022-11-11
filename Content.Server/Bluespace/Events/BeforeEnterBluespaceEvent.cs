namespace Content.Server.Bluespace.Events;

public sealed class BeforeEnterBluespaceEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;

    public BeforeEnterBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
