using System.Threading.Tasks;
using Content.Server.Overmap;
using Content.Server.Overmap.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.Shuttles;

[TestFixture]
[TestOf(typeof(ShuttleSystem))]
public sealed class BluespaceTests
{
    [Test]
    public async Task TestEnterExitBluespace()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
        var server = pairTracker.Pair.Server;
        await server.WaitIdleAsync();

        var mapManager = server.ResolveDependency<IMapManager>();
        var entitiesManager = server.ResolveDependency<IEntityManager>();
        var random = server.ResolveDependency<IRobustRandom>();
        var shuttleSystem = entitiesManager.System<ShuttleSystem>();
        var overmapSystem = entitiesManager.System<OvermapSystem>();

        var shuttleGridEn = EntityUid.Invalid;
        var startTilePosition = Vector2i.Zero;
        var startLocalPosition = Vector2.Zero;
        BluespaceComponent bpComponent;

        await server.WaitAssertion(() =>
        {
            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            shuttleGridEn = grid.GridEntityId;

            entitiesManager.EnsureComponent(shuttleGridEn, out TransformComponent xForm);
            entitiesManager.EnsureComponent(shuttleGridEn, out OvermapObjectComponent _);
            entitiesManager.EnsureComponent(shuttleGridEn, out ShuttleComponent shuttle);

            xForm.WorldPosition = new Vector2(random.Next(-1000, 1000), random.Next(-1000, 1000));
            startLocalPosition = xForm.WorldPosition;
            var tilePosition = overmapSystem.GetTileEntityOn(shuttleGridEn)?.Position;

            Assert.That(tilePosition, Is.Not.Null);

            startTilePosition = tilePosition.Value;

            Assert.That(shuttleSystem.TryEnterBluespace(shuttle, out bpComponent), Is.True);
            bpComponent!.Accumulator = 0;
        });

        await server.WaitRunTicks(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(overmapSystem.IsEntityInBluespace(shuttleGridEn), Is.True);

            bpComponent = entitiesManager.GetComponent<BluespaceComponent>(shuttleGridEn);
            shuttleSystem.ExitBluespace(shuttleGridEn, bpComponent);

            bpComponent.Accumulator = 0;
        });

        await server.WaitRunTicks(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(overmapSystem.IsEntityInBluespace(shuttleGridEn), Is.False);

            var xForm = entitiesManager.GetComponent<TransformComponent>(shuttleGridEn);
            var tilePosition = overmapSystem.GetTileEntityOn(shuttleGridEn)!.Position;

            Assert.That(xForm.WorldPosition.X, Is.EqualTo(startLocalPosition.X).Within(1f));
            Assert.That(xForm.WorldPosition.Y, Is.EqualTo(startLocalPosition.Y).Within(1f));
            Assert.That(tilePosition.X, Is.EqualTo(startTilePosition.X));
            Assert.That(tilePosition.Y, Is.EqualTo(startTilePosition.Y));
        });
    }

    [Test]
    public async Task TestBluespaceExitsPlaceOccupied()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
        var server = pairTracker.Pair.Server;
        await server.WaitIdleAsync();

        var mapManager = server.ResolveDependency<IMapManager>();
        var entitiesManager = server.ResolveDependency<IEntityManager>();
        var shuttleSystem = entitiesManager.System<ShuttleSystem>();

        var shuttleGridEn = EntityUid.Invalid;
        BluespaceComponent bpComponent;
        var mapId = MapId.Nullspace;

        await server.WaitAssertion(() =>
        {
            mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            shuttleGridEn = grid.GridEntityId;

            entitiesManager.EnsureComponent(shuttleGridEn, out TransformComponent xForm);
            entitiesManager.EnsureComponent(shuttleGridEn, out OvermapObjectComponent _);
            entitiesManager.EnsureComponent(shuttleGridEn, out ShuttleComponent shuttle);

            xForm.WorldPosition = new Vector2(0, 0);

            Assert.That(shuttleSystem.TryEnterBluespace(shuttle, out bpComponent), Is.True);
            bpComponent!.Accumulator = 0;
        });

        await server.WaitRunTicks(30);

        await server.WaitAssertion(() =>
        {
            var secondGrid = mapManager.CreateGrid(mapId);

            entitiesManager.EnsureComponent(secondGrid.GridEntityId, out TransformComponent xForm);

            xForm.WorldPosition = new Vector2(0, 0);

            Assert.That(shuttleSystem.ExitBluespace(shuttleGridEn), Is.False);
        });
    }

    [Test]
    public async Task TestBluespaceExitsPlaceNotOccupied()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
        var server = pairTracker.Pair.Server;
        await server.WaitIdleAsync();

        var mapManager = server.ResolveDependency<IMapManager>();
        var entitiesManager = server.ResolveDependency<IEntityManager>();
        var shuttleSystem = entitiesManager.System<ShuttleSystem>();

        var shuttleGridEn = EntityUid.Invalid;
        BluespaceComponent bpComponent;
        var mapId = MapId.Nullspace;

        await server.WaitAssertion(() =>
        {
            mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            shuttleGridEn = grid.GridEntityId;

            entitiesManager.EnsureComponent(shuttleGridEn, out TransformComponent xForm);
            entitiesManager.EnsureComponent(shuttleGridEn, out OvermapObjectComponent _);
            entitiesManager.EnsureComponent(shuttleGridEn, out ShuttleComponent shuttle);

            xForm.WorldPosition = new Vector2(0, 0);

            Assert.That(shuttleSystem.TryEnterBluespace(shuttle, out bpComponent), Is.True);
            bpComponent!.Accumulator = 0;
        });

        await server.WaitRunTicks(30);

        await server.WaitAssertion(() =>
        {
            var secondGrid = mapManager.CreateGrid(mapId);

            entitiesManager.EnsureComponent(secondGrid.GridEntityId, out TransformComponent xForm);

            xForm.WorldPosition = new Vector2(500, 500);

            Assert.That(shuttleSystem.ExitBluespace(shuttleGridEn), Is.True);
        });
    }
}
