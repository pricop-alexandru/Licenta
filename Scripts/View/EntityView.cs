using System;
using System.Collections.Generic;
using Deckrinth.Services;
using Godot;
using static Godot.Control;

namespace Deckrinth.View;

public partial class EntityView : Node2D
{
    private GridManager _grid;
    private Vector2I _currentLogicalPos;
    private const float MOVE_DURATION = 0.25f;
    private Label _nameplateLabel;

    // References to the sprite and textures for the entity, allowing for dynamic facing direction based on movement
    [Export] public Sprite2D MainSprite { get; set; }
    [Export] public Texture2D TextureFront { get; set; } // Looks towards Right-Down
    [Export] public Texture2D TextureBack { get; set; }  // Looks towards Left-Up

    public void Initialize(GridManager grid, Vector2I logicalPos, float dropDelay = 0f, string displayName = "")
    {
        _grid = grid;
        _currentLogicalPos = logicalPos; // We record the logical position of the entity for future movement calculations
        
        Vector2 targetPos = _grid.LogicalToIsometric(logicalPos);
        Position = targetPos + new Vector2(0, -500);
        Modulate = new Color(1, 1, 1, 0);

        if (!string.IsNullOrEmpty(displayName))
        {
            _nameplateLabel = new Label();
            _nameplateLabel.Text = displayName;
            _nameplateLabel.HorizontalAlignment = HorizontalAlignment.Center;
            
            // Text design: font size, color, outline
            _nameplateLabel.AddThemeFontSizeOverride("font_size", 20);
            _nameplateLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            _nameplateLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            _nameplateLabel.AddThemeConstantOverride("outline_size", 4);
            
            // Centered above the entity
            _nameplateLabel.SetAnchorsPreset(LayoutPreset.CenterTop);
            // Forcing the position mathematically since the preset changes the position based on the node's size (which is still 0)
            _nameplateLabel.Position = new Vector2(-50, -70); // X is half of the estimated width of 100px
            _nameplateLabel.CustomMinimumSize = new Vector2(100, 20);
            
            AddChild(_nameplateLabel);
        }
        float duration = 0.4f * SettingsManager.AnimDurationMultiplier;
        float parallelDur = 0.2f * SettingsManager.AnimDurationMultiplier;

        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "position", targetPos, duration).SetDelay(dropDelay).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(this, "modulate", new Color(1, 1, 1, 1), parallelDur).SetDelay(dropDelay);
        
        UpdateFacingDirection(new Vector2I(1, 0)); // Spawn facing right by default
    }

    public void MoveTo(Vector2I newLogicalPos)
    {
        // We find out the direction of the movement
        Vector2I direction = newLogicalPos - _currentLogicalPos;
        
        if (direction != Vector2I.Zero)
        {
            UpdateFacingDirection(direction);
        }

        // Movement
        Vector2 targetScreenPos = _grid.LogicalToIsometric(newLogicalPos);
        float moveDur = MOVE_DURATION * SettingsManager.AnimDurationMultiplier;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "position", targetScreenPos, moveDur)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.InOut);

        _currentLogicalPos = newLogicalPos;
    }

    // Flip the sprite based on the movement direction to ensure the entity is facing the correct way
    private void UpdateFacingDirection(Vector2I dir)
    {
        if (MainSprite == null || TextureFront == null || TextureBack == null) return;

        // In our isometric setup, we consider the following:
        // X positive = right, X negative = left
        // Y positive = down, Y negative = up

        // If we are moving right or down, we face the front texture
        if (dir.X > 0 || dir.Y > 0)
        {
            MainSprite.Texture = TextureFront;
            // If we are moving downwards, we flip the front texture to face the correct direction
            MainSprite.FlipH = (dir.X <= 0 && dir.Y > 0); 
        }
        // If we are moving left or up, we face the back texture
        else if (dir.X < 0 || dir.Y < 0)
        {
            MainSprite.Texture = TextureBack;
            // If we are moving directly to the left, we flip the back texture to face the correct direction
            MainSprite.FlipH = (dir.X < 0 && dir.Y == 0);
        }
    }
    public void WalkPath(List<Vector2I> path, Action onComplete)
    {
        if (path == null || path.Count <= 1) 
        { 
            onComplete?.Invoke(); 
            return; 
        }
        
        // Begin the recursive movement sequence starting from the second position in the path (the first is the current position)
        SequenceMoves(path, 1, onComplete);
    }
    private void SequenceMoves(List<Vector2I> path, int index, Action onComplete)
    {
        if (index >= path.Count) 
        { 
            onComplete?.Invoke(); 
            return; 
        }

        Vector2I nextPos = path[index];
        Vector2I direction = nextPos - _currentLogicalPos;
        
        if (direction != Vector2I.Zero) UpdateFacingDirection(direction);

        Vector2 targetScreenPos = _grid.LogicalToIsometric(nextPos);
        _currentLogicalPos = nextPos;
        float walkDur = 0.33f * SettingsManager.AnimDurationMultiplier;
        Tween tween = GetTree().CreateTween();
        // Speed of ~ 3 tiles per second, so ~0.33 seconds per tile
        tween.TweenProperty(this, "position", targetScreenPos, walkDur).SetTrans(Tween.TransitionType.Linear);
        
        // Recursively call the next movement after this tween completes
        tween.TweenCallback(Callable.From(() => SequenceMoves(path, index + 1, onComplete)));
    }
}