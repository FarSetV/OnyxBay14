using System.Diagnostics.CodeAnalysis;
using Content.Server.Bluespace.Events;
using Content.Shared.Bluespace;
using Content.Shared.Bluespace.Events;
using Content.Shared.GameTicking;
using Content.Shared.Parallax;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Bluespace;

public sealed class BluespaceSystem : SharedBluespaceSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    private uint _guestsCounter;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("bluespace");

        SubscribeLocalEvent<BeforeEnterBluespaceEvent>(OnBeforeEnterBluespace);
        SubscribeLocalEvent<AfterExitBluespaceEvent>(OnAfterExitBluespace);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<BluespaceComponent, AfterEnterBluespaceEvent>(OnAfterEnterBluespace);
    }

    private void OnAfterEnterBluespace(EntityUid uid, BluespaceComponent component, AfterEnterBluespaceEvent ev)
    {
        component.State = BluespaceState.Travelling;
    }

    public bool TryEnterBluespace(
        EntityUid uid,
        float enterTime,
        [NotNullWhen(true)] out BluespaceComponent? component,
        [NotNullWhen(false)] out string? reason
    )
    {
        _sawmill.Info($"{ToPrettyString(uid)} tries to enter bluespace");

        if (TryComp(uid, out component))
        {
            reason = Loc.GetString("bluespace-already-in-bluespace");
            return false;
        }

        var attempt = new AttemptEnterBluespaceEvent(uid);
        RaiseLocalEvent(uid, attempt, true);

        if (attempt.Cancelled)
        {
            component = null;
            reason = attempt.Reason!;
            return false;
        }

        component = AddComp<BluespaceComponent>(uid);
        component.Accumulator = enterTime;
        component.State = BluespaceState.Starting;
        reason = null;

        return true;
    }

    public bool TryExitBluespace(EntityUid uid, float exitTime, BluespaceComponent? component,
        [NotNullWhen(false)] out string? reason)
    {
        _sawmill.Info($"{ToPrettyString(uid)} tries to exit bluespace");

        if (!Resolve(uid, ref component))
        {
            reason = Loc.GetString("bluespace-cant-enter-bluespace");
            return false;
        }

        var attempt = new AttemptExitBluespaceEvent(uid);
        RaiseLocalEvent(uid, attempt, true);

        if (attempt.Cancelled)
        {
            reason = attempt.Reason!;
            return false;
        }

        component.State = BluespaceState.Arriving;
        component.Accumulator = exitTime;

        reason = null;
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var component in EntityQuery<BluespaceComponent>())
        {
            component.Accumulator -= frameTime;

            if (component.Accumulator > 0f)
                continue;

            switch (component.State)
            {
                case BluespaceState.Starting:
                    var enterer = component.Owner;

                    var enterEvent = new EnterBluespaceEvent(enterer);
                    RaiseLocalEvent(enterer, enterEvent, true);

                    if (!enterEvent.Handled)
                        RemComp<BluespaceComponent>(enterer);
                    else
                        component.State = BluespaceState.Travelling;

                    break;
                case BluespaceState.Arriving:
                    var exiter = component.Owner;

                    var exitEvent = new ExitBluespaceEvent(exiter);
                    RaiseLocalEvent(exiter, exitEvent, true);

                    RemComp<BluespaceComponent>(exiter);

                    break;
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        CleanupBluespaceMap();
    }

    private void OnAfterExitBluespace(AfterExitBluespaceEvent ev)
    {
        _guestsCounter = Math.Max(0, _guestsCounter - 1);

        if (_guestsCounter != 0)
            return;

        DebugTools.AssertNotNull(BluespaceMapId);
        _mapManager.SetMapPaused(BluespaceMapId!.Value, true);
        _sawmill.Info("freezing bluespace map");
    }

    private void OnBeforeEnterBluespace(BeforeEnterBluespaceEvent ev)
    {
        SetupBluespaceMap();
        _guestsCounter += 1;

        DebugTools.AssertNotNull(BluespaceMapId);
        _mapManager.SetMapPaused(BluespaceMapId!.Value, false);
        _sawmill.Info("unfreezing bluespace map");
    }

    private void SetupBluespaceMap()
    {
        if (BluespaceMapId is not null && _mapManager.MapExists(BluespaceMapId.Value))
            return;

        BluespaceMapId = _mapManager.CreateMap();
        _sawmill.Info($"created bluespace map: {BluespaceMapId}");
        DebugTools.Assert(!_mapManager.IsMapPaused(BluespaceMapId.Value));

        var parallax = EnsureComp<ParallaxComponent>(_mapManager.GetMapEntityId(BluespaceMapId.Value));
        parallax.Parallax = "FastSpace";
        RaiseNetworkEvent(new BluespaceMapUpdatedEvent(BluespaceMapId.Value));
    }

    private void CleanupBluespaceMap()
    {
        if (BluespaceMapId is null)
            return;

        _sawmill.Info($"deleting bluespace map: {BluespaceMapId}");
        if (_mapManager.MapExists(BluespaceMapId.Value))
            _mapManager.DeleteMap(BluespaceMapId.Value);

        BluespaceMapId = null;
    }

    public MapId GetBluespace()
    {
        if (BluespaceMapId is null || !_mapManager.MapExists(BluespaceMapId.Value))
            SetupBluespaceMap();

        DebugTools.AssertNotNull(BluespaceMapId);

        return BluespaceMapId!.Value;
    }
}
