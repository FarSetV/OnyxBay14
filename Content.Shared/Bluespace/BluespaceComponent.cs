namespace Content.Shared.Bluespace;

/// <summary>
///     Added to an entity when it is queued or is travelling via Bluespace.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedBluespaceSystem))]
public sealed class BluespaceComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public float Accumulator = 0f;
    [ViewVariables] public BluespaceState State = BluespaceState.Available;
}

public enum BluespaceState : byte
{
    Invalid = 0,

    /// <summary>
    ///     A dummy state for presentation
    /// </summary>
    Available,

    /// <summary>
    ///     Sound played and launch started
    /// </summary>
    Starting,

    /// <summary>
    ///     When they're on the Bluespace map
    /// </summary>
    Travelling,

    /// <summary>
    ///     Approaching destination, play effects or whatever,
    /// </summary>
    Arriving
}
