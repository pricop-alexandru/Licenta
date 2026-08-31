using Godot;
using System;

namespace Deckrinth.View;

public partial class PauseMenuUI : CanvasLayer
{
    [Export] public Button OpenPauseButton { get; set; } // Corner pause button during gameplay
    [Export] public Control PauseOverlay { get; set; } // Gray overlay that appears when the game is paused
    [Export] public Button ResumeButton { get; set; }
    [Export] public Button SettingsButton { get; set; }
    [Export] public Button UnlocksButton { get; set; }
    [Export] public Button SaveAndQuitButton { get; set; }
    [Export] public Button AbandonRunButton { get; set; }

    [Export] public Control SaveQuitPopup { get; set; } // Small confirmation popup for quitting the game
    [Export] public Button ToMainMenuButton { get; set; }
    [Export] public Button ToDesktopButton { get; set; }
    [Export] public Button CancelQuitButton { get; set; }
    [Export] public ReferenceRect UnlocksNotificationBorder { get; set; }
    private Model.PlayerProfile _currentProfile;

    public event Action OnSettingsRequested;
    public event Action OnUnlocksRequested;
    public event Action OnAbandonRequested;
    public event Action OnSaveAndQuitToMain;
    public event Action OnSaveAndQuitToDesktop;

    public bool IsGameActive { get; set; } = false;

    public void Setup()
    {
        PauseOverlay.Visible = false;
        SaveQuitPopup.Visible = false;

        OpenPauseButton.Pressed += TogglePause;
        ResumeButton.Pressed += TogglePause;
        
        SettingsButton.Pressed += () => OnSettingsRequested?.Invoke();
        UnlocksButton.Pressed += () => OnUnlocksRequested?.Invoke();
        AbandonRunButton.Pressed += () => OnAbandonRequested?.Invoke();

        SaveAndQuitButton.Pressed += () => SaveQuitPopup.Visible = true;
        CancelQuitButton.Pressed += () => SaveQuitPopup.Visible = false;
        
        ToMainMenuButton.Pressed += () => OnSaveAndQuitToMain?.Invoke();
        ToDesktopButton.Pressed += () => OnSaveAndQuitToDesktop?.Invoke();
    }

    public override void _Input(InputEvent @event)
    {
        // If the game is active and the player presses ESCAPE, we toggle the pause menu. If the SaveQuitPopup is open, we close it first.
        if (IsGameActive && @event.IsActionPressed("ui_cancel"))
        {
            if (SaveQuitPopup.Visible) 
                SaveQuitPopup.Visible = false;
            else 
                TogglePause();
        }
    }

    private void TogglePause()
    {
        PauseOverlay.Visible = !PauseOverlay.Visible;
        SaveQuitPopup.Visible = false; // We reset the popup visibility when toggling the pause menu to avoid confusion

        // We pause the game when the pause overlay is visible and unpause it when it's hidden, ensuring that the game state reflects the UI state
        GetTree().Paused = PauseOverlay.Visible;
        if (PauseOverlay.Visible && _currentProfile != null && UnlocksNotificationBorder != null)
        {
            UnlocksNotificationBorder.Visible = _currentProfile.UnseenUnlockIds.Count > 0;
        }
    }
    public void ForceClose()
    {
        PauseOverlay.Visible = false;
        GetTree().Paused = false;
    }
    public void SetProfile(Model.PlayerProfile profile)
    {
        _currentProfile = profile;
    }
}