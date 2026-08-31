using Godot;
using Deckrinth.Model;
using System.Collections.Generic;
using System;

namespace Deckrinth.View;

public partial class HandUI : CanvasLayer
{
    [Export] public PackedScene CardPrefab { get; set; }
    [Export] public Control CardContainer { get; set; }
    [Export] public Button EndTurnButton { get; set; }

    public event Action<Card> OnCardSelectedAction;
    public event Action OnEndTurnRequested;
    private CardUI _currentSelectedUI = null;
    public override void _Ready()
    {
        // Connecting the EndTurnButton's pressed signal to invoke the OnEndTurnRequested event when the button is pressed
        if (EndTurnButton != null)
        {
            EndTurnButton.Pressed += () => OnEndTurnRequested?.Invoke();
        }
    }
    // Method which updates the hand based on the current list of cards, ensuring that the UI reflects the current state of the player's hand
    public void UpdateHand(List<Card> hand)
    {
        // Identify the CardUI nodes that are no longer in the hand and need to be removed
        List<CardUI> toRemove = new List<CardUI>();
        foreach (Node child in CardContainer.GetChildren())
        {
            if (child is CardUI cardUI && !hand.Contains(cardUI.CardData))
            {
                toRemove.Add(cardUI);
            }
        }

        // We remove the CardUI nodes that are no longer in the hand, ensuring we also clear the selection if the removed card was selected
        foreach (CardUI ui in toRemove)
        {
            // If the card being removed is currently selected, we clear the selection reference
            if (_currentSelectedUI == ui) _currentSelectedUI = null; 
            ui.QueueFree();
        }

        // Go through the hand list in reverse order to maintain the correct visual order in the UI
        float staggerDelay = 0f;

        for (int i = hand.Count - 1; i >= 0; i--)
        {
            Card card = hand[i];
            CardUI existingUI = GetCardUIFor(card);

            if (existingUI == null)
            {
                CardUI newCardUI = CardPrefab.Instantiate<CardUI>();
                CardContainer.AddChild(newCardUI);
                newCardUI.Setup(card);

                newCardUI.EnableHoverAnimation = true;

                newCardUI.OnCardSelected += HandleCardClick;
                newCardUI.AnimateEntrance(staggerDelay);
                staggerDelay += 0.05f;
                
                existingUI = newCardUI;
            }

            // Math to determine the correct index in the CardContainer to maintain the order of cards as per the hand list
            int desiredTreeIndex = (hand.Count - 1) - i;
            CardContainer.MoveChild(existingUI, desiredTreeIndex);
        }
    }

    // Method to find the CardUI corresponding to a given Card data object
    private CardUI GetCardUIFor(Card card)
    {
        foreach (Node child in CardContainer.GetChildren())
        {
            if (child is CardUI cardUI && cardUI.CardData == card)
            {
                return cardUI;
            }
        }
        return null;
    }

    private void HandleCardClick(CardUI clickedUI)
    {
        // Turn off the selection effect for the previously selected card, if any
        if (_currentSelectedUI != null && IsInstanceValid(_currentSelectedUI))
        {
            _currentSelectedUI.SetSelectedState(false);
        }

        // Turn on the selection effect for the newly clicked card
        _currentSelectedUI = clickedUI;
        _currentSelectedUI.SetSelectedState(true);

        // Send the selected card data to the game logic
        OnCardSelectedAction?.Invoke(clickedUI.CardData);
    }

    public void ClearHand()
    {
        foreach (Node child in CardContainer.GetChildren())
        {
            child.QueueFree();
        }
        _currentSelectedUI = null;
    }
}