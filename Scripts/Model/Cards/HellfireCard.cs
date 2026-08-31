using System.Collections.Generic;
using Deckrinth.Model;
using Godot;

namespace Deckrinth.Model.Cards;

public class HellfireCard : Card
{
    public HellfireCard()
    {
        Id = "card_hellfire";
        CardName = "Hellfire";
        CardDescription = "Call down hellfire on a 3x3 area. Exhausted until shop.";
        Rarity = CardRarity.Epic;
        Category = CardCategory.Spell;
        
        DropWeight = 10;
        MaxCopiesInDeck = 2;
        CostInShop = 100;
        ReplenishesAtShop = true; 
        IsConsumable = true; // One-time use per combat/shop cycle
        CooldownTurns = 0;
        EndsTurn = false;
        RequiresGridTarget = true;
    }

    // A spell can target any possible tile
    public override void GetValidTargets(Entity source, GridModel grid, List<Entity> enemies, out List<Vector2I> validMoves, out List<Vector2I> validAttacks)
    {
        validMoves = new List<Vector2I>();
        validAttacks = new List<Vector2I>();

        // Anywhere on the map
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                validAttacks.Add(new Vector2I(x, y));
            }
        }
    }

    // We overwrite the AoE to return a 3x3 square
    public override List<Vector2I> GetAoETiles(Vector2I targetCenter, GridModel grid)
    {
        return grid.GetTilesInRadius(targetCenter, 1);
    }

    // Executing the spell
    public override bool Play(Entity source, GridModel grid, Vector2I targetPosition)
    {
        List<Vector2I> impactZone = GetAoETiles(targetPosition, grid);

        // We damage every tile in the area
        foreach (Vector2I tile in impactZone)
        {
            // We could just leave this empty as its a validator and to this in gameroot
        }

        return true; 
    }
}