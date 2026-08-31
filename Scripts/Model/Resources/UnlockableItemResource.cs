using Godot;

namespace Deckrinth.Model.Resources;

public enum UnlockableType { Card, Enemy }

// Added GlobalClass attribute so it appears in Godot's "Create New Resource" menu
[GlobalClass] 
public partial class UnlockableItemResource : Resource
{
    [ExportCategory("Core Data")]
    [Export] public string Id { get; set; } = "";
    [Export] public UnlockableType ItemType { get; set; } = UnlockableType.Card;
    
    [ExportCategory("Display Info")]
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Texture2D Icon { get; set; }
    [Export] public Texture2D LargeArtwork { get; set; }
    
    [ExportCategory("Lore & Unlock")]
    [Export(PropertyHint.MultilineText)] public string LoreText { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string UnlockConditionText { get; set; } = "";
    // Unlock condition is based on a specific stat key and value (example "KillSlimes" with value 50 means "kill 50 slimes")
    [Export] public string RequiredStatKey { get; set; } = "";
    [Export] public int RequiredStatValue { get; set; } = 1;
}