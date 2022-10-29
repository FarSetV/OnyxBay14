using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components
{
    [NetSerializable, Serializable]
    public enum RadiationCollectorVisuals
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum RadiationCollectorSlotVisuals
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum RadiationCollectorVisualState
    {
        Active,
        Inactive
    }

    [NetSerializable, Serializable]
    public enum RadiationCollectorSlotVisualState
    {
        Occupied,
        Free
    }
}
