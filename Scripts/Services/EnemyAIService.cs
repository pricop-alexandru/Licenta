using System.Collections.Generic;
using System.Linq;
using Godot;
using Deckrinth.Model;

namespace Deckrinth.Services;

public class EnemyAIService
{
    private GridModel _grid;
    private PathfindingService _pathfinder;
    private List<Entity> _enemies;
    private Entity _player;

    public EnemyAIService(GridModel grid, PathfindingService pathfinder, List<Entity> enemies, Entity player)
    {
        _grid = grid;
        _pathfinder = pathfinder;
        _enemies = enemies;
        _player = player;
    }

    public Dictionary<Entity, EnemyIntent> ComputeEnemyIntents()
    {
        var intents = new Dictionary<Entity, EnemyIntent>();
        var claimedPositions = new HashSet<Vector2I>(); // Soft collision map

        // Generate the flow field (Dijkstra map) for the player's position
        int[,] flowField = _pathfinder.GenerateFlowField(_player.Position);

        // Sort enemies by their distance to the player using the flow field values
        var sortedEnemies = _enemies.Where(e => !e.IsDead)
                                    .OrderBy(e => flowField[e.Position.X, e.Position.Y]).ToList();

        // We block the positions of rooted enemies first to prevent other enemies from moving into their space
        foreach (var enemy in sortedEnemies)
        {
            if (enemy.IsRooted) claimedPositions.Add(enemy.Position);
        }

        foreach (var enemy in sortedEnemies)
        {
            EnemyIntent intent = new EnemyIntent { Type = IntentType.Move, TargetPosition = enemy.Position };

            if (enemy.IsRooted)
            {
                if (enemy.Tier == EnemyTier.Boss)
                {
                    intent.Type = IntentType.Spell;
                    intent.TargetPosition = _player.Position;
                }
                intents[enemy] = intent;
                continue;
            }

            Vector2I bestMove = enemy.Position;
            int bestScore = flowField[enemy.Position.X, enemy.Position.Y]; // Current score at the enemy's position
            bool foundMove = false;

            // Test all possible moves based on the enemy's movement pattern
            foreach (Vector2I dir in enemy.MovementPattern)
            {
                Vector2I nextPos = enemy.Position + dir;

                // Jump mechanic - for enemies that can cross gaps or obstacles, the step is calculated by the best available destination, not the journey
                if (_grid.IsInBounds(nextPos) && _grid.IsCellWalkable(nextPos))
                {
                    // If the position is not already claimed by another enemy or if it's the player's position, consider it for movement
                    if (!claimedPositions.Contains(nextPos) || nextPos == _player.Position)
                    {
                        int score = flowField[nextPos.X, nextPos.Y];
                        
                        // We look for the move that brings the enemy closest to the player (lowest score in the flow field)
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestMove = nextPos;
                            foundMove = true;
                        }
                    }
                }
            }

            // Intent attributing
            if (foundMove && bestMove != enemy.Position)
            {
                intent.TargetPosition = bestMove;
                
                if (bestMove == _player.Position)
                {
                    intent.Type = IntentType.AttackPlayer;
                }
                else
                {
                    claimedPositions.Add(bestMove); // Reserve the position for this enemy
                    intent.FullPath = new List<Vector2I> { enemy.Position, bestMove }; // UI arrow
                }
            }
            else
            {
                // If blocked or no better move found, stay in place
                claimedPositions.Add(enemy.Position);
            }

            intents[enemy] = intent;
        }

        return intents;
    }
}