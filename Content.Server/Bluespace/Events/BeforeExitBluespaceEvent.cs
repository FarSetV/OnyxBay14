namespace Content.Server.Bluespace.Events;

public sealed class BeforeExitBluespaceEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;

    public BeforeExitBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
