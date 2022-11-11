using Robust.Shared.Audio;

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
public sealed class ShuttleComponent : Component
{
    /// <summary>
    ///     The thrusters contributing to the angular impulse of the shuttle.
    /// </summary>
    public readonly List<ThrusterComponent> AngularThrusters = new();

    /// <summary>
    ///     The cached thrust available for each cardinal direction
    /// </summary>
    [ViewVariables] public readonly float[] LinearThrust = new float[4];

    /// <summary>
    ///     The thrusters contributing to each direction for impulse.
    /// </summary>
    public readonly List<ThrusterComponent>[] LinearThrusters = new List<ThrusterComponent>[4];

    [ViewVariables] public float AngularThrust = 0f;

    [ViewVariables] public bool Enabled = true;

    [ViewVariables] public float EnginesCooldown = 0f;

    /// <summary>
    ///     A bitmask of all the directions we are considered thrusting.
    /// </summary>
    [ViewVariables] public DirectionFlag ThrustDirections = DirectionFlag.None;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("soundTravel")]
    public SoundSpecifier? TravelSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_progress.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f).WithLoop(true)
    };

    public IPlayingAudioStream? TravelStream;
}
