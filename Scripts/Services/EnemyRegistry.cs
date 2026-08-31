using System;
using System.Collections.Generic;
using System.Linq;
using Deckrinth.Model;
using Deckrinth.Model.Enemies;
namespace Deckrinth.Services;

public static class EnemyRegistry
{
    private static readonly Dictionary<string, Func<Entity>> _enemyFactory = new Dictionary<string, Func<Entity>>();
    private static readonly List<Entity> _enemyDatabase = new List<Entity>();

    public static void Initialize()
    {
        // Adding all enemy types here
        RegisterEnemy("enemy_slime", () => new SlimeEnemy());
        RegisterEnemy("enemy_goblin", () => new GoblinEnemy());
    }

    private static void RegisterEnemy(string id, Func<Entity> constructor)
    {
        if (!_enemyFactory.ContainsKey(id))
        {
            _enemyFactory.Add(id, constructor);
            _enemyDatabase.Add(constructor()); 
        }
    }

    public static Entity CreateEnemyInstance(string id)
    {
        if (_enemyFactory.TryGetValue(id, out Func<Entity> constructor))
        {
            return constructor();
        }
        return null; 
    }

    // Returns only the enemies that meet the depth requirement and are unlocked
    public static List<Entity> GetSpawnPool(int currentDepth, List<string> unlockedIds)
    {
        return _enemyDatabase
            .Where(e => e.Faction == EntityFaction.Enemy)
            .Where(e => e.MinDepth <= currentDepth)
            .Where(e => unlockedIds.Contains(e.Id))
            .ToList();
    }
    // Used by the Main Menu to calculate unlock progress
    public static List<Entity> GetAllEnemiesMasterList()
    {
        return _enemyDatabase;
    }
}