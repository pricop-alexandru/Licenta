using Godot;
using System;
using System.Collections.Generic;
using Deckrinth.Model;
using System.Linq;

namespace Deckrinth.Services;

public class LevelGeneratorService
{
    // Limite absolute impuse de design
    private const int MAX_MAP_SIZE = 30;
    private const int MAX_ENEMIES_CAP = 15;

    // Called by GameLoopManager to orchestrate the entire floor setup
    public void SetupNextLevel(int currentDepth, List<string> unlockedEnemyIds, out GridModel newGrid, out List<Entity> newEnemies, out Vector2I playerStart, out Vector2I exitPos)
    {
        // 1. Dynamic scaling based on your Staggered Progression formula
        int size = Math.Min(4 + (currentDepth / 3), MAX_MAP_SIZE);
        int width = size;
        int height = size;
        
        Random rng = new Random();

        // 2. Generarea dinamica a punctelor de Start si Exit
        // Player Start: Coloana 0 (Stanga). Randul Y: aleatoriu, ignorand colturile
        int startY = rng.Next(1, height - 1);
        playerStart = new Vector2I(0, startY);

        // Exit: Ultima coloana (Dreapta, adica Width - 1). Randul Y: aleatoriu
        int exitY = rng.Next(1, height - 1);
        exitPos = new Vector2I(width - 1, exitY);

        // 3. Generate the layout and validate it
        newGrid = GenerateLevel(width, height, playerStart, exitPos);
        SpawnChests(newGrid, currentDepth, playerStart, exitPos);

        // 4. Enemy scaling formula: creste inaintea maririi hartii
        int numEnemies = Math.Min(1 + ((currentDepth + 1) / 3), MAX_ENEMIES_CAP);
        newEnemies = SpawnEnemies(newGrid, playerStart, exitPos, numEnemies, currentDepth, unlockedEnemyIds);
    }

