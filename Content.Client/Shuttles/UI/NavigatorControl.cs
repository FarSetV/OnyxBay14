using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Overmap;
using Content.Shared.Overmap.Systems;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Physics.Components;

namespace Content.Client.Shuttles.UI;

public sealed class NavigatorControl : Control
{
    private Font _labelsFonts = default!;
    private const int PixelDensity = 32;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly Dictionary<EntityUid, Label> _pointsLabels = new();
    private OvermapNavigatorBoundInterfaceState? _state;

    public NavigatorControl()
    {
        IoCManager.InjectDependencies(this);

        _labelsFonts = _cache.Exo2Stack(size: (int) (8 * UIScale));
        MinSize = SharedOvermapSystem.OvermapBluespaceSize / PixelDensity;
        RectClipContent = true;
    }

    private float PointRadius => 2f * UIScale;

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        foreach (var label in _pointsLabels.Values)
        {
            RemoveChild(label);
            label.Dispose();
        }

        _pointsLabels.Clear();

        base.Dispose(disposing);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_state is null)
            return;

        base.Draw(handle);

        handle.DrawRect(PixelSizeBox, Color.Black);

        DrawRanges(handle);
        DrawGrid(handle);
        DrawPoints(handle);
    }

    private Vector2 TilePixelSize()
    {
        return PixelSizeBox.Size / SharedOvermapSystem.OvermapTilesCount;
    }

    private void DrawGrid(DrawingHandleScreen handle)
    {
        var tilesCount = SharedOvermapSystem.OvermapTilesCount;
        var tilesSize = TilePixelSize();

        for (var columnX = 0; columnX < tilesCount.X; columnX++)
        {
            var from = new Vector2(tilesSize.X * columnX, 0);
            var to = new Vector2(from.X, PixelSizeBox.Bottom);
            handle.DrawLine(from, to, new Color(15, 15, 15));
        }

        for (var columnY = 0; columnY < tilesCount.Y; columnY++)
        {
            var from = new Vector2(0, tilesSize.Y * columnY);
            var to = new Vector2(PixelSizeBox.Right, from.Y);
            handle.DrawLine(from, to, new Color(15, 15, 15));
        }
    }

    private Vector2 BluespacePixelDensity()
    {
        return SharedOvermapSystem.OvermapBluespaceSize / PixelSizeBox.Size;
    }

    private void DrawPoints(DrawingHandleScreen handle)
    {
        foreach (var label in _pointsLabels.Values)
        {
            label.Visible = false;
        }

        foreach (var point in _state!.OvermapPoints)
        {
            var isSelf = point.EntityUid == _state.ParentGrid;
            var color = isSelf ? Color.Green : point.Color;
            var position = GetAbsoluteOvermapPosition(point);

            if (isSelf)
            {
                var physics = _entityManager.GetComponent<PhysicsComponent>(point.EntityUid);
                handle.DrawLine(position, position + physics.LinearVelocity * 0.5f * UIScale, Color.White);
            }

            handle.DrawCircle(position, PointRadius, color);

            if (point.VisibleName is null)
            {
                if (!_pointsLabels.TryGetValue(point.EntityUid, out var label))
                    continue;

                RemoveChild(label);
                label.Dispose();

                _pointsLabels.Remove(point.EntityUid);
            }
            else
            {
                if (!_pointsLabels.TryGetValue(point.EntityUid, out var label))
                {
                    label = new Label
                    {
                        HorizontalAlignment = HAlignment.Left
                    };

                    _pointsLabels[point.EntityUid] = label;
                    AddChild(label);
                }

                label.Text = point.VisibleName;
                label.FontOverride = _labelsFonts;
                label.FontColorOverride = color;
                label.Visible = true;

                LayoutContainer.SetPosition(label,
                    position / UIScale - new Vector2(label.Width / 2f, label.Height + 5f));
            }
        }
    }

    private Vector2 GetAbsoluteOvermapPosition(OvermapPointState point)
    {
        var pixelsDensity = BluespacePixelDensity();
        var xForm = _entityManager.GetComponent<TransformComponent>(point.EntityUid);

        if (point.InBluespace)
        {
            var bluespaceSize = SharedOvermapSystem.OvermapBluespaceSize;

            var worldPosition = new Vector2(
                Math.Clamp(xForm.WorldPosition.X, 0, bluespaceSize.X),
                Math.Clamp(xForm.WorldPosition.Y, 0, bluespaceSize.Y)
            );

            // TODO: Anyway this has a small offset sowehow 🤯
            return worldPosition / pixelsDensity;
        }

        var tilePixelsSize = TilePixelSize();
        var tilePosition = point.TilePosition;
        var tileCenterPosition = tilePosition * tilePixelsSize + tilePixelsSize / 2;
        var tileHalfSize = SharedOvermapSystem.OvermapTileSize / 2f;
        var tileRelativePosition = new Vector2(
            Math.Clamp(xForm.WorldPosition.X, -tileHalfSize + 150, tileHalfSize - 150),
            Math.Clamp(xForm.WorldPosition.Y, -tileHalfSize + 150, tileHalfSize - 150)
        );

        return tileCenterPosition + tileRelativePosition * SharedOvermapSystem.ScaleFactor / pixelsDensity;
    }

    private void DrawRanges(DrawingHandleScreen handle)
    {
        var me = _state!.OvermapPoints.FirstOrDefault(point => point.EntityUid == _state!.ParentGrid);

        if (me is null)
            return;

        var pixelsDensity = BluespacePixelDensity();
        var myPosition = GetAbsoluteOvermapPosition(me);

        var iffRadius = _state.IFFRadius / pixelsDensity.X;
        var signatureRadius = _state.SignatureRadius / pixelsDensity.X;

        handle.DrawCircle(myPosition, iffRadius, new Color(55, 55, 55));
        handle.DrawCircle(myPosition, iffRadius, new Color(0, 255, 0), false);

        handle.DrawCircle(myPosition, signatureRadius, new Color(255, 255, 0), false);
    }

    public void UpdateState(OvermapNavigatorBoundInterfaceState state)
    {
        _state = state;
    }
}
