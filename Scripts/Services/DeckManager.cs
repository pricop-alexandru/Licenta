using System;
using System.Collections.Generic;
using System.Linq;
using Deckrinth.Model;
using Godot;

namespace Deckrinth.Services;

public class DeckManager
{
    public List<Card> DrawPile { get; private set; } = new List<Card>();
    public List<Card> Hand { get; private set; } = new List<Card>();
    public List<Card> DiscardPile { get; private set; } = new List<Card>();
    public List<Card> ExhaustedPile { get; private set; } = new List<Card>();
    
    // Remembers the cooldown state of cards
    private Dictionary<Card, int> _cooldowns = new Dictionary<Card, int>();
    private Random _rng = new Random();

    public void InitializeDeck(List<Card> startingDeck, int initialDraw = 3)
    {
        DrawPile = new List<Card>(startingDeck);
        Hand.Clear();
        DiscardPile.Clear();
        _cooldowns.Clear();

        Card movementCard = DrawPile.FirstOrDefault(c => c.Category == CardCategory.Transformation);
        if (movementCard != null)
        {
            // We remove it and put in the hand
            DrawPile.Remove(movementCard);
            Hand.Add(movementCard);
            
            // We reduce the initial draw count by 1 since we already gave the player a card
            initialDraw = Math.Max(0, initialDraw - 1); 
        }
        ShuffleDrawPile();
        DrawCards(initialDraw);
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (DrawPile.Count == 0)
            {
                if (DiscardPile.Count == 0) break; // No available cards to draw, exit early
                
                // Reshuffle the discard pile back into the draw pile if it's empty
                DrawPile.AddRange(DiscardPile);
                DiscardPile.Clear();
                ShuffleDrawPile();
            }

            // Draw the top card from the draw pile and add it to the hand
            Card drawnCard = DrawPile[0];
            DrawPile.RemoveAt(0);
            Hand.Add(drawnCard);
        }
    }

    // Call this method when a card is played to handle its transition to the appropriate pile based on its properties
    public void OnCardPlayed(Card card)
    {
        Hand.Remove(card);
        if (card.Category == CardCategory.Transformation)
        {
            Hand.Add(card);
            return;
        }
        if (card.IsConsumable) return; 

        if (card.CooldownTurns > 0)
        {
            // Cooldown cards go into a temporary cooldown state, tracked in the _cooldowns dictionary
            _cooldowns[card] = card.CooldownTurns;
        }
        else if (card.ReplenishesAtShop)
        {
           ExhaustedPile.Add(card);
        }
        else 
        {
            DiscardPile.Add(card);
        }
    }

    // Calling this method at the start of each player turn
    public void ProcessTurnStart()
    {
        DrawCards(1);
    }
    public void ProcessTurnEnd()
    {
        List<Card> cardsReady = new List<Card>();
        foreach (var kvp in _cooldowns.ToList()) 
        {
            _cooldowns[kvp.Key]--;
            if (_cooldowns[kvp.Key] <= 0)
            {
                cardsReady.Add(kvp.Key);
            }
        }

        foreach (Card card in cardsReady)
        {
            _cooldowns.Remove(card);
            if (card.Category == CardCategory.Transformation)
            {
                Hand.Add(card); // We put it back in the hand if it's a transformation card
            }
            else
            {
            int randomInsertIndex = _rng.Next(0, DrawPile.Count + 1);
            DrawPile.Insert(randomInsertIndex, card);
            }
        }
    }
    public void ReplenishFromShop()
    {
        DrawPile.AddRange(ExhaustedPile);
        ExhaustedPile.Clear();
        ShuffleDrawPile();
    }
    private void ShuffleDrawPile()
    {
        int n = DrawPile.Count;
        while (n > 1)
        {
            n--;
            int k = _rng.Next(n + 1);
            Card value = DrawPile[k];
            DrawPile[k] = DrawPile[n];
            DrawPile[n] = value;
        }
    }

    public bool ReduceAllCooldowns(int amount = 1)
    {
        if (_cooldowns.Count == 0) return false;

        bool reducedAny = false;
        var keys = _cooldowns.Keys.ToList(); // Clone of the list of keys to avoid modifying the collection while iterating

        foreach (var key in keys)
        {
            _cooldowns[key] -= amount;
            reducedAny = true;
            
            if (_cooldowns[key] <= 0)
            {
                _cooldowns.Remove(key);
               int randomInsertIndex = _rng.Next(0, DrawPile.Count + 1);
                DrawPile.Insert(randomInsertIndex, key); // We throw the card back into the Draw pile when its cooldown reaches zero
            }
        }
        return reducedAny;
    }
}