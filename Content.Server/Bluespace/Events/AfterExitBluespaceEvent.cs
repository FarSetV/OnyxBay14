namespace Content.Server.Bluespace.Events;

public sealed class AfterExitBluespaceEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;

    public AfterExitBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
