using Content.Server.Bluespace;
using Content.Server.BluespaceOvermapTransition;
using Content.Shared.Overmap;
using Content.Shared.Overmap.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Server.Overmap.Systems;

public sealed class OvermapNavigatorSystem : SharedOvermapNavigatorSystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly BluespaceOvermapTransitionSystem _transition = default!;
    [Dependency] private readonly OvermapSystem _overmap = default!;
    [Dependency] private readonly BluespaceSystem _bluespace = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvermapNavigatorComponent, ComponentGetState>(OnComponentGetState);
    }

    private void OnComponentGetState(EntityUid uid, OvermapNavigatorComponent component, ref ComponentGetState args)
    {
        args.State = new OvermapNavigatorComponentState
        {
            SignatureRadius = component.SignatureRadius,
            FFIRadius = component.IFFRadius,
            Points = component.Points
        };
    }

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(5);
        var iffQuery = GetEntityQuery<IFFComponent>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var xFormQuery = GetEntityQuery<TransformComponent>();

        foreach (var navigator in _entityManager.EntityQuery<OvermapNavigatorComponent>())
        {
            navigator.Points = new List<OvermapPointState>();
            var navigatorXForm = xFormQuery.GetComponent(navigator.Owner);

            foreach (var overmapObject in _overmap.GetOvermapObjects())
            {
                var entity = overmapObject.Owner;
                var xForm = xFormQuery.GetComponent(entity);
                var isSelf = navigatorXForm.GridUid == xForm.GridUid;

                if (!overmapObject.ShowOnOvermap && !isSelf)
                    continue;

                var distance = _transition.GetDistance(navigator.Owner, entity);

                if (distance is null)
                    continue;

                var canSee = navigator.SignatureRadius >= distance || isSelf;

                if (!canSee)
                    continue;

                var canSeeName = navigator.IFFRadius >= distance || isSelf;
                string? name = null;
                var color = Color.Yellow;

                if (canSeeName)
                {
                    if (iffQuery.TryGetComponent(entity, out var iff))
                    {
                        color = iff.Color;

                        if ((iff.Flags & IFFFlags.Hide) != 0 && !isSelf)
                            continue;

                        if ((iff.Flags & IFFFlags.HideLabel) == 0 && !isSelf)
                        {
                            var meta = metaQuery.GetComponent(entity);
                            name = meta.EntityName;
                        }
                    }
                    else
                    {
                        var meta = metaQuery.GetComponent(entity);
                        name = meta.EntityName;
                    }
                }

                if (string.IsNullOrEmpty(name?.Trim()))
                    name = null;

                navigator.Points.Add(new OvermapPointState
                {
                    VisibleName = name,
                    EntityUid = entity,
                    TilePosition = _overmap.GetTileEntityOn(entity, xFormQuery)?.Position ?? Vector2i.Zero,
                    InBluespace = _bluespace.IsEntityInBluespace(entity, xForm),
                    Color = color
                });
            }

            RaiseLocalEvent(navigator.Owner, new OvermapNavigatorUpdated(navigator));
            Dirty(navigator);
        }
    }
}
