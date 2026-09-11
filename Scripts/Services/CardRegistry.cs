using System;
using System.Collections.Generic;
using System.Linq;
using Deckrinth.Model;
using Deckrinth.Model.Cards;

namespace Deckrinth.Services;

public static class CardRegistry
{
    // Dictionary mapping a Card ID to its constructor function
    private static readonly Dictionary<string, Func<Card>> _cardFactory = new Dictionary<string, Func<Card>>();

    // Holds the master list of all cards (useful for generating the Unlocks menu)
    private static readonly List<Card> _cardDatabase = new List<Card>();

    // Called exactly once when the application starts
    public static void Initialize()
    {
        // Declaring the cards in the game. Each card has a unique ID and a constructor function.
        RegisterCard("card_core_movement", () => new CoreMovementCard());
        RegisterCard("card_diagonal_strike", () => new DiagonalStrikeCard());
        RegisterCard("card_avarice", () => new GreedPassiveCard());
        RegisterCard("card_hellfire", () => new HellfireCard());
    }

    public static void RegisterCard(string id, Func<Card> constructor)
    {
        if (!_cardFactory.ContainsKey(id))
        {
            _cardFactory.Add(id, constructor);
            
            // Generate one instance strictly for data reading (Unlocks menu, Shop logic)
            _cardDatabase.Add(constructor()); 
        }
    }

    // Creates a brand new instance of a card by its ID (Used by Save System & Deck Drawing)
    public static Card CreateCardInstance(string id)
    {
        if (_cardFactory.TryGetValue(id, out Func<Card> constructor))
        {
            return constructor();
        }
        
        // In a real scenario, we might want to log an error here or return a fallback 'Glitch' card, but that is not necessary now
        return null; 
    }

    // Used by the Shop and Drop mechanics to filter what can spawn
    public static List<Card> GetDropPool(CardCategory category, List<string> unlockedCardIds)
    {
        return _cardDatabase
            .Where(c => c.Category == category && unlockedCardIds.Contains(c.Id))
            .ToList();
    }

    // Used by the UI Unlocks Menu
    public static List<Card> GetAllCardsMasterList()
    {
        return _cardDatabase;
    }
}