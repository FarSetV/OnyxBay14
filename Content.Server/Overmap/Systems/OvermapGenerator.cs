using System.Linq;
using Content.Server.BluespaceOvermapTransition;
using Content.Server.GameTicking.Events;
using Content.Server.Overmap.Prototypes;
using Content.Shared.Bluespace;
using Content.Shared.CCVar;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Overmap.Systems;

public sealed class OvermapGenerator : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly OvermapSystem _overmap = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BluespaceOvermapTransitionSystem _transition = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("overmap_gen");
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        if (!_cfg.GetCVar(CCVars.OvermapGenerate))
            return;

        var presetId = _cfg.GetCVar(CCVars.OvermapPreset);
        _sawmill.Info($"using preset \"{presetId}\"");

        var presetPrototype = _prototype.Index<OvermapPresetPrototype>(presetId);

        foreach (var layerId in presetPrototype.Layers)
        {
            var layerPrototype = _prototype.Index<OvermapLayerPrototype>(layerId);

            GenerateLayer(layerPrototype);
        }
    }

    private void GenerateLayer(OvermapLayerPrototype layer)
    {
        var bluespaceSize = SharedBluespaceSystem.OvermapBluespaceSize;
        var loadedGrids = 0;

        if (!Enum.TryParse(layer.NoiseType, out NoiseGenerator.NoiseType noiseType))
        {
            _sawmill.Error($"invalid noise type: {layer.NoiseType}");
            return;
        }

        var noise = new NoiseGenerator(noiseType);

        noise.SetFrequency(layer.Frequency);
        noise.SetLacunarity(layer.Lacunarity);
        noise.SetOctaves(layer.Octaves);
        noise.SetPersistence(layer.Persistence);
        noise.SetPeriodX(bluespaceSize.X);
        noise.SetPeriodY(bluespaceSize.Y);
        noise.SetSeed((uint) _random.Next());

        var position = Vector2.Zero;
        var gridPrototypes = layer.Grids.Select(id => _prototype.Index<OvermapLayerContentPrototype>(id)).ToList();
        Dictionary<Vector2i, List<string>> tileUniquePrototypes = new();
        List<string> uniquePrototypes = new();
        var xFormQuery = GetEntityQuery<TransformComponent>();

        while (position.X < bluespaceSize.X || position.Y < bluespaceSize.Y)
        {
            if (position.X >= bluespaceSize.X)
            {
                position.Y += layer.MinDensity;
                position.X = 0;
            }

            var tilePosition = _transition.BluespacePositionToTilePosition(position);
            var flooredTilePosition = tilePosition.Floored();
            var noiseValue = noise.GetNoiseTiled(position);

            if (noiseValue > layer.MinNoise && _random.Prob(noiseValue))
            {
                var gridToSpawn = gridPrototypes.FirstOrDefault(grid =>
                {
                    if (grid.UniquePerTile && tileUniquePrototypes.TryGetValue(flooredTilePosition, out var uniques) &&
                        uniques.Contains(grid.ID))
                        return false;

                    if (grid.Unique && uniquePrototypes.Contains(grid.ID))
                        return false;

                    return _random.Prob(grid.Chance);
                });

                if (gridToSpawn is not null)
                {
                    if (gridToSpawn.UniquePerTile)
                    {
                        if (!tileUniquePrototypes.TryGetValue(flooredTilePosition, out var value))
                        {
                            tileUniquePrototypes[flooredTilePosition] = new List<string>
                            {
                                gridToSpawn.ID
                            };
                        }
                        else
                            tileUniquePrototypes[flooredTilePosition].Add(gridToSpawn.ID);
                    }

                    if (gridToSpawn.Unique)
                        uniquePrototypes.Add(gridToSpawn.ID);

                    var localPosition = _transition.BluespacePositionToLocalPosition(position, tilePosition);
                    var angle = _random.NextAngle();
                    var box2 = Box2.CenteredAround(localPosition, gridToSpawn.Bounds.Size);
                    var box2Rotated = new Box2Rotated(box2, angle, localPosition);
                    var mapId = _overmap.GetMapForTileOrCreate(flooredTilePosition);

                    if (_map.FindGridsIntersecting(mapId, box2Rotated).Any())
                        continue;

                    var (_, grid) = _mapLoader.LoadGrid(mapId, gridToSpawn.MapPath.ToString());

                    if (grid is not null)
                    {
                        var xForm = xFormQuery.GetComponent(grid.Value);
                        xForm.WorldPosition = localPosition;
                        xForm.WorldRotation = angle;
                        var comp = EnsureComp<OvermapObjectComponent>(grid.Value);
                        comp.ShowOnOvermap = gridToSpawn.ShowOnOvermap;

                        loadedGrids += 1;
                    }
                }
            }

            position.X += layer.MinDensity;
        }

        _sawmill.Info($"loaded grids by layer \"{layer.ID}\": {loadedGrids}");
    }
}
