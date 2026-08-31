using Godot;

namespace Deckrinth.Model.Enemies;

public class SlimeEnemy : Entity
{
    public SlimeEnemy()
    {
        Id = "enemy_slime";
        DisplayName = "Slime";
        Faction = EntityFaction.Enemy;
        Tier = EnemyTier.Minion;
        
        SpawnWeight = 100; // Easy to spawn, common enemy
        MinDepth = 1;
        SkullsDrop = 5;
        // Default movement pattern so no change
    }
}