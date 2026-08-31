using Godot;
using System;
using Deckrinth.Model;
using Deckrinth.Services;

namespace Deckrinth.View;

public partial class PreRunMenuUI : Control
{
    [Export] public Button NewRunButton { get; set; }
    [Export] public Button ContinueRunButton { get; set; }
    [Export] public Button UnlocksButton { get; set; }
    [Export] public Button StatsButton { get; set; }
    [Export] public Button ReturnToMainButton { get; set; }
    [Export] public ReferenceRect UnlocksNotificationBorder { get; set; }

    public event Action OnNewRunRequested;
    public event Action OnContinueRunRequested;
    public event Action OnUnlocksRequested;
    public event Action OnStatsRequested;
    public event Action OnReturnRequested;

    public void Setup()
    {
        NewRunButton.Pressed += () => OnNewRunRequested?.Invoke();
        ContinueRunButton.Pressed += () => OnContinueRunRequested?.Invoke();
        UnlocksButton.Pressed += () => OnUnlocksRequested?.Invoke();
        StatsButton.Pressed += () => OnStatsRequested?.Invoke();
        ReturnToMainButton.Pressed += () => OnReturnRequested?.Invoke();
    }

    public void ShowMenu(PlayerProfile profile, int slotIndex)
    {
        Visible = true;
        
        // We check if there is an active run
        bool hasRun = SaveManager.HasActiveRun(slotIndex);
        ContinueRunButton.Disabled = !hasRun;
        
        // Semi-transparent effect for the ContinueRunButton if there's no active run, providing visual feedback to the player
        ContinueRunButton.Modulate = hasRun ? Colors.White : new Color(1, 1, 1, 0.4f);
        if (UnlocksNotificationBorder != null)
        {
            UnlocksNotificationBorder.Visible = profile.UnseenUnlockIds.Count > 0;
        }
    }

    public void HideMenu()
    {
        Visible = false;
    }
}