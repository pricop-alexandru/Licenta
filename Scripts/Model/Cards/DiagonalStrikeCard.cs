using Godot;
using System;
using System.Collections.Generic;

namespace Deckrinth.Model.Cards;

public class DiagonalStrikeCard : Card
{
    public DiagonalStrikeCard()
    {
        Id = "card_diagonal_strike";
        CardName = "Diagonal Strike";
		CardDescription = "Attack in one of the four diagonal directions without moving.";
        Category = CardCategory.Attack;
		Rarity = CardRarity.Common;
        EndsTurn = false; 
        CostInShop = 25;
		CooldownTurns = 2;
        MaxCopiesInDeck = 3;
		RequiresGridTarget = true;
    }

    public override void GetValidTargets(Entity source, GridModel grid, List<Entity> enemies, out List<Vector2I> validMoves, out List<Vector2I> validAttacks)
    {
        validMoves = new List<Vector2I>(); // Aceasta carte NU ofera miscare
        validAttacks = new List<Vector2I>();

        Vector2I[] diagonalDirections = { 
            new Vector2I(1, 1), new Vector2I(1, -1), 
            new Vector2I(-1, 1), new Vector2I(-1, -1) 
        };

        foreach (var dir in diagonalDirections)
        {
            Vector2I targetPos = source.Position + dir;
            if (grid.IsInBounds(targetPos))
            {
                validAttacks.Add(targetPos);
            }
        }
    }

    public override bool Play(Entity source, GridModel grid, Vector2I targetPosition)
    {
        // Verificam daca distanta pe X si Y este fix 1 (adica e pe diagonala)
        int dx = Math.Abs(source.Position.X - targetPosition.X);
        int dy = Math.Abs(source.Position.Y - targetPosition.Y);
        
        return dx == 1 && dy == 1;
    }
}