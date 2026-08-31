using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Deckrinth.Model;
using Deckrinth.Model.Resources;
using Deckrinth.Services;

namespace Deckrinth.View;

// This one may be one of the scariest files I did
public partial class UnlocksMenuUI : Control
{
    // Top Tabs
    [Export] public Button CardsTabButton { get; set; }
    [Export] public Button EnemiesTabButton { get; set; }

    // Filters
    [Export] public OptionButton CategoryDropdown { get; set; }
    [Export] public OptionButton OrderDropdown { get; set; }
    [Export] public OptionButton StateDropdown { get; set; } // All / Unlocked / Locked
    
    // Grid
    [Export] public GridContainer ItemsGrid { get; set; }
    [Export] public PackedScene GridItemPrefab { get; set; }
    
    // Details Panel
    [Export] public Control PlaceholderPanel { get; set; }
    [Export] public Control DetailsPanel { get; set; }
    [Export] public TextureRect DetailImage { get; set; }
    [Export] public Label DetailName { get; set; }
    [Export] public Label DetailDescription { get; set; }
    [Export] public Label DetailUnlockCondition { get; set; }

    [Export] public Button BackButton { get; set; }

    public event Action OnReturnRequested;

    private PlayerProfile _profile;
    private List<UnlockableItemResource> _allUnlockables;
    private UnlockableType _currentTab = UnlockableType.Card;
    private UnlockGridItemUI _selectedItemUI = null;

    public void Setup()
    {
        Visible = false;
        BackButton.Pressed += () => { Visible = false; OnReturnRequested?.Invoke(); };

        CardsTabButton.Pressed += () => SwitchTab(UnlockableType.Card);
        EnemiesTabButton.Pressed += () => SwitchTab(UnlockableType.Enemy);

        CategoryDropdown.ItemSelected += (idx) => RefreshGrid();
        OrderDropdown.ItemSelected += (idx) => RefreshGrid();
        StateDropdown.ItemSelected += (idx) => RefreshGrid();
    }

    public void ShowMenu(PlayerProfile profile, List<UnlockableItemResource> allUnlockables)
    {
        Visible = true;
        _profile = profile;
        _allUnlockables = allUnlockables;
        
        SwitchTab(UnlockableType.Card); // Opens Cards by default
    }

    private void SwitchTab(UnlockableType type)
    {
        _currentTab = type;
        
        // Highlight logic for tabs
        CardsTabButton.Modulate = type == UnlockableType.Card ? Colors.White : new Color(0.6f, 0.6f, 0.6f);
        EnemiesTabButton.Modulate = type == UnlockableType.Enemy ? Colors.White : new Color(0.6f, 0.6f, 0.6f);

        // Update Filters visibility
        CategoryDropdown.Visible = (type == UnlockableType.Card); // Enemies don't have type filter now

        ClearSelection();
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        // Clears grid
        foreach (Node child in ItemsGrid.GetChildren()) child.QueueFree();

        // Initial tab filtering
        var pool = _allUnlockables.Where(u => u.ItemType == _currentTab);

        // Filtering by unlock state
        int stateIdx = StateDropdown.Selected;
        if (stateIdx == 1) // Unlocked
            pool = pool.Where(u => IsUnlocked(u));
        else if (stateIdx == 2) // Locked
            pool = pool.Where(u => !IsUnlocked(u));

        // Cards filtering by category
        if (_currentTab == UnlockableType.Card && CategoryDropdown.Selected > 0)
        {
            // Index 0 is 'All', next are the filters by card category
            CardCategory selectedCat = (CardCategory)(CategoryDropdown.Selected - 1);
            pool = pool.Where(u => GetCardCategory(u.Id) == selectedCat);
        }

        // Sorting (painful)
        bool isDescending = OrderDropdown.Selected == 1;

        if (_currentTab == UnlockableType.Card)
        {
            pool = isDescending 
                ? pool.OrderByDescending(u => GetCardCategory(u.Id)).ThenByDescending(u => GetCardRarity(u.Id))
                : pool.OrderBy(u => GetCardCategory(u.Id)).ThenBy(u => GetCardRarity(u.Id));
        }
        else
        {
            pool = isDescending 
                ? pool.OrderByDescending(u => GetEnemyTier(u.Id))
                : pool.OrderBy(u => GetEnemyTier(u.Id));
        }

        // Instancing
        foreach (var resource in pool)
        {
            UnlockGridItemUI itemUI = GridItemPrefab.Instantiate<UnlockGridItemUI>();
            ItemsGrid.AddChild(itemUI);

            bool isUnlocked = IsUnlocked(resource);
            bool isNew = _profile.UnseenUnlockIds.Contains(resource.Id);
            int currentProg = string.IsNullOrEmpty(resource.RequiredStatKey) ? 0 : _profile.LifetimeStats.GetValueOrDefault(resource.RequiredStatKey, 0);

            itemUI.Setup(resource, isUnlocked, isNew, currentProg);
            itemUI.OnItemClicked += HandleItemClicked;
        }
    }

