using Godot;

namespace Deckrinth.Model;

// Defines specific effects a tile can have (expandable for future mechanics)
public enum TileEffect
{
    None,
    Quicksand,
    Spikes,
    Speed,
	Shielding,
    Chest
}

public struct TileData
{
    public Vector2I Position;
    public bool IsWalkable;
    public TileEffect Effect; // The new property for tile behaviors

    public TileData(Vector2I position, bool isWalkable)
    {
        Position = position;
        IsWalkable = isWalkable;
        Effect = TileEffect.None; // Default is a normal floor
    }
}