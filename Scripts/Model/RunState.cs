using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Deckrinth.Model;

public class RunState
{
    public int CurrentDepth { get; set; }
    public int Skulls { get; private set; }
    // Current run stats (reset at the start of each run)
    public int TotalSkullsEarnedThisRun { get; set; }
    public int SkullsSpentThisRun { get; set; }
    public Dictionary<string, int> CurrentRunStats { get; set; }
    
    // Limits
    private int _baseEnemySoftCap = 10; // Starting soft cap for number of enemies on a floor
    private int _enemyCapBonus = 0; // Modified by specific passive cards
    private const int HARD_CAP = 25; // Engine/Design absolute maximum
    
    // Store IDs for JSON serialization instead of abstract objects
    public List<string> PassiveBuildIds { get; set; }
    public List<string> ActiveDeckIds { get; set; }
    // Tracks one-time use cards that should no longer drop in this run
    public List<string> ExhaustedCardIds { get; private set; }
    public float SkullBonusMultiplier { get; set; } = 1.0f;

    // The player's decks (Ignored by JSON to prevent deep serialization errors)
    [JsonIgnore] public List<Card> PassiveBuild { get; private set; }
    [JsonIgnore] public List<Card> ActiveDeck { get; private set; }
    
    // Combat-specific piles (reset at the start of each floor)
    [JsonIgnore] public List<Card> DrawPile { get; private set; }
    [JsonIgnore] public List<Card> DiscardPile { get; private set; }
    [JsonIgnore] public List<Card> CurrentHand { get; private set; }
    
    // Tracks cards that are on cooldown (Card reference, turns remaining)
    [JsonIgnore] public Dictionary<Card, int> CooldownTracker { get; private set; }

    public RunState()
    {
        CurrentDepth = 1;
        Skulls = 0;
        TotalSkullsEarnedThisRun = 0;
        SkullsSpentThisRun = 0;
        CurrentRunStats = new Dictionary<string, int>();
        
        // Initialize ID lists for the save system
        PassiveBuildIds = new List<string>();
        ActiveDeckIds = new List<string>();
        ExhaustedCardIds = new List<string>();

        // Initialize runtime instances
        PassiveBuild = new List<Card>();
        ActiveDeck = new List<Card>();
        DrawPile = new List<Card>();
        DiscardPile = new List<Card>();
        CurrentHand = new List<Card>();
        CooldownTracker = new Dictionary<Card, int>();
    }

    // Called after SaveManager loads the JSON file to rebuild the actual objects
    public void RehydrateDecks()
    {
        PassiveBuild.Clear();
        foreach (string id in PassiveBuildIds)
        {
            Card passiveCard = Services.CardRegistry.CreateCardInstance(id);
            if (passiveCard != null) PassiveBuild.Add(passiveCard);
        }

        ActiveDeck.Clear();
        foreach (string id in ActiveDeckIds)
        {
            Card activeCard = Services.CardRegistry.CreateCardInstance(id);
            if (activeCard != null) ActiveDeck.Add(activeCard);
        }
    }

    public bool TrySpendSkulls(int amount)
    {
        if (Skulls >= amount)
        {
            Skulls -= amount;
            SkullsSpentThisRun += amount;
            return true;
        }
        return false;
    }

    // Economy Formula: BaseValue * DepthMultiplier
    public int AddSkullsFromKill(int baseValue)
    {
        float depthMultiplier = 1.0f + (CurrentDepth * 0.15f);
        int finalYield = (int)Math.Round(baseValue * depthMultiplier);
        Skulls += finalYield;
        TotalSkullsEarnedThisRun += finalYield;
        return finalYield;
    }

    public void AddSkulls(int rawAmount)
    {
        Skulls += rawAmount;
        TotalSkullsEarnedThisRun += rawAmount;
    }

    public void IncreaseEnemyCap(int amount)
    {
        _enemyCapBonus += amount;
    }

    // Calculates the maximum number of enemies allowed on the current floor
    public int GetCurrentSoftCap()
    {
        int scaledCap = _baseEnemySoftCap + (CurrentDepth / 2) + _enemyCapBonus;
        return Math.Min(scaledCap, HARD_CAP);
    }
    public int GetCardCount(string cardId)
    {
        int count = 0;
        foreach (Card c in ActiveDeck) if (c.Id == cardId) count++;
        foreach (Card c in PassiveBuild) if (c.Id == cardId) count++;
        return count;
    }
}