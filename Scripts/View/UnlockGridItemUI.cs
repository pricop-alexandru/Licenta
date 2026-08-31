using Godot;
using System;
using Deckrinth.Model.Resources;

namespace Deckrinth.View;

public partial class UnlockGridItemUI : Button
{
    [Export] public TextureRect IconRect { get; set; }
    [Export] public Label NameLabel { get; set; }
    [Export] public Control LockedMask { get; set; } // Semi-Transparent ColorRect
    [Export] public ProgressBar UnlockProgressBar { get; set; }
    [Export] public Label NewTagLabel { get; set; } // The NEW! corner text
    [Export] public ReferenceRect GoldenBorder { get; set; } // The border when it's clicked

    public UnlockableItemResource ResourceData { get; private set; }
    public bool IsUnlocked { get; private set; }

    public event Action<UnlockGridItemUI> OnItemClicked;

    public override void _Ready()
    {
        Pressed += () => OnItemClicked?.Invoke(this);
    }

    public void Setup(UnlockableItemResource resource, bool isUnlocked, bool isNew, int currentStatProgress)
    {
        ResourceData = resource;
        IsUnlocked = isUnlocked;

        GoldenBorder.Visible = false;
        NewTagLabel.Visible = isNew;

        if (isUnlocked)
        {
            IconRect.Texture = resource.Icon;
            IconRect.Modulate = Colors.White;
            NameLabel.Text = resource.DisplayName;
            LockedMask.Visible = false;
            UnlockProgressBar.Visible = false;
        }
        else
        {
            // Black Silhouette
            IconRect.Texture = resource.Icon;
            IconRect.Modulate = Colors.Black; 
            NameLabel.Text = "Locked";
            LockedMask.Visible = true;

            // Progress bar
            UnlockProgressBar.Visible = true;
            UnlockProgressBar.MaxValue = resource.RequiredStatValue;
            UnlockProgressBar.Value = Mathf.Min(currentStatProgress, resource.RequiredStatValue);
        }
    }

    public void SetSelected(bool selected)
    {
        GoldenBorder.Visible = selected;
        if (selected && NewTagLabel.Visible)
        {
            NewTagLabel.Visible = false; // We hide the NEW! when we click it
    	}
	}
}