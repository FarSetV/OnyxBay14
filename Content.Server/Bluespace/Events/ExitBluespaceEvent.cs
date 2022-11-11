namespace Content.Server.Bluespace.Events;

public sealed class ExitBluespaceEvent : HandledEntityEventArgs
{
    public readonly EntityUid EntityUid;

    public ExitBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
