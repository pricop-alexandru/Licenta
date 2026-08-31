using Godot;
using Deckrinth.Model;
using System.Collections.Generic;
using System.Linq;

namespace Deckrinth.Services;

public class CombatService
{
    private GridModel _grid;
    private List<Entity> _entities;
    private Entity _player;

    public CombatService(GridModel grid, List<Entity> entities, Entity player)
    {
        _grid = grid;
        _entities = entities;
        _player = player;
    }

    public bool AttemptMoveOrAttack(Entity actor, Vector2I direction, out bool freeActionGained, out bool chestOpened)
    {
        freeActionGained = false;
        chestOpened = false;
        Vector2I currentPos = actor.Position;
        Vector2I targetPos = currentPos + direction;

        if (!_grid.IsCellWalkable(targetPos)) 
            return false; 

        Entity targetEntity = GetEntityAt(targetPos);

        if (targetEntity != null)
        {
            if (targetEntity.Faction == actor.Faction)
                return false;

            targetEntity.TakeHit(out bool bouncedBack);

            if (!bouncedBack && targetEntity.IsDead)
            {
                MoveEntityOnGrid(actor, currentPos, targetPos);
                EvaluateTileEffect(actor, targetPos, out freeActionGained, out chestOpened);
            }
            
            return true; 
        }
        else
        {
            MoveEntityOnGrid(actor, currentPos, targetPos);
            EvaluateTileEffect(actor, targetPos, out freeActionGained, out chestOpened);
            return true;
        }
    }
    public void ExecuteRangedAttack(Vector2I targetPos)
    {
        Entity targetEntity = GetEntityAt(targetPos);

        if (targetEntity != null)
        {
            // Loveste inamicul. Daca vrem, pe viitor putem pasa damage-ul cartii ca argument.
            targetEntity.TakeHit(out bool bouncedBack);
        }
    }

    // Recursive Push Logic (Domino Effect)
    public void ApplyPush(Entity target, Vector2I direction)
    {
        // 1. Dead or immovable entities (Bosses) absorb the force but do not move
        if (target.IsDead || target.IsKnockbackResistant) return;

        Vector2I currentPos = target.Position;
        Vector2I newPos = currentPos + direction;

        // 2. Boundary Checking (The Void)
        if (!_grid.IsInBounds(newPos))
        {
            target.ForceKill(); 
            _grid.Tiles[currentPos.X, currentPos.Y].IsWalkable = true;
            return;
        }

        // 3. Chain Reaction Evaluation
        Entity entityAtTarget = GetEntityAt(newPos);
        if (entityAtTarget != null)
        {
            if (entityAtTarget.IsKnockbackResistant)
            {
                // Crashed into a Boss. Both take damage, nobody moves.
                target.TakeHit(out _);
                entityAtTarget.TakeHit(out _);
                return;
            }
            else
            {
                // Recursively push the next entity first
                ApplyPush(entityAtTarget, direction);

                // After attempting to push the next entity, verify if the space opened up
                if (entityAtTarget.IsDead || entityAtTarget.Position != newPos)
                {
                    // Space is clear, cascade movement forward
                    MoveEntityOnGrid(target, currentPos, newPos);
                    EvaluateTileEffect(target, newPos, out _, out _);
                }
                else
                {
                    // The next entity crashed into a wall. The chain compresses.
                    target.TakeHit(out _);
                    entityAtTarget.TakeHit(out _);
                }
                return;
            }
        }

        // 4. Solid Wall Collision
        if (!_grid.IsCellWalkable(newPos))
        {
            target.TakeHit(out _);
            return;
        }

        // 5. Clean Push onto empty tile
        MoveEntityOnGrid(target, currentPos, newPos);
        EvaluateTileEffect(target, newPos, out _, out _); 
    }

    private void MoveEntityOnGrid(Entity entity, Vector2I from, Vector2I to)
    {
        //_grid.Tiles[from.X, from.Y].IsWalkable = true;
        //_grid.Tiles[to.X, to.Y].IsWalkable = false;
        entity.Position = to;
    }

    private void EvaluateTileEffect(Entity entity, Vector2I pos, out bool freeActionGained, out bool chestOpened)
    {
        freeActionGained = false;
        chestOpened = false;
        TileEffect effect = _grid.Tiles[pos.X, pos.Y].Effect;

        switch (effect)
        {
            case TileEffect.Chest:
                // Doar jucatorul poate declansa cufarul
                if (entity.Faction == EntityFaction.Player)
                {
                    chestOpened = true;
                    _grid.Tiles[pos.X, pos.Y].Effect = TileEffect.None; // Consuma cufarul de pe harta
                }
                break;
            case TileEffect.Spikes:
                entity.TakeHit(out _); 
                break;
            case TileEffect.Shielding:
                entity.HasShield = true;
                _grid.Tiles[pos.X, pos.Y].Effect = TileEffect.None; 
                break;
            case TileEffect.Speed:
                freeActionGained = true;
                _grid.Tiles[pos.X, pos.Y].Effect = TileEffect.None;
                break;
            case TileEffect.Quicksand:
                entity.IsRooted = true; 
                break;
        }
    }

    private Entity GetEntityAt(Vector2I pos)
    {
        if (_player != null && _player.Position == pos && !_player.IsDead)
        {
            return _player;
        }
        return _entities.FirstOrDefault(e => e.Position == pos && !e.IsDead);
    }
}