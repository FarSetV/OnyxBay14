using Content.Server.Singularity.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Singularity.Components
{
    /// <summary>
    ///     Generates electricity from radiation.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(RadiationCollectorSystem))]
    public sealed class RadiationCollectorComponent : Component
    {
        /// <summary>
        ///     How much joules will collector generate for each rad.
        /// </summary>
        [DataField("chargeModifier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ChargeModifier = 700f;

        /// <summary>
        ///     How much the collector heats per produced joule.
        /// </summary>
        [DataField("kelvinsPerJoule")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float KelvinsPerJoule = 0.008f;

        /// <summary>
        ///     "HP" of the collector.
        /// </summary>
        [DataField("integrity")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Integrity = 100f;

        // 400C
        [DataField("temperatureThreshold")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float TemperatureThreshold = 373.15f;

        /// <summary>
        ///     Per impulse.
        /// </summary>
        [DataField("drainMoles")]
        [ViewVariables(VVAccess.ReadWrite)]
        public static readonly float DrainMoles = 0.001f;

        /// <summary>
        ///     Cooldown time between users interaction.
        /// </summary>
        [DataField("cooldown")]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Cooldown = TimeSpan.FromSeconds(0.81f);

        /// <summary>
        ///     Describes the multiplier of a radiation's input power. So, from some gases we can get more power
        ///     than from others.
        /// </summary>
        [DataField("gasMultiplier")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float[] GasMultiplier = new float[Atmospherics.AdjustedNumberOfGases];

        [DataField("tankSlot", required: true)]
        public string TankSlot = "tankSlot";

        /// <summary>
        ///     Was machine activated by user?
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)] public bool Enabled;

        /// <summary>
        ///     Timestamp when machine can be deactivated again.
        /// </summary>
        public TimeSpan CoolDownEnd;

        /// <summary>
        ///     How much joules did produced the last time.
        /// </summary>
        public float LastProducedPower = 0.0f;

        /// <summary>
        ///     How much kelvins did produced the last time.
        /// </summary>
        public float LastProducedHeat = 0.0f;
    }
}
