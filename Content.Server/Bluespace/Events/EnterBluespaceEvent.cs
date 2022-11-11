namespace Content.Server.Bluespace.Events;

public sealed class EnterBluespaceEvent : HandledEntityEventArgs
{
    public readonly EntityUid EntityUid;

    public EnterBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
