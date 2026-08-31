using Godot;
using System;
using Deckrinth.Model;

namespace Deckrinth.View;

public partial class ShopUI : Control
{
    [Export] public Control DropdownPanel { get; set; }
    [Export] public GridContainer CardsGrid { get; set; }
    
    [Export] public Button RerollButton { get; set; }
    [Export] public Label RerollCostLabel { get; set; }
    [Export] public Button LeaveButton { get; set; }
    
    [Export] public PackedScene CardPrefab { get; set; }

    public event Action<int> OnBuyRequested;
    public event Action OnRerollRequested;
    public event Action OnLeaveRequested;

    public void Setup()
    {
        Visible = false;
        RerollButton.Pressed += () => OnRerollRequested?.Invoke();
        LeaveButton.Pressed += () => OnLeaveRequested?.Invoke();
    }

    public void ShowShop(Card[] offers, int rerollCost)
    {
        Visible = true;
        RerollCostLabel.Text = $"{rerollCost} Skulls";
        RefreshGrid(offers);

        DropdownPanel.Position = new Vector2(DropdownPanel.Position.X, -GetViewportRect().Size.Y);
        Tween tween = CreateTween();
        tween.TweenProperty(DropdownPanel, "position:y", 0f, 0.6f)
             .SetTrans(Tween.TransitionType.Bounce)
             .SetEase(Tween.EaseType.Out);
    }

    public void HideShop()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(DropdownPanel, "position:y", -GetViewportRect().Size.Y, 0.4f)
             .SetTrans(Tween.TransitionType.Back)
             .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() => Visible = false));
    }

    public void RefreshGrid(Card[] offers)
    {
        // Curatam grid-ul vechi
        foreach (Node child in CardsGrid.GetChildren()) child.QueueFree();

        // Cream fix 6 slot-uri vizuale
        for (int i = 0; i < offers.Length; i++)
        {
            Card card = offers[i];
            
            // Container vertical (Carte Sus, Pret Jos)
            VBoxContainer slotContainer = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            CardsGrid.AddChild(slotContainer);

            if (card != null)
            {
                // Instantiaza cartea
                CardUI cardUI = CardPrefab.Instantiate<CardUI>();
                slotContainer.AddChild(cardUI);
                cardUI.Setup(card);
                
                // Dezactivam shield-ul de Z-Index ca sa mearga hover-ul curat in grid
                cardUI.MouseFilter = MouseFilterEnum.Pass; 

                // Butonul ascuns de Buy (apasand pe carte)
                int slotIndex = i; // Salvare locala a indexului pentru event
                cardUI.OnCardSelected += (c) => OnBuyRequested?.Invoke(slotIndex);

                Label costLabel = new Label 
                { 
                    Text = $"{card.CostInShop} Skulls", 
                    HorizontalAlignment = HorizontalAlignment.Center 
                };
                slotContainer.AddChild(costLabel);
            }
            else
            {
                Control emptySpace = new Control { CustomMinimumSize = new Vector2(200, 300) };
                slotContainer.AddChild(emptySpace);
            }
        }
    }
    public void UpdateRerollCost(int newCost)
    {
        if (RerollCostLabel != null)
        {
            RerollCostLabel.Text = $"{newCost} Skulls";
        }
    }
}