    private GridModel GenerateLevel(int width, int height, Vector2I startPos, Vector2I exitPos)
    {
        GridModel grid = new GridModel(width, height);

        // 1. Initializam tot terenul ca fiind o platforma complet deschisa (walkable)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid.Tiles[x, y] = new Model.TileData { IsWalkable = true, Effect = TileEffect.None };
            }
        }

        // 2. Plasare Aleatoare de Ziduri (Obstacole)
        System.Random rng = new System.Random();
        int totalTiles = width * height;
        
        // 20% din harta vor fi ziduri (Poti ajusta procentul in functie de cat de aglomerat vrei sa fie)
        int maxWalls = (int)(totalTiles * 0.40f); 
        int wallsPlaced = 0;
        int attempts = 0;

        while (wallsPlaced < maxWalls && attempts < 1000)
        {
            attempts++;
            int rx = rng.Next(width);
            int ry = rng.Next(height);
            Vector2I pos = new Vector2I(rx, ry);

            // Nu punem zid pe start sau pe usa de iesire
            if (pos != startPos && pos != exitPos && grid.Tiles[rx, ry].IsWalkable)
            {
                // Plantam zidul temporar
                grid.Tiles[rx, ry].IsWalkable = false;

                // Verificam daca prin punerea acestui zid am blocat drumul catre iesire
                if (IsPathValid(grid, startPos, exitPos))
                {
                    wallsPlaced++; // Aprobam zidul
                }
                else
                {
                    // Daca drumul e blocat, stergem zidul si incercam altundeva
                    grid.Tiles[rx, ry].IsWalkable = true;
                }
            }
        }
        SealIsolatedAreas(grid, startPos);
        return grid;
    }
    private void SealIsolatedAreas(GridModel grid, Vector2I start)
    {
        bool[,] reachable = new bool[grid.Width, grid.Height];
        System.Collections.Generic.Queue<Vector2I> queue = new System.Collections.Generic.Queue<Vector2I>();

        // Incepem de la player
        queue.Enqueue(start);
        reachable[start.X, start.Y] = true;

        Vector2I[] dirs = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        // Descoperim tot ce este conectat la player
        while (queue.Count > 0)
        {
            Vector2I curr = queue.Dequeue();
            foreach (Vector2I dir in dirs)
            {
                Vector2I next = curr + dir;
                if (grid.IsInBounds(next) && !reachable[next.X, next.Y] && grid.Tiles[next.X, next.Y].IsWalkable)
                {
                    reachable[next.X, next.Y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        // Transformam in perete orice tile pe care nu am putut calca
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.Tiles[x, y].IsWalkable && !reachable[x, y])
                {
                    grid.Tiles[x, y].IsWalkable = false;
                }
            }
        }
    }
    // Algoritm de tip "Flood Fill" (Breadth-First Search) pentru a garanta ca jocul poate fi castigat
    private bool IsPathValid(GridModel grid, Vector2I start, Vector2I exit)
    {
        bool[,] visited = new bool[grid.Width, grid.Height];
        System.Collections.Generic.Queue<Vector2I> queue = new System.Collections.Generic.Queue<Vector2I>();

        queue.Enqueue(start);
        visited[start.X, start.Y] = true;

        Vector2I[] dirs = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        while (queue.Count > 0)
        {
            Vector2I curr = queue.Dequeue();
            if (curr == exit) return true; // Found a path to the exit

            foreach (Vector2I dir in dirs)
            {
                Vector2I next = curr + dir;
                // If we are within bounds, not visited, and walkable, we can continue
                if (next.X >= 0 && next.X < grid.Width && next.Y >= 0 && next.Y < grid.Height)
                {
                    if (!visited[next.X, next.Y] && grid.Tiles[next.X, next.Y].IsWalkable)
                    {
                        visited[next.X, next.Y] = true;
                        queue.Enqueue(next);
                    }
                }
            }
        }
        return false; // Exit is blocked
    }

    // Spawns enemies dynamically using a Safe Zone matrix
    private List<Entity> SpawnEnemies(GridModel grid, Vector2I playerPos, Vector2I exitPos, int requestedCount, int currentDepth, List<string> unlockedEnemyIds)
    {
        List<Entity> enemies = new List<Entity>();
        Random rng = new Random();
        List<Entity> availablePool = EnemyRegistry.GetSpawnPool(currentDepth, unlockedEnemyIds);

        if (availablePool.Count == 0) return enemies;

        //We build the safe zone matrix
        List<Vector2I> safeSpawnPoints = new List<Vector2I>();
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Vector2I currentPos = new Vector2I(x, y);
                
                // If it's a walkable tile and not the exit
                if (grid.Tiles[x, y].IsWalkable && currentPos != exitPos)
                {
                    // Manhattan distance from the player must be STRICTLY greater than or equal to 2
                    // (A distance of 1 would mean it spawns right next to you, which is unfair)
                    int distanceToPlayer = Math.Abs(currentPos.X - playerPos.X) + Math.Abs(currentPos.Y - playerPos.Y);
                    
                    if (distanceToPlayer >= 2)
                    {
                        safeSpawnPoints.Add(currentPos);
                    }
                }
            }
        }

        // Calculate the number of enemies to spawn based on the requested count and the available safe spawn points
        // We take the minimum of the requested count and the number of safe spawn points to ensure
        int enemiesToSpawn = Math.Min(requestedCount, safeSpawnPoints.Count);

        // Spawn the enemies
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // We choose a random spawn point from the safe list
            int spawnIndex = rng.Next(safeSpawnPoints.Count);
            Vector2I spawnPos = safeSpawnPoints[spawnIndex];
            
            // Eliminate the chosen spawn point from the list to avoid duplicates
            safeSpawnPoints.RemoveAt(spawnIndex);

            // Choose enemy based on weighted spawn chance
            int totalWeight = availablePool.Sum(e => e.SpawnWeight);
            int randomValue = rng.Next(0, totalWeight);
            int cumulativeWeight = 0;
            Entity selectedEnemyTemplate = availablePool[0];

            foreach (Entity template in availablePool)
            {
                cumulativeWeight += template.SpawnWeight;
                if (randomValue < cumulativeWeight)
                {
                    selectedEnemyTemplate = template;
                    break;
                }
            }

            // Instance the enemy and set its position
            Entity newEnemy = EnemyRegistry.CreateEnemyInstance(selectedEnemyTemplate.Id);
            newEnemy.Position = spawnPos;
            enemies.Add(newEnemy);
        }

        return enemies;
    }
    private void SpawnChests(GridModel grid, int currentDepth, Vector2I start, Vector2I exit)
    {
        Random rng = new Random();

        int chestCount = rng.Next(0, 100) < 35 ? 1 : 0;
        
        // If the player is deeper than depth 5, there's a 15% chance to spawn a second chest
        if (currentDepth > 5 && rng.Next(0, 100) < 15) chestCount = 2;

        List<Vector2I> validPoints = new List<Vector2I>();

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                // A chest can only spawn on a walkable tile that doesn't already have an effect, and it can't be on the start or exit positions
                if (grid.Tiles[x, y].IsWalkable && grid.Tiles[x, y].Effect == TileEffect.None)
                {
                    Vector2I pos = new Vector2I(x, y);
                    if (pos != start && pos != exit)
                    {
                        validPoints.Add(pos);
                    }
                }
            }
        }

        // Random extraction
        for (int i = 0; i < chestCount && validPoints.Count > 0; i++)
        {
            int index = rng.Next(validPoints.Count);
            Vector2I chestPos = validPoints[index];
            
            grid.Tiles[chestPos.X, chestPos.Y].Effect = TileEffect.Chest;
            validPoints.RemoveAt(index);
        }
    }
}