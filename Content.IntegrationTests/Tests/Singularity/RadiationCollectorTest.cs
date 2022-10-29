using System;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Singularity;

internal sealed class TestHelper : IAsyncDisposable
{
    private const int WaitTicks = 100;

    private const string Prototypes = @"
- type: entity
  id: DummyRadiationCollector
  name: radiation collector
  components:
  - type: Physics
    bodyType: Static
  - type: Fixtures
  - type: Transform
    anchored: true
    noRot: true
  - type: NodeContainer
    examinable: true
    nodes:
      input:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: RadiationCollector
    tankSlot: tankSlot
    gasMultiplier:
      - 0.0
      - 0.0
      - 0.0
      - 4.0 # plasma
    # Note that this doesn't matter too much (see next comment)
    # However it does act as a cap on power receivable via the collector.
  - type: Battery
    maxCharge: 100000
    startingCharge: 0
  - type: BatteryDischarger
    # This is JUST a default. It has to be dynamically adjusted to ensure that the battery doesn't discharge ""too fast"" & run out immediately, while still scaling by input power.
    activeSupplyRate: 100000
  - type: RadiationReceiver
  - type: Anchorable
  - type: PowerNetworkBattery
    maxSupply: 1000000000
    supplyRampTolerance: 1000000000
  - type: ItemSlots
    slots:
      tankSlot:
        whitelist:
          components:
            - GasTank
  - type: ContainerContainer
    containers:
      tankSlot: !type:ContainerSlot

- type: entity
  name: radiation source
  id: DummyRadiationSource
  components:
  - type: RadiationSource
    intensity: 5

- type: entity
  id: DummyTank
  parent: GasTankBase
  name: dummy tank
  components:
    - type: GasTank
";

    public IEntityManager EntityManager;
    public TestMapData Map;
    public PairTracker PairTracker;
    public IEntitySystemManager SystemManager;

    private TestHelper()
    {
    }

    private RadiationCollectorSystem CollectorSystem => SystemManager.GetEntitySystem<RadiationCollectorSystem>();

    public async ValueTask DisposeAsync()
    {
        await PairTracker.DisposeAsync();
    }

    public static async Task<TestHelper> Init()
    {
        var helper = new TestHelper();

        helper.PairTracker = await PoolManager.GetServerClient(new PoolSettings
            { NoClient = true, ExtraPrototypes = Prototypes });

        var server = helper.PairTracker.Pair.Server;
        await server.WaitIdleAsync();

        helper.EntityManager = server.ResolveDependency<IEntityManager>();
        helper.SystemManager = helper.EntityManager.EntitySysManager;
        helper.Map = await PoolManager.CreateTestMap(helper.PairTracker);

        return helper;
    }

    public (EntityUid collector, EntityUid radSource) SpawnStuff()
    {
        var collector = EntityManager.SpawnEntity("DummyRadiationCollector", Map.GridCoords);
        var radSource = EntityManager.SpawnEntity("DummyRadiationSource", Map.GridCoords);

        return (collector, radSource);
    }

    public void ToggleCollector(EntityUid uid)
    {
        EntityManager.TryGetComponent<RadiationCollectorComponent>(uid, out var collectorComponent);

        Assert.That(collectorComponent, Is.Not.Null);

        var wasEnabled = collectorComponent.Enabled;
        CollectorSystem.ToggleCollector(uid, null, collectorComponent);

        Assert.That(collectorComponent.Enabled, Is.Not.EqualTo(wasEnabled));
    }

    public GasMixture MakePlasmaMixture(float moles = 11.3f)
    {
        var mix = new GasMixture();

        mix.AdjustMoles(Gas.Plasma, moles);
        mix.Temperature = Atmospherics.T20C;

        return mix;
    }

    public GasMixture MakeOxygenMixture(float moles = 11.3f)
    {
        var mix = new GasMixture();

        mix.AdjustMoles(Gas.Oxygen, moles);
        mix.Temperature = Atmospherics.T20C;

        return mix;
    }

