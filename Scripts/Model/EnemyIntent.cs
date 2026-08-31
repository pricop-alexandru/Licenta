using System.Collections.Generic;
using Godot;

namespace Deckrinth.Model;

public enum IntentType { Move, AttackPlayer, Spell }

public class EnemyIntent
{
    public IntentType Type { get; set; }
    public Vector2I TargetPosition { get; set; }
    public List<Vector2I> FullPath { get; set; } // Used by Godot to draw the path arrows

    public EnemyIntent()
    {
        Type = IntentType.Move;
        TargetPosition = Vector2I.Zero;
        FullPath = new List<Vector2I>();
    }
}