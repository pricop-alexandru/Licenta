using Godot;
using System;
using System.Collections.Generic;
using Deckrinth.Model;

namespace Deckrinth.View;

public partial class GameOverUI : Control
{
    [Export] public Label DepthLabel { get; set; }
    [Export] public Label KillsLabel { get; set; }
    [Export] public Label CurrencyLabel { get; set; }
    
    [Export] public Control UnlocksPanel { get; set; }
    [Export] public HBoxContainer UnlocksScrollContainer { get; set; } // Card clones from the registry
    [Export] public PackedScene CardPrefab { get; set; } // Same prefab used in the shop / hand
    
    [Export] public Button ReturnButton { get; set; }

    public event Action OnReturnToMenuRequested;

    public void Setup()
    {
        Visible = false;
        ReturnButton.Pressed += () => OnReturnToMenuRequested?.Invoke();
    }

    public void ShowGameOver(RunState finalRun, List<UnlockableDisplayData> newUnlocks, PlayerProfile profile)
    {
        Visible = true;

        // Stats
        
        // Depth
        int highestDepth = profile.LifetimeStats.ContainsKey("max_depth") ? profile.LifetimeStats["max_depth"] : 0;
        string depthSuffix = finalRun.CurrentDepth >= highestDepth ? " (New Highscore!)" : "";
        DepthLabel.Text = $"Depth Reached: {finalRun.CurrentDepth}{depthSuffix}";

        // Kills
        int runKills = finalRun.CurrentRunStats.ContainsKey("total_kills") ? finalRun.CurrentRunStats["total_kills"] : 0;
        KillsLabel.Text = $"Enemies Killed: {runKills}";

        // Currency
        CurrencyLabel.Text = $"Skulls Gained: {finalRun.TotalSkullsEarnedThisRun}";

        // Unlocks
        
        // We empty the old unlocks list before anything else
        foreach (Node child in UnlocksScrollContainer.GetChildren())
        {
            child.QueueFree();
        }

        if (newUnlocks.Count == 0)
        {
            UnlocksPanel.Visible = false; // If we didnt unlock anything, theres no use for an unlock panel
        }
        else
        {
            UnlocksPanel.Visible = true;

            // Make a clone for aspect on every unlock
            foreach (var unlock in newUnlocks)
            {
                // We just call the registry for a false image of the card
                var cardLogic = Services.CardRegistry.CreateCardInstance(unlock.Id);
                
                if (cardLogic != null && CardPrefab != null)
                {
                    CardUI cardVisual = CardPrefab.Instantiate<CardUI>();
                    UnlocksScrollContainer.AddChild(cardVisual);
                    
                    cardVisual.Setup(cardLogic);
                    
                    // We turn off the gameplay hover since its display only
                    cardVisual.EnableHoverAnimation = false; 
                    
                    // Pop effect for those new unlocked cards
                    cardVisual.Scale = Vector2.Zero;
                    Tween popTween = CreateTween();
                    popTween.TweenProperty(cardVisual, "scale", Vector2.One, 0.4f)
                            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                }
            }
        }
    }
}