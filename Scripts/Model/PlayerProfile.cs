using System.Collections.Generic;

namespace Deckrinth.Model;

// This represents the meta-progression (Save Slot Data)
public class PlayerProfile
{
    public string ProfileName { get; set; }
    public int TotalLifetimeSkulls { get; set; }
    public int LifetimeSkullsSpent { get; set; }
    
    // The master list of what the player has unlocked.
    // By default, it contains the starting deck IDs.
    public List<string> UnlockedCardIds { get; set; }
    public List<string> UnlockedEnemyIds { get; set; }
    public List<string> UnseenUnlockIds { get; set; }
    public Dictionary<string, int> LifetimeStats { get; set; }
    // Constructor without parameters (used for deserialization)
    public PlayerProfile()
    {
        TotalLifetimeSkulls = 0;
        UnlockedCardIds = new List<string> { "card_core_movement" };
        UnlockedEnemyIds = new List<string>();
        UnseenUnlockIds = new List<string>();
        LifetimeStats = new Dictionary<string, int>();
    }

    // Constructor with name (used when creating a new profile)
    // ": this()" calls the default constructor to initialize the lists and dictionary
    public PlayerProfile(string name) : this() 
    {
        ProfileName = name;
    }
}