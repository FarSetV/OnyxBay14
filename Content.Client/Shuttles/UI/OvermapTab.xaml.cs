﻿using System.Linq;
using Content.Shared.Bluespace;
using Content.Shared.Overmap;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class OvermapTab : Control
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private OvermapNavigatorBoundInterfaceState? _state;
    private OvermapPointState? _self;

    public Action? BluespaceEnterPressed;
    public Action? BluespaceExitPressed;

    public OvermapTab()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        BluespaceButton.OnPressed += OnBluespaceButtonPressed;
    }

    private void OnBluespaceButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        if (_state is null || _self is null)
            return;

        if (_state.BluespaceState is not {} bluespaceState)
        {
            BluespaceEnterPressed?.Invoke();
        } else if (bluespaceState == BluespaceState.Travelling)
        {
            BluespaceExitPressed?.Invoke();
        }
    }

    public void UpdateState(OvermapNavigatorBoundInterfaceState state)
    {
        _state = state;
        _self = _state.OvermapPoints.FirstOrDefault(point => point.EntityUid == _state.ParentGrid);
        Navigator.UpdateState(state);

        if (_state.BluespaceState is not {} bluespaceState)
        {
            BluespaceButton.Disabled = false;
            BluespaceButton.Text = Loc.GetString("shuttle-console-enter-bluespace-button");
        }
        else if (bluespaceState == BluespaceState.Travelling)
        {
            BluespaceButton.Disabled = false;
            BluespaceButton.Text = Loc.GetString("shuttle-console-exit-bluespace-button");
        }
        else
        {
            BluespaceButton.Disabled = _state.EnginesCooldown > float.Epsilon;
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_state is null || _self is null)
            return;

        var xForm = _entityManager.GetComponent<TransformComponent>(_state.ParentGrid);
        var localPosition = xForm.WorldPosition;
        LocalPosition.Text = $"{localPosition.X:0.0}, {localPosition.Y:0.0}";

        var tilePosition = _self.TilePosition;
        TilePosition.Text = _self.InBluespace ? "-, -" : $"{tilePosition.X}, {tilePosition.Y}";
    }
}
