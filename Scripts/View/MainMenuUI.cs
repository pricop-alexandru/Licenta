using Godot;
using System;
using Deckrinth.Model;
using Deckrinth.Services;

namespace Deckrinth.View;

public partial class MainMenuUI : Control
{
    [Export] public Control TitleScreenBox { get; set; }
    [Export] public Button PlayButton { get; set; }
    [Export] public Button QuitButton { get; set; }

    [Export] public Control SaveSlotsBox { get; set; }
	[Export] public LineEdit NameInputField { get; set; }
    [Export] public Button[] SlotButtons { get; set; } // 3 button array
    [Export] public Button[] DeleteButtons { get; set; } // 3 button array
    [Export] public Button BackToTitleButton { get; set; }
    [Export] public Button SettingsButton { get; set; }

    // Signal to notify when a slot is selected, passing the PlayerProfile and the slot index
    public event Action<PlayerProfile, int> OnSlotSelected;
    public event Action OnSettingsRequested;

    public void Setup()
    {
        ShowTitleScreen();

        PlayButton.Pressed += ShowSaveSlots;
        QuitButton.Pressed += () => GetTree().Quit();
        BackToTitleButton.Pressed += ShowTitleScreen;
        if (SettingsButton != null)
        {
            SettingsButton.Pressed += () => OnSettingsRequested?.Invoke();
        }
        // We connect the slots
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i; // Local save for the lambda to capture correctly
            SlotButtons[i].Pressed += () => HandleSlotClicked(slotIndex);
            DeleteButtons[i].Pressed += () => HandleDeleteClicked(slotIndex);
        }
    }

    private void ShowTitleScreen()
    {
        TitleScreenBox.Visible = true;
        SaveSlotsBox.Visible = false;
    }

    private void ShowSaveSlots()
    {
        TitleScreenBox.Visible = false;
        SaveSlotsBox.Visible = true;
        RefreshSlotsUI();
    }

    private void RefreshSlotsUI()
    {
        int totalCards = CardRegistry.GetAllCardsMasterList().Count;
        int totalEnemies = EnemyRegistry.GetAllEnemiesMasterList().Count;

        for (int i = 0; i < 3; i++)
        {
            PlayerProfile profile = SaveManager.LoadProfile(i);

            if (profile == null)
            {
                SlotButtons[i].Text = $"Slot {i + 1}: Empty (Click to Create)";
                DeleteButtons[i].Visible = false; // Nu poti sterge un slot gol
            }
            else
            {
                int maxDepth = profile.LifetimeStats.ContainsKey("max_depth") ? profile.LifetimeStats["max_depth"] : 0;
                int unlockedCards = profile.UnlockedCardIds.Count;
                int unlockedEnemies = profile.UnlockedEnemyIds.Count;

                SlotButtons[i].Text = $"{profile.ProfileName}\nMax Depth: {maxDepth} | Cards: {unlockedCards}/{totalCards} | Enemies: {unlockedEnemies}/{totalEnemies}";
                DeleteButtons[i].Visible = true;
            }
        }
    }

    private void HandleSlotClicked(int slotIndex)
    {
        PlayerProfile profile = SaveManager.LoadProfile(slotIndex);

        if (profile == null)
        {
			// Creating a new profile for the empty slot (with chosen name or default name)
			string chosenName = string.IsNullOrWhiteSpace(NameInputField?.Text) ? $"Save {slotIndex + 1}" : NameInputField.Text;
            profile = new PlayerProfile(chosenName);
            // Reset the unlocked lists to ensure a clean start
			profile.UnlockedCardIds.Clear();
            profile.UnlockedEnemyIds.Clear();
            // By default, we unlock the base cards and enemies for a new profile
            profile.UnlockedCardIds.Add("card_core_movement");
			profile.UnlockedCardIds.Add("card_diagonal_strike");
            profile.UnlockedCardIds.Add("card_avarice");
            profile.UnlockedEnemyIds.Add("enemy_slime");
            profile.UnlockedEnemyIds.Add("enemy_goblin");

            SaveManager.SaveProfile(profile, slotIndex);
			// Clear the input field after creating a new profile
			if (NameInputField != null) NameInputField.Text = "";
        }

        // Hide the menu and notify GameRoot to start the pre-run menu with the selected profile
        Visible = false; 
        OnSlotSelected?.Invoke(profile, slotIndex);
    }

    private void HandleDeleteClicked(int slotIndex)
    {
        // Delete the profile file and any associated active run data
        string profilePath = ProjectSettings.GlobalizePath($"user://Saves/profile_slot_{slotIndex}.json");
        if (System.IO.File.Exists(profilePath)) System.IO.File.Delete(profilePath);
        
        SaveManager.DeleteActiveRun(slotIndex);
        
        RefreshSlotsUI(); // Update the UI to reflect the deletion
    }
}