using Godot;
using Deckrinth.Model;
using System;

namespace Deckrinth.View;

public partial class CardUI : Control
{
    // Sloturile noastre noi
    [Export] public TextureRect CardImage { get; set; }
    [Export] public Label NameLabel { get; set; } // Pentru titlul de pe carte
    
    // Sloturile pentru Tooltip
    [Export] public Control TooltipPanel { get; set; }
    [Export] public Label DescriptionLabel { get; set; }
    [Export] public Control VisualRoot { get; set; }
    public Card CardData { get; private set; }
    public event Action<CardUI> OnCardSelected;
    public bool EnableHoverAnimation { get; set; } = false;

    public void Setup(Card card)
    {
        CardData = card;
        NameLabel.Text = card.CardName;
        
        // Default, we show the card's image if it exists; otherwise, we can use a placeholder
        DescriptionLabel.Text = $"Type: {card.Category}\n\nCooldown: {card.CooldownTurns}\n\nEffect:\n{card.CardDescription}";
    }

    public void AnimateEntrance(float delay = 0f)
    {
        if (VisualRoot == null) return;

        // We make the card start off-screen to the right and fade in
        VisualRoot.Modulate = new Color(1, 1, 1, 0);
        VisualRoot.Position = new Vector2(60f, 0f);

        // Creating a tween for smooth animation
        Tween tween = CreateTween().SetParallel(true);
        
        // Slide in from the right
        tween.TweenProperty(VisualRoot, "position:x", 0f, 0.25f)
             .SetDelay(delay)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.Out);
             
        // Fade in the card
        tween.TweenProperty(VisualRoot, "modulate:a", 1f, 0.2f)
             .SetDelay(delay)
             .SetTrans(Tween.TransitionType.Linear);
    }

    public override void _Ready()
    {
        // Setting the pivot offset to the center of the card for better scaling and rotation effects
        PivotOffset = Size / 3; 
    }

    public void _on_gui_input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            OnCardSelected?.Invoke(this);
        }
    }

    // Tooltip logic: When the mouse enters the card, we show the tooltip; when it exits, we hide it.
    public void _on_mouse_entered()
    {
        if (TooltipPanel != null) 
        {
            TooltipPanel.Visible = true;

            float panelWidth = TooltipPanel.Size.X > 0 ? TooltipPanel.Size.X : TooltipPanel.CustomMinimumSize.X;
            float panelHeight = TooltipPanel.Size.Y > 0 ? TooltipPanel.Size.Y : TooltipPanel.CustomMinimumSize.Y;
            
            Vector2 viewportSize = GetViewportRect().Size;
            Vector2 newPos = new Vector2();

            // X-axis clamping
            if (GlobalPosition.X > viewportSize.X / 2.0f)
                newPos.X = -panelWidth - 10;
            else
                newPos.X = Size.X + 10;

            // Smart clamping for the Y position to ensure the tooltip doesn't go off-screen
            // We initialize it to the top of the card, but we will adjust it if it goes off-screen
            newPos.Y = 0; 
            
            // We calculate the absolute position of the tooltip panel in the viewport
            float absoluteBottom = GlobalPosition.Y + newPos.Y + panelHeight;
            float absoluteTop = GlobalPosition.Y + newPos.Y;

            if (absoluteBottom > viewportSize.Y)
            {
                newPos.Y -= (absoluteBottom - viewportSize.Y) + 10;
            }
            // We push it down if it goes above the top of the screen
            else if (absoluteTop < 0)
            {
                newPos.Y += Math.Abs(absoluteTop) + 10; 
            }
            if (absoluteTop < 0)
            {
                newPos.Y += Math.Abs(absoluteTop) + 10; 
            }

            TooltipPanel.Position = newPos;
        }

        ZIndex = 100; // Bringing it to the front
        
        // We remove the previous tween to avoid clipping out of screen
        SetSelectedState(true); 
        if (EnableHoverAnimation)
        {
            Tween tween = CreateTween().SetParallel(true);
            tween.TweenProperty(this, "position:y", -50f, 0.15f).SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(this, "scale", new Vector2(1.2f, 1.2f), 0.15f).SetTrans(Tween.TransitionType.Sine);
        }
    }

    public void _on_mouse_exited()
    {
        if (TooltipPanel != null) TooltipPanel.Visible = false;
        ZIndex = 0;
        
        SetSelectedState(false); // Remove glow
        if (EnableHoverAnimation)
        {
            Tween tween = CreateTween().SetParallel(true);
            tween.TweenProperty(this, "position:y", 0f, 0.15f).SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(this, "scale", Vector2.One, 0.15f).SetTrans(Tween.TransitionType.Sine);
        }
    }
    
    public void SetSelectedState(bool isSelected)
    {
        // A small glow effect to indicate selection. We can adjust the color modulation to make it brighter when selected.
        Modulate = isSelected ? new Color(1.5f, 1.5f, 1.2f) : new Color(1f, 1f, 1f);
    }
}