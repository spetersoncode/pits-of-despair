using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for performing a reach attack with a melee weapon that has extended range.
/// Reach weapons (e.g., spears) use STR for attack/damage like melee weapons but can target
/// enemies beyond adjacent tiles. Requires line of sight like ranged attacks.
/// </summary>
public class ReachAttackAction : Action
{
    private readonly GridPosition _targetPosition;
    private readonly int _attackIndex;

    public override string Name => "Reach Attack";

    public ReachAttackAction(GridPosition targetPosition, int attackIndex = 0)
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

        // Validate attack index and that it's a melee attack with reach (range > 1)
        var attackData = attackComponent.GetAttack(_attackIndex);
        if (attackData == null || attackData.Type != AttackType.Melee || attackData.Range <= 1)
        {
            return false;
        }

        // Validate range (use Chebyshev distance)
        int distance = DistanceHelper.ChebyshevDistance(actor.GridPosition, _targetPosition);
        if (distance > attackData.Range || distance == 0)
        {
            return false;
        }

        // Validate line of sight using FOV calculator with grid-based distance
        var visibleTiles = FOVCalculator.CalculateVisibleTiles(
            actor.GridPosition,
            attackData.Range,
            context.MapSystem,
            Helpers.DistanceMetric.Chebyshev);

        if (!visibleTiles.Contains(_targetPosition))
        {
            return false;
        }

        // Validate there's a target entity at the position
        var entityAtTarget = context.EntityManager.GetEntityAtPosition(_targetPosition);
        if (entityAtTarget == null)
        {
            return false;
        }

        // Validate target has health and is alive
        var targetHealth = entityAtTarget.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null || !targetHealth.IsAlive())
        {
            return false;
        }

        return true;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (!CanExecute(actor, context))
        {
            return ActionResult.CreateFailure("Cannot reach that target.");
        }

        var attackComponent = actor.GetNodeOrNull<AttackComponent>("AttackComponent");

        // Find target entity at position
        var target = context.EntityManager.GetEntityAtPosition(_targetPosition);
        if (target == null)
        {
            return ActionResult.CreateFailure("No target at that position.");
        }

        // Use the proper combat system with opposed rolls
        // This will emit signals that CombatSystem.OnAttackRequested handles
        // Since this is a melee attack type, it will use STR modifier automatically
        attackComponent!.RequestAttack(target, _attackIndex);

        return ActionResult.CreateSuccess();
    }
}
