namespace Content.Server.Bluespace.Events;

public sealed class AfterEnterBluespaceEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;

    public AfterEnterBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
