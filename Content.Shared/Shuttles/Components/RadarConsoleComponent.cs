using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedRadarConsoleSystem))]
public sealed class RadarConsoleComponent : Component
{
    [DataField("maxRange")] private float _maxRange = 256f;
    [DataField("rotation")] private Angle _rotation = Angle.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxRange
    {
        get => _maxRange;
        set
        {
            _maxRange = value;
            Dirty();
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public double Rotation
    {
        get => _rotation.Degrees;
        set
        {
            _rotation = Angle.FromDegrees(value);
            Dirty();
        }
    }
}
