using Godot;
using System;
using System.Collections.Generic;

namespace Deckrinth.Model;

// The matrix of the game
public class GridModel
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public TileData[,] Tiles { get; private set; }

    public GridModel(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new TileData[width, height];
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                // Implicit, grid-ul este plin (blocat)
                Tiles[x, y] = new TileData(new Vector2I(x, y), false);
            }
        }
    }

    // O(1) time complexity - Boundary checking
    public bool IsInBounds(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
    }

    // Centralized method to check if a cell is walkable, considering both bounds and tile properties
    public bool IsCellWalkable(Vector2I position)
    {
        // If the position is out of bounds, we consider it non-walkable
        if (!IsInBounds(position)) return false;

        // Returns true if the tile at the given position is walkable, false otherwise
        return Tiles[position.X, position.Y].IsWalkable;
    }
    public List<Vector2I> GetTilesInRadius(Vector2I center, int radius)
    {
        List<Vector2I> tiles = new List<Vector2I>();
        for (int x = center.X - radius; x <= center.X + radius; x++)
        {
            for (int y = center.Y - radius; y <= center.Y + radius; y++)
            {
                Vector2I pos = new Vector2I(x, y);
                if (IsInBounds(pos)) tiles.Add(pos);
            }
        }
        return tiles;
    }
}