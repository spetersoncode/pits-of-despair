using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for performing a ranged attack at a target position.
/// Validates line of sight, spawns a projectile, and applies damage on impact.
/// </summary>
public class RangedAttackAction : Action
{
    private readonly GridPosition _targetPosition;
    private readonly int _attackIndex;

    public override string Name => "Ranged Attack";

    public RangedAttackAction(GridPosition targetPosition, int attackIndex = 0)
    {
        _targetPosition = targetPosition;
        _attackIndex = attackIndex;
    }

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || context == null)
        {
            return false;
        }

        // Validate attacker has attack component
        var attackComponent = actor.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
        {
            return false;
        }

        // Validate attack index and that it's a ranged attack
        var attackData = attackComponent.GetAttack(_attackIndex);
        if (attackData == null || attackData.Type != AttackType.Ranged)
        {
            return false;
        }

        // Validate range (use Euclidean distance for circular range, matching FOV)
        int distanceSquared = DistanceHelper.EuclideanDistance(actor.GridPosition, _targetPosition);
        int rangeSquared = attackData.Range * attackData.Range;
        if (distanceSquared > rangeSquared || distanceSquared == 0)
        {
            return false;
        }

        // Validate line of sight using FOV calculator
        var visibleTiles = FOVCalculator.CalculateVisibleTiles(
            actor.GridPosition,
            attackData.Range,
            context.MapSystem);

        return visibleTiles.Contains(_targetPosition);
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (!CanExecute(actor, context))
        {
            return ActionResult.CreateFailure("Cannot attack that target.");
        }

        var attackComponent = actor.GetNodeOrNull<AttackComponent>("AttackComponent");
        var attackData = attackComponent!.GetAttack(_attackIndex);

        // Find target entity at position (may be null if shooting at empty tile)
        BaseEntity target = null;
        var entityAtTarget = context.EntityManager.GetEntityAtPosition(_targetPosition);
        if (entityAtTarget != null)
        {
            // Check if entity has health component
            if (entityAtTarget.GetNodeOrNull<HealthComponent>("HealthComponent") != null)
            {
                target = entityAtTarget;
            }
        }

        // Spawn projectile - it will handle the combat on impact
        if (actor is Player player)
        {
            // Use target entity's current position if available, otherwise use cursor position
            var projectileTarget = target != null ? target.GridPosition : _targetPosition;
            player.EmitSignal(Player.SignalName.RangedAttackRequested,
                actor.GridPosition.ToVector2I(), projectileTarget.ToVector2I(), target, _attackIndex);
        }
        else
        {
            // For non-player entities, we could add a similar signal
            // For now, just do immediate hit (no projectile animation for enemies)
            if (target != null)
            {
                attackComponent.RequestAttack(target, _attackIndex);
            }
        }

        return ActionResult.CreateSuccess();
    }
}