    private void HandleItemClicked(UnlockGridItemUI clickedUI)
    {
        if (_selectedItemUI != null && IsInstanceValid(_selectedItemUI))
        {
            _selectedItemUI.SetSelected(false);
        }

        _selectedItemUI = clickedUI;
        _selectedItemUI.SetSelected(true);

        var res = clickedUI.ResourceData;

        // If it is new, we pull it from memory and save it
        if (_profile.UnseenUnlockIds.Contains(res.Id))
        {
            _profile.UnseenUnlockIds.Remove(res.Id);
            // For simplicity, this goes to be processed in Game root, easy step though
        }

        ShowDetails(res, clickedUI.IsUnlocked);
    }

    private void ShowDetails(UnlockableItemResource resource, bool isUnlocked)
    {
        PlaceholderPanel.Visible = false;
        DetailsPanel.Visible = true;

        if (isUnlocked)
        {
            DetailImage.Texture = resource.LargeArtwork ?? resource.Icon;
            DetailImage.Modulate = Colors.White;
            DetailName.Text = resource.DisplayName;
            DetailDescription.Text = resource.LoreText;
            DetailUnlockCondition.Text = "Unlocked";
            DetailUnlockCondition.AddThemeColorOverride("font_color", Colors.Green);
        }
        else
        {
            DetailImage.Texture = resource.LargeArtwork ?? resource.Icon;
            DetailImage.Modulate = Colors.Black; // Silhouette
            DetailName.Text = "???";
            DetailDescription.Text = "Keep playing to discover this entity's secrets.";
            DetailUnlockCondition.Text = $"To Unlock: {resource.UnlockConditionText}";
            DetailUnlockCondition.AddThemeColorOverride("font_color", Colors.Red);
        }
    }

    private void ClearSelection()
    {
        _selectedItemUI = null;
        PlaceholderPanel.Visible = true;
        DetailsPanel.Visible = false;
    }

    // Helper methods
    private bool IsUnlocked(UnlockableItemResource res)
    {
        if (res.ItemType == UnlockableType.Card) return _profile.UnlockedCardIds.Contains(res.Id);
        return _profile.UnlockedEnemyIds.Contains(res.Id);
    }

    private CardCategory GetCardCategory(string id) {
        var card = CardRegistry.CreateCardInstance(id);
        return card != null ? card.Category : CardCategory.Attack;
    }
    private CardRarity GetCardRarity(string id) {
        var card = CardRegistry.CreateCardInstance(id);
        return card != null ? card.Rarity : CardRarity.Common;
    }
    private EnemyTier GetEnemyTier(string id) {
        var enemy = EnemyRegistry.CreateEnemyInstance(id);
        return enemy != null ? enemy.Tier : EnemyTier.Minion;
    }
}