using System;
using System.Collections.Generic;
using System.Linq;
using Deckrinth.Model;

namespace Deckrinth.Services;

public class ShopManager
{
    public int RerollCost { get; private set; } = 10;
    public int ActiveSlots { get; private set; } = 3; 
    public int MaxSlots { get; private set; } = 6;
    
    public Card[] CurrentOffers { get; private set; }

    private Random _rng = new Random();
    
    // We save data about the unlocked cards and the current run state
    private IEnumerable<string> _cachedUnlockedIds;
    private RunState _cachedRunData;

    // This method opens the shop phase 
    public void InitializeShopPhase(IEnumerable<string> unlockedCardIds, RunState runData)
    {
        _cachedUnlockedIds = unlockedCardIds;
        _cachedRunData = runData;
        RerollCost = 10; // Reset the reroll cost at the start of each shop phase
        
        GenerateOffers();
    }

    private void GenerateOffers()
    {
        CurrentOffers = new Card[MaxSlots];
        
        var validPool = CardRegistry.GetAllCardsMasterList()
            .Where(c => _cachedUnlockedIds.Contains(c.Id))
            .Where(c => _cachedRunData.GetCardCount(c.Id) < c.MaxCopiesInDeck)
            .ToList();

        for (int i = 0; i < ActiveSlots; i++)
        {
            if (validPool.Count > 0)
            {
                int randomIndex = _rng.Next(validPool.Count);
                CurrentOffers[i] = CardRegistry.CreateCardInstance(validPool[randomIndex].Id);
                
                // We remove the selected card from the pool to avoid duplicates in the same shop phase
                validPool.RemoveAt(randomIndex); 
            }
            else
            {
                CurrentOffers[i] = null; 
            }
        }
    }

    public bool TryBuyCard(int slotIndex, Func<int, bool> spendCurrencyCallback, Action<Card> grantCardCallback)
    {
        Card cardToBuy = CurrentOffers[slotIndex];
        if (cardToBuy == null) return false;

        if (spendCurrencyCallback(cardToBuy.CostInShop))
        {
            grantCardCallback(cardToBuy);
            CurrentOffers[slotIndex] = null; 
            return true;
        }
        return false;
    }

    public bool TryReroll(Func<int, bool> spendCurrencyCallback)
    {
        // Reroll cost increases with each reroll, so we check if the player can afford it
        if (spendCurrencyCallback(RerollCost))
        {
            RerollCost += 5;
            GenerateOffers();
            return true;
        }
        return false;
    }
}