using Godot;

namespace Deckrinth.Model;

public enum EntityFaction { Player, Enemy }
public enum EnemyTier { None, Minion, Elite, Boss }

public class Entity
{
    public string Id { get; set; }
    public string DisplayName { get; set; } = "Unknown";
    public EntityFaction Faction { get; set; }
    public EnemyTier Tier { get; set; }
    
    public Vector2I Position { get; set; }
    public int SkullsDrop { get; protected set; } = 0;
    
    // Boss mechanics
    public int BossHP { get; set; } 
    public bool IsKnockbackResistant => Tier == EnemyTier.Boss;
    // Spawning logic metadata
    public int SpawnWeight { get; protected set; } = 100; 
    public int MinDepth { get; protected set; } = 1;
    // Movement pattern for enemies; can be overridden for specific behaviors
    public Vector2I[] MovementPattern { get; protected set; } = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
    // Boolean states replace counters
    public bool HasShield { get; set; }
    public bool HasSecondWind { get; set; }
    public bool IsRooted { get; set; } 
    public bool IsDead { get; private set; }
    
    public void ForceKill()
    {
        IsDead = true;
    }
    
    public void TakeHit(out bool bouncedBack)
    {
        bouncedBack = false;

        if (HasShield)
        {
            HasShield = false;
            bouncedBack = true;
            return;
        }

        if (Tier == EnemyTier.Boss)
        {
            BossHP--;
            if (BossHP > 0)
            {
                bouncedBack = true; 
                return;
            }
        }

        if (HasSecondWind)
        {
            HasSecondWind = false;
            return;
        }

        IsDead = true;
    }
}