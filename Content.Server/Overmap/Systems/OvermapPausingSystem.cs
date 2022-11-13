using System.Linq;
using Content.Server.Station.Systems;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Overmap.Systems;

public sealed class OvermapPausingSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly OvermapSystem _overmap = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(10);

        var xFormQuery = GetEntityQuery<TransformComponent>();
        var mapsToPause = _overmap.Tiles.GetMapIds().ToHashSet();

        // TODO: Some tests may fail without people :(
        if (mapsToPause.Count == 0 || !_playerManager.ServerSessions.Any())
            return;

        foreach (var playerSession in _playerManager.ServerSessions)
        {
            if (playerSession.AttachedEntity is not { } playerEntity)
                continue;

            var xForm = xFormQuery.GetComponent(playerEntity);
            mapsToPause.Remove(xForm.MapID);
        }

        foreach (var mapId in mapsToPause)
        {
            _mapManager.SetMapPaused(mapId, true);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;

        if (e.Session.AttachedEntity is not { } entity)
            return;

        var xForm = Transform(entity);

        if (!_overmap.Tiles.TryGetByMapId(xForm.MapID, out _))
            return;

        _mapManager.SetMapPaused(xForm.MapID, false);
    }
}
