using Godot;

namespace Deckrinth.Model.Enemies;

public class GoblinEnemy : Entity
{
    public GoblinEnemy()
    {
        Id = "enemy_goblin";
        DisplayName = "Goblin";
        Faction = EntityFaction.Enemy;
        Tier = EnemyTier.Minion;
        
        SpawnWeight = 80;  // A little rarer than the slime, but still common
        MinDepth = 2;      // Spawns starting with depth 2
        SkullsDrop = 15;
        // Diagonal movement
        MovementPattern = new Vector2I[] { 
            new Vector2I(1, 1),   // Bottom-Right (Isometric)
            new Vector2I(1, -1),  // Top-Right (Isometric)
            new Vector2I(-1, 1),  // Bottom-Left (Isometric)
            new Vector2I(-1, -1)  // Top-Left (Isometric)
        };
    }
}