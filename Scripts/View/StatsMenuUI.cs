using Godot;
using System;
using System.Text;
using Deckrinth.Model;

namespace Deckrinth.View;

public partial class StatsMenuUI : Control
{
    [Export] public Label ProfileNameLabel { get; set; }
    
    // Containers to clone text
    [Export] public VBoxContainer EconomyStatsContainer { get; set; }
    [Export] public VBoxContainer CombatStatsContainer { get; set; }
    
    [Export] public Button BackButton { get; set; }

    public event Action OnReturnRequested;

    public void Setup()
    {
        Visible = false;
        BackButton.Pressed += () => OnReturnRequested?.Invoke();
    }

    public void ShowStats(PlayerProfile profile)
    {
        Visible = true;
        ProfileNameLabel.Text = $"{profile.ProfileName}'s Lifetime Statistics";

        ClearContainer(EconomyStatsContainer);
        ClearContainer(CombatStatsContainer);

        // Economy Stats - hardcoded from profile
        AddStatLine(EconomyStatsContainer, "Total Skulls Earned", profile.TotalLifetimeSkulls.ToString());
        AddStatLine(EconomyStatsContainer, "Total Skulls Spent", profile.LifetimeSkullsSpent.ToString());

        // Combat and depth stats - from dynamic dictionary
        foreach (var kvp in profile.LifetimeStats)
        {
            string formattedKey = FormatStatKey(kvp.Key);
            AddStatLine(CombatStatsContainer, formattedKey, kvp.Value.ToString());
        }
    }

    private void ClearContainer(Container container)
    {
        if (container == null) return;
        foreach (Node child in container.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void AddStatLine(Container container, string labelText, string valueText)
    {
        if (container == null) return;

        HBoxContainer row = new HBoxContainer();
        
        Label nameLabel = new Label();
        nameLabel.Text = labelText;
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill; // Fills the space
		nameLabel.AddThemeFontSizeOverride("font_size", 32);
        
        Label valueLabel = new Label();
        valueLabel.Text = valueText;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        // Yellow text for numbers
        valueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f)); 
		valueLabel.AddThemeFontSizeOverride("font_size", 32);

        row.AddChild(nameLabel);
        row.AddChild(valueLabel);
        
        container.AddChild(row);
    }

    // Converts underlined strings into normal text
    private string FormatStatKey(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey)) return "";

        string[] words = rawKey.Split('_');
        StringBuilder sb = new StringBuilder();

        foreach (string word in words)
        {
            if (word.Length > 0)
            {
                sb.Append(char.ToUpper(word[0]));
                sb.Append(word.Substring(1).ToLower());
                sb.Append(" ");
            }
        }

        return sb.ToString().Trim();
    }
}