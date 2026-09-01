using System;
using System.Collections.Generic;
using Godot;
using System.Diagnostics;

namespace Deckrinth.Services;

public class PathfindingService
{
    private Model.GridModel _grid;

    public PathfindingService(Model.GridModel grid)
    {
        _grid = grid;
    }

    // Flow field (Dijkstra map) generation for a given target position
    public int[,] GenerateFlowField(Vector2I targetPosition)
    {
        Stopwatch sw = Stopwatch.StartNew();
        int[,] flowField = new int[_grid.Width, _grid.Height];
        
        // We initialize the flow field with a high value (9999) to represent unvisited cells
        for (int x = 0; x < _grid.Width; x++)
            for (int y = 0; y < _grid.Height; y++)
                flowField[x, y] = 9999;

        var queue = new Queue<Vector2I>();
        queue.Enqueue(targetPosition);
        flowField[targetPosition.X, targetPosition.Y] = 0;

        // We use 4 directions (4 cardinal) for flow field generation, 8 direction bugged the linear enemies
        Vector2I[] dirs = { 
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
        };

        while (queue.Count > 0)
        {
            Vector2I current = queue.Dequeue();
            int currentDist = flowField[current.X, current.Y];

            foreach (Vector2I dir in dirs)
            {
                Vector2I neighbor = current + dir;
                
                // If the neighbor is within bounds and walkable (we ignore enemies here to allow routes "through" them)
                if (_grid.IsInBounds(neighbor) && _grid.IsCellWalkable(neighbor))
                {
                    if (flowField[neighbor.X, neighbor.Y] > currentDist + 1)
                    {
                        flowField[neighbor.X, neighbor.Y] = currentDist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        sw.Stop();
        // Will print at every step enemies take towards me
        GD.Print($"[TEST 2] FlowField Dijkstra (Size {_grid.Width}x{_grid.Height}) a durat: {sw.ElapsedMilliseconds} ms ({sw.ElapsedTicks} ticks)");
        return flowField;
    }
}