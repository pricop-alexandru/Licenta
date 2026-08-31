using Godot;
using System.Collections.Generic;

namespace Deckrinth.Model;

public enum CardRarity { Common, Rare, Epic, Legendary }
public enum CardCategory { Movement, Attack, Spell, Skill, Passive, Transformation }

public abstract class Card
{
    // Identity
    public string Id { get; protected set; }
    public string CardName { get; protected set; }
    public string CardDescription { get; protected set; }
    public CardRarity Rarity { get; protected set; }
    public CardCategory Category { get; protected set; }
    public List<string> Keywords { get; protected set; } = new List<string>();
    public string IconPath { get; protected set; } = "res://Resources/Textures/Cards/default.png";
    
    // Meta progression
    public int DropWeight { get; protected set; } 
    public int MaxCopiesInDeck { get; protected set; } = 3;
    public int CostInShop { get; protected set; } = 50;
    public bool ReplenishesAtShop { get; protected set; } = false;
    public bool IsUpgraded { get; protected set; } = false;
    // This is for more advanced mechanics
    public string UpgradedVersionId { get; protected set; } = null;
    
    // Base mechanics
    public bool EndsTurn { get; protected set; } = true;
    public int CooldownTurns { get; protected set; } = 0; 
    public bool IsConsumable { get; protected set; } = false;
    public bool IsImmuneToDisable { get; protected set; } = false;
    
    public bool RequiresGridTarget { get; protected set; } = true;

    // Virtual Hooks
    public virtual bool CanPlay(Entity source, GridModel grid)
    {
        return true; 
    }

    public virtual void GetValidTargets(Entity source, GridModel grid, List<Entity> enemies, out List<Vector2I> validMoves, out List<Vector2I> validAttacks)
    {
        validMoves = new List<Vector2I>();
        validAttacks = new List<Vector2I>();
    }
    public virtual List<Vector2I> GetAoETiles(Vector2I targetCenter, GridModel grid)
    {
        return new List<Vector2I> { targetCenter };
    }

    public abstract bool Play(Entity source, GridModel grid, Vector2I targetPosition);

    public virtual void OnDiscard(Entity source, GridModel grid) { }

    public virtual void OnDrawn(Entity source, GridModel grid) { }
    public virtual void OnEquipPassive(Core.GameLoopManager gameLoop) 
    { 
    }
    public virtual string GetDynamicDescription(Core.GameLoopManager gameLoop = null)
    {
        return CardDescription;
    }
}