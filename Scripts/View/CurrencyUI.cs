using Godot;

namespace Deckrinth.View;

public partial class CurrencyUI : Control
{
    [Export] public Label SkullsLabel { get; set; }

    public void UpdateSkullsDisplay(int newAmount)
    {
        SkullsLabel.Text = newAmount.ToString();
        
        // Un mic efect de marire si revenire (pentru feedback)
        PivotOffset = new Vector2(Size.X, 0);

        Tween popTween = CreateTween();
        Scale = new Vector2(1.3f, 1.3f);
        popTween.TweenProperty(this, "scale", Vector2.One, 0.25f)
                .SetTrans(Tween.TransitionType.Bounce)
                .SetEase(Tween.EaseType.Out);
    }
}