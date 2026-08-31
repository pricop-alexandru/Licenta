using Godot;
using Deckrinth.Model.Resources;

namespace Deckrinth.Model;

// A read-only snapshot sent to the Godot View Layer
public struct UnlockableDisplayData
{
    public string Id { get; private set; }
    public UnlockableType ItemType { get; private set; }
    public bool IsUnlocked { get; private set; }
    
    public string DisplayName { get; private set; }
    public string LoreText { get; private set; }
    public string UnlockConditionText { get; private set; }
    
    public Texture2D Icon { get; private set; }
    public Texture2D LargeArtwork { get; private set; }

    public UnlockableDisplayData(UnlockableItemResource resource, bool isUnlocked)
    {
        Id = resource.Id;
        ItemType = resource.ItemType;
        IsUnlocked = isUnlocked;
        
        // Expose unlock condition regardless of state
        UnlockConditionText = resource.UnlockConditionText;
        Icon = resource.Icon;
        LargeArtwork = resource.LargeArtwork;

        if (isUnlocked)
        {
            DisplayName = resource.DisplayName;
            LoreText = resource.LoreText;
        }
        else
        {
            // Mask the data for locked items
            DisplayName = "???";
            LoreText = "Keep playing to discover this entity's secrets.";
        }
    }
}