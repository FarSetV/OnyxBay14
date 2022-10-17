using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
///     HumanoidSystem. Primarily deals with the appearance and visual data
///     of a humanoid entity. HumanoidVisualizer is what deals with actually
///     organizing the sprites and setting up the sprite component's layers.
///     This is a shared system, because while it is server authoritative,
///     you still need a local copy so that players can set up their
///     characters.
/// </summary>
public abstract class SharedHumanoidSystem : EntitySystem
{
    public const string DefaultSpecies = "Human";
    public const string DefaultBodyType = "NormalHuman";
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public void SetAppearance(EntityUid uid,
        string species,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayer,
        Color skinColor,
        Sex sex,
        string bodyType,
        List<HumanoidVisualLayers> visLayers,
        List<Marking> markings)
    {
        var data = new HumanoidVisualizerData(species, customBaseLayer, skinColor, sex, bodyType, visLayers, markings);

        // Locally raise an event for this, because there might be some systems interested
        // in this.
        RaiseLocalEvent(uid, new HumanoidAppearanceUpdateEvent(data), true);
        _appearance.SetData(uid, HumanoidVisualizerKey.Key, data);
    }

    public List<BodyTypePrototype> GetValidBodyTypes(SpeciesPrototype species, Sex sex)
    {
        return species.BodyTypes.Select(protoId => _prototypeManager.Index<BodyTypePrototype>(protoId))
            .Where(proto => !proto.SexRestrictions.Contains(sex.ToString())).ToList();
    }

    public static bool IsBodyTypeValid(BodyTypePrototype bodyType, SpeciesPrototype species, Sex sex)
    {
        return species.BodyTypes.Contains(bodyType.ID);
    }
}

public sealed class HumanoidAppearanceUpdateEvent : EntityEventArgs
{
    public HumanoidAppearanceUpdateEvent(HumanoidVisualizerData data)
    {
        Data = data;
    }

    public HumanoidVisualizerData Data { get; }
}
