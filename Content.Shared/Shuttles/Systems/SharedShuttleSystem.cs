namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeIFF();
    }
}

[Flags]
[Obsolete("Use BluespaceState")]
public enum FTLState : byte
{
    Invalid = 0,

    /// <summary>
    /// A dummy state for presentation
    /// </summary>
    Available = 1 << 0,

    /// <summary>
    /// Sound played and launch started
    /// </summary>
    Starting = 1 << 1,

    /// <summary>
    /// When they're on the FTL map
    /// </summary>
    Travelling = 1 << 2,

    /// <summary>
    /// Approaching destination, play effects or whatever,
    /// </summary>
    Arriving = 1 << 3,
    Cooldown = 1 << 4,
}


public enum BluespaceState : byte
{
    Invalid = 0,

    /// <summary>
    /// A dummy state for presentation
    /// </summary>
    Available,

    /// <summary>
    /// Sound played and launch started
    /// </summary>
    Starting,

    /// <summary>
    /// When they're on the Bluespace map
    /// </summary>
    Travelling,

    /// <summary>
    /// Approaching destination, play effects or whatever,
    /// </summary>
    Arriving,
    Cooldown,
}