    public GasTankComponent PushTankWithMixture(EntityUid uid, GasMixture mixture)
    {
        var collectorComponent = EntityManager.GetComponent<RadiationCollectorComponent>(uid);
        var slotSystem = SystemManager.GetEntitySystem<ItemSlotsSystem>();
        var coords = Map.GridCoords;

        var tank = EntityManager.SpawnEntity("DummyTank", coords);
        Assert.That(EntityManager.TryGetComponent(tank, out GasTankComponent tankComponent), Is.True);

        tankComponent.Air.CopyFromMutable(mixture);
        Assert.That(slotSystem.TryInsert(uid, collectorComponent.TankSlot, tank, null), Is.True);

        return tankComponent;
    }

    public async Task Run(Action before, Action after)
    {
        await PairTracker.Pair.Server.WaitAssertion(before);
        await PoolManager.RunTicksSync(PairTracker.Pair, WaitTicks);
        await PairTracker.Pair.Server.WaitAssertion(after);
    }
}

[TestFixture]
[TestOf(typeof(RadiationCollectorComponent))]
[TestOf(typeof(RadiationCollectorSystem))]
public sealed class RadiationCollectorTest
{
    [Test]
    public async Task CollectorDoesNotProduceEnergyWithoutTank()
    {
        await using var helper = await TestHelper.Init();

        EntityUid collector = default!;

        await helper.Run(() =>
        {
            (collector, _) = helper.SpawnStuff();
            helper.ToggleCollector(collector);
        }, () =>
        {
            var component = helper.EntityManager.GetComponent<RadiationCollectorComponent>(collector);

            Assert.Multiple(() =>
            {
                Assert.That(component.LastProducedPower, Is.EqualTo(0.0f));
                Assert.That(component.LastProducedHeat, Is.EqualTo(0.0f));
            });
        });
    }

    [Test]
    public async Task CollectorProducesEnergyWithPlasmaTank()
    {
        await using var helper = await TestHelper.Init();

        EntityUid collector = default!;

        await helper.Run(() =>
        {
            (collector, _) = helper.SpawnStuff();

            helper.ToggleCollector(collector);
            helper.PushTankWithMixture(collector, helper.MakePlasmaMixture());
        }, () =>
        {
            var component = helper.EntityManager.GetComponent<RadiationCollectorComponent>(collector);

            Assert.Multiple(() =>
            {
                Assert.That(component.LastProducedPower, Is.GreaterThan(0.0f));
                Assert.That(component.LastProducedHeat, Is.GreaterThan(0.0f));
            });
        });
    }

    [Test]
    public async Task CollectorDoesNotProduceEnergyWithZeroMultiplier()
    {
        await using var helper = await TestHelper.Init();

        EntityUid collector = default!;

        await helper.Run(() =>
        {
            (collector, _) = helper.SpawnStuff();

            helper.ToggleCollector(collector);
            // Oxygen has zero multiplier in the dummy prototype.
            helper.PushTankWithMixture(collector, helper.MakeOxygenMixture());
        }, () =>
        {
            var component = helper.EntityManager.GetComponent<RadiationCollectorComponent>(collector);

            Assert.Multiple(() =>
            {
                Assert.That(component.LastProducedPower, Is.EqualTo(0.0f));
                Assert.That(component.LastProducedHeat, Is.EqualTo(0.0f));
            });
        });
    }

    [Test]
    public async Task CollectorDrainsMolesFromTank()
    {
        await using var helper = await TestHelper.Init();

        EntityUid collector = default!;
        GasTankComponent tankComponent = default!;
        var molesBefore = 0.0f;

        await helper.Run(() =>
        {
            (collector, _) = helper.SpawnStuff();

            helper.ToggleCollector(collector);
            tankComponent = helper.PushTankWithMixture(collector, helper.MakePlasmaMixture());
            molesBefore = tankComponent.Air.TotalMoles;
        }, () =>
        {
            var component = helper.EntityManager.GetComponent<RadiationCollectorComponent>(collector);

            Assert.Multiple(() =>
            {
                Assert.That(component.LastProducedPower, Is.GreaterThan(0.0f));
                Assert.That(component.LastProducedHeat, Is.GreaterThan(0.0f));
                Assert.That(tankComponent.Air.TotalMoles, Is.LessThan(molesBefore));
            });
        });
    }
}
