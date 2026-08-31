using Godot;
using System.Collections.Generic;
using System.Linq;
using Deckrinth.Model;

namespace Deckrinth.View;

public partial class DeckOverlayUI : Control
{
    [Export] public TextureButton DeckButton { get; set; }
    [Export] public Label DeckCountLabel { get; set; }
    
    [Export] public Control OverlayPanel { get; set; }
    // Grid container to hold the cards in the overlay, allowing for a flexible layout
    [Export] public VBoxContainer ContentList { get; set; } 
    [Export] public Button CloseButton { get; set; }
    
    [Export] public PackedScene CardPrefab { get; set; }

    [Signal]
    public delegate void OnOverlayOpenedRequestedEventHandler();

    public void Setup()
    {
        OverlayPanel.Visible = false;
        DeckButton.Pressed += () => {
            EmitSignal(SignalName.OnOverlayOpenedRequested);
            OverlayPanel.Visible = true;
        };
        CloseButton.Pressed += () => OverlayPanel.Visible = false;
    }

    public void UpdateDeckCount(int count)
    {
        DeckCountLabel.Text = count.ToString();
    }

    public void PopulateOverlay(RunState runData, Services.DeckManager deckLogic)
    {
        // We clear the ContentList to remove any previous cards or sections before populating it with the current run data
        foreach (Node child in ContentList.GetChildren())
        {
            child.QueueFree();
        }

        // Passive section
        if (runData.PassiveBuild.Count > 0)
        {
            AddSectionTitle("Passives Collected");
            GridContainer passiveGrid = CreateGrid();
            
            // We group the passives by their ID to count how many copies of each passive the player has collected
            var groupedPassives = runData.PassiveBuild.GroupBy(c => c.Id);
            foreach (var group in groupedPassives)
            {
                Card cardTemplate = group.First();
                int copies = group.Count();
                
                CardUI cardUI = InstantiateCard(cardTemplate, passiveGrid);
                AddBadgeToCard(cardUI, $"x{copies}", Colors.Gold);
            }
        }

        // Active section
        if (runData.ActiveDeck.Count > 0)
        {
            // A small spacer between the passive and active sections for better visual separation
            if (runData.PassiveBuild.Count > 0) 
            {
                ContentList.AddChild(new Control { CustomMinimumSize = new Vector2(0, 40) }); 
            }

            AddSectionTitle("Active Deck (To Draw / Total)");
            GridContainer activeGrid = CreateGrid();

            var groupedActives = runData.ActiveDeck.GroupBy(c => c.Id);
            foreach (var group in groupedActives)
            {
                Card cardTemplate = group.First();
                int totalOwned = group.Count();
                int remainingInDrawPile = deckLogic.DrawPile.Count(c => c.Id == cardTemplate.Id);
                
                CardUI cardUI = InstantiateCard(cardTemplate, activeGrid);
                AddBadgeToCard(cardUI, $"{remainingInDrawPile} / {totalOwned}", Colors.Cyan);
            }
        }
    }

    // Helper methods to create UI elements for the overlay

    private void AddSectionTitle(string text)
    {
        Label title = new Label 
        { 
            Text = text, 
            HorizontalAlignment = HorizontalAlignment.Center 
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        ContentList.AddChild(title);
    }

    private GridContainer CreateGrid()
    {
        GridContainer grid = new GridContainer { Columns = 5 }; // Adjustable column count for better layout
        ContentList.AddChild(grid);
        return grid;
    }

    private CardUI InstantiateCard(Card cardData, GridContainer parentGrid)
    {
        CardUI cardUI = CardPrefab.Instantiate<CardUI>();
        parentGrid.AddChild(cardUI);
        cardUI.Setup(cardData);
        cardUI.MouseFilter = MouseFilterEnum.Ignore;
        return cardUI;
    }

    private void AddBadgeToCard(CardUI cardUI, string text, Color color)
    {
        Label badge = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Modulate = color
        };
        
        // Big text with outline for better visibility
        badge.AddThemeFontSizeOverride("font_size", 30);
        badge.AddThemeColorOverride("font_outline_color", Colors.Black);
        badge.AddThemeConstantOverride("outline_size", 8);
        
        // Fixes the badge to the top-right corner of the card
        badge.SetAnchorsPreset(LayoutPreset.TopRight);
        
        // A small offset to make it look better
        badge.Position = new Vector2(-15, 10);
        
        cardUI.AddChild(badge);
    }
}