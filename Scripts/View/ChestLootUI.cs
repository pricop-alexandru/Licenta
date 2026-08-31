using Godot;
using System;
using Deckrinth.Model;

namespace Deckrinth.View;

public partial class ChestLootUI : CanvasLayer
{
    [Export] public Label CardNameLabel { get; set; }
    [Export] public Button TakeButton { get; set; }
    [Export] public Button LeaveButton { get; set; }

    // Event trimis inapoi catre GameRoot cand jucatorul a luat o decizie
    public event Action<bool> OnLootResolved;

    public override void _Ready()
    {
        // Conectam butoanele din Godot la codul nostru C#
        TakeButton.Pressed += () => Resolve(true);
        LeaveButton.Pressed += () => Resolve(false);
        
        // Ascundem meniul la inceputul jocului
        Visible = false;
    }

    public void ShowLoot(Card card)
    {
        // Actualizam textul si afisam meniul
        if (card == null)
        {
            CardNameLabel.Text = "The chest was completely empty!";
        }
        else
        {
            CardNameLabel.Text = $"You found a card:\n{card.CardName}";
        }
        Visible = true;
    }

    private void Resolve(bool takeCard)
    {
        Visible = false;
        OnLootResolved?.Invoke(takeCard);
    }
}