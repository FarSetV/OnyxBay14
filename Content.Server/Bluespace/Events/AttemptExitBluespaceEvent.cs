namespace Content.Server.Bluespace.Events;

public sealed class AttemptExitBluespaceEvent : CancellableEntityEventArgs
{
    public string? Reason = null;
    public readonly EntityUid EntityUid;

    public AttemptExitBluespaceEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}
