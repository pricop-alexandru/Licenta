using Godot;
using System;
using System.Collections.Generic;

namespace Deckrinth.Model.Cards;

public class CoreMovementCard : Card
{
    public CoreMovementCard()
    {
        Id = "card_core_movement";
        CardName = "Horizontal Attack";
        CardDescription = "Move or attack in one of the four cardinal directions.";
        Category = CardCategory.Transformation;
        Rarity = CardRarity.Common;
        EndsTurn = true;
        MaxCopiesInDeck = 1;
        IsImmuneToDisable = true;
    }

    // We divide clearly between valid moves and valid attacks, allowing the player to see where they can move and where they can attack separately.
    public override void GetValidTargets(Entity source, GridModel grid, List<Entity> enemies, out List<Vector2I> validMoves, out List<Vector2I> validAttacks)
    {
        validMoves = new List<Vector2I>();
        validAttacks = new List<Vector2I>();
        
        Vector2I[] directions = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        foreach (var dir in directions)
        {
            Vector2I targetPos = source.Position + dir;
            
            if (targetPos.X >= 0 && targetPos.X < grid.Width && targetPos.Y >= 0 && targetPos.Y < grid.Height)
            {
                if (grid.Tiles[targetPos.X, targetPos.Y].IsWalkable)
                {
                    validMoves.Add(targetPos); // Celula libera = Verde
                }
                else
                {
                    // If not walkable, check if there's an enemy at that position to allow for an attack
                    bool isEnemyHere = enemies.Exists(e => e.Position == targetPos && !e.IsDead);
                    if (isEnemyHere)
                    {
                        validAttacks.Add(targetPos); // Inamic = Rosu
                    }
                }
            }
        }
    }

    public override bool Play(Entity source, GridModel grid, Vector2I targetPosition)
    {
        int distance = Math.Abs(source.Position.X - targetPosition.X) + Math.Abs(source.Position.Y - targetPosition.Y);
        return distance == 1; // We allow the card to be played if the target position is adjacent (Manhattan distance of 1)
    }
}