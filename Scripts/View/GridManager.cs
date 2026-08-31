using Godot;
using Deckrinth.Model;
using System.Collections.Generic;
using Deckrinth.Services;
namespace Deckrinth.View;

public partial class GridManager : Node2D
{
    [Export] public PackedScene TilePrefab { get; set; } 
    public enum HighlightType { Movement, Attack, EnemyIntent }
    [Export] public PackedScene MoveHighlightPrefab { get; set; }
    [Export] public PackedScene AttackHighlightPrefab { get; set; }
    private List<Node2D> _playerHighlights = new List<Node2D>();
    private List<Node2D> _enemyHighlights = new List<Node2D>();
    [Export] public PackedScene ChestPrefab { get; set; }
    private Dictionary<Vector2I, Node2D> _chestVisuals = new Dictionary<Vector2I, Node2D>(); // To remember the chests on the map
    
    // Dimensions of the tiles
    private const int TILE_WIDTH_PX = 110; 
    private const int TILE_HEIGHT_PX = 64;
    
    // Mathematic step
    private const int STEP_X = 120;
    private const int STEP_Y = 74;
    
    private const float DROP_HEIGHT = -500f; 
    private const float DROP_DURATION = 0.4f; 
    private const float STAGGER_DELAY = 0.05f; 

    public void RenderGridWithAnimation(GridModel gridData)
    {
        _chestVisuals.Clear();
        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }

        int elementIndex = 0; 

        for (int x = 0; x < gridData.Width; x++)
        {
            for (int y = 0; y < gridData.Height; y++)
            {
                Node2D tileVisual = TilePrefab.Instantiate<Node2D>();
                AddChild(tileVisual);
                
                Vector2 targetScreenPos = LogicalToIsometric(new Vector2I(x, y));
                Vector2 startScreenPos = targetScreenPos + new Vector2(0, DROP_HEIGHT);
                tileVisual.Position = startScreenPos;

                // We make the walls semi transparent for now
                float targetOpacity = gridData.Tiles[x, y].IsWalkable ? 1f : 0.2f;

                tileVisual.Modulate = new Color(1, 1, 1, 0);

                Tween tween = GetTree().CreateTween();
                float currentDelay = elementIndex * STAGGER_DELAY;
                
                tween.TweenProperty(tileVisual, "position", targetScreenPos, DROP_DURATION)
                     .SetDelay(currentDelay)
                     .SetTrans(Tween.TransitionType.Bounce) 
                     .SetEase(Tween.EaseType.Out);
                     
                tween.Parallel().TweenProperty(tileVisual, "modulate", new Color(1, 1, 1, targetOpacity), DROP_DURATION * 0.5f)
                     .SetDelay(currentDelay);
                if (gridData.Tiles[x, y].Effect == TileEffect.Chest)
                {
                    Node2D chestVisual = ChestPrefab.Instantiate<Node2D>();
                    AddChild(chestVisual);
                    
                    // Will fall from above, same as the tile, but with a slight delay to make it look like it's falling after the tile
                    chestVisual.Position = startScreenPos;
                    chestVisual.Modulate = new Color(1, 1, 1, 0);
                    
                    Tween chestTween = GetTree().CreateTween();
                    chestTween.TweenProperty(chestVisual, "position", targetScreenPos, DROP_DURATION)
                         .SetDelay(currentDelay).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
                    chestTween.Parallel().TweenProperty(chestVisual, "modulate", new Color(1, 1, 1, 1), DROP_DURATION * 0.5f)
                         .SetDelay(currentDelay);
                    _chestVisuals.Add(new Vector2I(x, y), chestVisual);
                }

                elementIndex++;
            }
        }
    }

    public Vector2 LogicalToIsometric(Vector2I logicalPos)
    {
        // We use STEP_X and STEP_Y to calculate the isometric position, ensuring that the grid is spaced correctly and maintains the isometric perspective.
        float screenX = (logicalPos.X - logicalPos.Y) * (STEP_X / 2f);
        float screenY = (logicalPos.X + logicalPos.Y) * (STEP_Y / 2f);
        return new Vector2(screenX, screenY);
    }
    
    public Vector2 LogicalToIsometric(Vector2 logicalPos)
    {
        float screenX = (logicalPos.X - logicalPos.Y) * (STEP_X / 2f);
        float screenY = (logicalPos.X + logicalPos.Y) * (STEP_Y / 2f);
        return new Vector2(screenX, screenY);
    }

    public void SpawnHighlight(Vector2I logicalPos, HighlightType type)
    {
        if (!SettingsManager.ShowHighlights && type == HighlightType.EnemyIntent) 
            return;
        // We choose the prefab
        PackedScene prefabToUse = (type == HighlightType.Movement) ? MoveHighlightPrefab : AttackHighlightPrefab;
        if (prefabToUse == null) return;

        Node2D highlight = prefabToUse.Instantiate<Node2D>();
        AddChild(highlight);
        highlight.Position = LogicalToIsometric(logicalPos);

        if (type == HighlightType.EnemyIntent)
        {
            highlight.Modulate = new Color(0.3f, 0.0f, 0.0f, 0.6f);
            _enemyHighlights.Add(highlight);
        }
        else
        {
            _playerHighlights.Add(highlight);
        }
    }
    public void ClearPlayerHighlights()
    {
        foreach (var hl in _playerHighlights) if (IsInstanceValid(hl)) hl.QueueFree();
        _playerHighlights.Clear();
    }

    public void ClearEnemyHighlights()
    {
        foreach (var hl in _enemyHighlights) if (IsInstanceValid(hl)) hl.QueueFree();
        _enemyHighlights.Clear();
    }
    public void SetChestLooted(Vector2I gridPos)
    {
        if (_chestVisuals.TryGetValue(gridPos, out Node2D chest))
        {
            if (IsInstanceValid(chest))
            {
                // We make it invisible with a fade-out animation
                Tween tween = CreateTween();
                tween.TweenProperty(chest, "modulate:a", 0.0f, 0.4f);
            }
        }
    }
}