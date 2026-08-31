using Deckrinth.Core;

namespace Deckrinth.Model.Cards;

public class GreedPassiveCard : Card
{
    public GreedPassiveCard()
    {
        Id = "card_avarice";
        CardName = "Avarice";
		CardDescription = "Your greed grants you a 20% bonus to all skull rewards. Stacks multiplicatively with other bonuses.";
        Category = CardCategory.Passive;
        Rarity = CardRarity.Rare;
        DropWeight = 25;
        CostInShop = 50;
        MaxCopiesInDeck = 5;
    }

    public override void OnEquipPassive(GameLoopManager gameLoop)
    {
        gameLoop.RunData.SkullBonusMultiplier += 0.2f;
    }

    public override bool Play(Entity player, GridModel grid, Godot.Vector2I targetPosition)
    {
        return false;
    }

    public override void GetValidTargets(Entity player, GridModel grid, System.Collections.Generic.List<Entity> enemies, out System.Collections.Generic.List<Godot.Vector2I> validMoves, out System.Collections.Generic.List<Godot.Vector2I> validAttacks)
    {
        validMoves = new System.Collections.Generic.List<Godot.Vector2I>();
        validAttacks = new System.Collections.Generic.List<Godot.Vector2I>();
    }
}