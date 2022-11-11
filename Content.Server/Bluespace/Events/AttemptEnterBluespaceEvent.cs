namespace Content.Server.Bluespace.Events;

public sealed class AttemptEnterBluespaceEvent : CancellableEntityEventArgs
{
    public string? Reason = null;
    public readonly EntityUid EntityUid;

    public AttemptEnterBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
