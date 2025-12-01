using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using System.Linq;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for moving an entity in a given direction.
/// Handles collision detection, bump-to-attack for player, and terrain validation.
/// </summary>
public class MoveAction : Action
{
    private readonly Vector2I _direction;

    public override string Name => "Move";

    public MoveAction(Vector2I direction)
    {
        _direction = direction;
    }

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || context == null)
        {
            return false;
        }

        var targetPos = actor.GridPosition.Add(_direction);

        // Check for entity at target position
        var targetEntity = GetEntityAtPosition(targetPos, context);

        if (targetEntity != null)
        {
            // If target is walkable (like items), movement is valid
            if (targetEntity.IsWalkable)
            {
                return context.MapSystem.IsWalkable(targetPos);
            }

            // Player can swap with any friendly creature (no cooldown)
            if (actor == context.Player && actor.Faction.IsFriendlyTo(targetEntity.Faction))
            {
                return true;
            }

            // AI creatures can swap positions with friendly creatures (but not with player)
            // Prevent ping-pong swapping (can't immediately swap back)
            if (actor.Faction.IsFriendlyTo(targetEntity.Faction) && targetEntity != context.Player)
            {
                // Prevent immediate re-swap (ping-pong)
                if (actor.LastSwappedWith == targetEntity)
                    return false;
                return true;
            }

            // Player-only: bump-to-attack anything with health (creatures, destructible decorations)
            if (actor == context.Player)
            {
                var targetHealth = targetEntity.GetNodeOrNull<HealthComponent>("HealthComponent");
                var actorAttack = actor.GetNodeOrNull<AttackComponent>("AttackComponent");

                // Can bump-to-attack if target has health and actor can attack
                if (targetHealth != null && actorAttack != null)
                {
                    return true;
                }
            }

            // Otherwise blocked by non-walkable entity
            return false;
        }

        // No entity blocking, check if tile is walkable
        return context.MapSystem.IsWalkable(targetPos);
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (!CanExecute(actor, context))
        {
            return ActionResult.CreateFailure("Cannot move in that direction.");
        }

        var currentPos = actor.GridPosition;
        var targetPos = currentPos.Add(_direction);
        var targetEntity = GetEntityAtPosition(targetPos, context);

        // Check for bump interactions
        if (targetEntity != null && !targetEntity.IsWalkable)
        {
            // Creatures can swap positions with friendly creatures
            // But AI cannot swap with the player - only player can initiate that
            bool canSwap = actor.Faction.IsFriendlyTo(targetEntity.Faction) &&
                           (actor == context.Player || targetEntity != context.Player);

            if (canSwap)
            {
                // Prevent ping-pong: can't immediately swap back with same entity
                // Player is exempt - they can always swap with allies
                if (actor != context.Player && actor.LastSwappedWith == targetEntity)
                {
                    // Blocked - can't re-swap, treat as blocked
                    return ActionResult.CreateFailure("Cannot swap back immediately.");
                }

                targetEntity.SetGridPosition(currentPos);
                actor.SetGridPosition(targetPos);

                // Track swap for cooldown (both entities remember)
                actor.LastSwappedWith = targetEntity;
                targetEntity.LastSwappedWith = actor;

                // Breaking grapples when swapping positions
                BreakGrapplesOnMove(actor, context);

                return ActionResult.CreateSuccess();
            }

            // Player-only: bump-to-attack anything with health (creatures, destructible decorations)
            if (actor == context.Player)
            {
                var targetHealth = targetEntity.GetNodeOrNull<HealthComponent>("HealthComponent");
                var actorAttack = actor.GetNodeOrNull<AttackComponent>("AttackComponent");

                if (targetHealth != null && actorAttack != null)
                {
                    // Execute attack instead of movement
                    var attackAction = new AttackAction(targetEntity);
                    return attackAction.Execute(actor, context);
                }
            }
        }

        // Perform normal movement (not a swap)
        actor.SetGridPosition(targetPos);

        // Clear swap cooldown since we moved normally
        actor.LastSwappedWith = null;

        // Break any grapples the actor was maintaining
        BreakGrapplesOnMove(actor, context);

        return ActionResult.CreateSuccess();
    }

    /// <summary>
    /// Breaks all grapples that the actor is maintaining.
    /// Called when the actor moves away from grappled targets.
    /// </summary>
    private void BreakGrapplesOnMove(BaseEntity actor, ActionContext context)
    {
        foreach (var entity in context.EntityManager.GetAllEntities())
        {
            var grappled = entity.GetActiveConditions()
                .OfType<GrappledCondition>()
                .FirstOrDefault(g => g.Grappler == actor);

            if (grappled != null)
            {
                // Mark as broken by movement so OnRemoved returns empty message
                grappled.WasBrokenByMovement = true;
                entity.RemoveConditionByType("grappled");

                // Emit release message from the actor (player)
                if (actor == context.Player)
                {
                    actor.EmitSignal(
                        BaseEntity.SignalName.ConditionMessage,
                        $"You release the {entity.DisplayName} from your grapple.",
                        Palette.ToHex(Palette.Default)
                    );
                }
            }
        }
    }

    /// <summary>
    /// Get entity at a specific grid position (checks both player and managed entities).
    /// </summary>
    private BaseEntity? GetEntityAtPosition(GridPosition position, ActionContext context)
    {
        // Check player
        if (context.Player != null && context.Player.GridPosition.Equals(position))
        {
            return context.Player;
        }

        // Check managed entities
        return context.EntityManager.GetEntityAtPosition(position);
    }
}
