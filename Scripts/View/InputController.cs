using Godot;
using System;
using Deckrinth.Core;

namespace Deckrinth.View;

public partial class InputController : Node
{
    private GameRoot _gameRoot;
    private GridManager _grid;
    private Vector2I _lastHoveredTile = new Vector2I(-999, -999);
    
    public override void _Ready()
    {
        _gameRoot = GetParent<GameRoot>();
        _grid = _gameRoot.GridView;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            // Mouse position in relation to camera offset
            Vector2 mousePos = _grid.GetGlobalMousePosition();
            
            // Convert back
            Vector2I clickedTile = IsometricToLogical(mousePos);
            
            // Send click to manager
            _gameRoot.HandleGridClick(clickedTile);
        }
        if (@event is InputEventMouseMotion)
        {
            Vector2 mousePos = _grid.GetGlobalMousePosition();
            Vector2I hoveredTile = IsometricToLogical(mousePos);

            // Optimization to call the function when tiles change not per frame
            if (hoveredTile != _lastHoveredTile)
            {
                _lastHoveredTile = hoveredTile;
                _gameRoot.HandleGridHover(hoveredTile);
            }
        }
    }

    // Inverse grid space to mouse conversion
    private Vector2I IsometricToLogical(Vector2 screenPos)
    {
        float halfW = 120f / 2f;
        float halfH = 74f / 2f;

        float logicY = (screenPos.Y / halfH - screenPos.X / halfW) / 2f;
        float logicX = (screenPos.Y / halfH + screenPos.X / halfW) / 2f;

        return new Vector2I((int)Math.Round(logicX), (int)Math.Round(logicY));
    }
}