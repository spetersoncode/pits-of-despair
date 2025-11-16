using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for attacking a target entity.
/// Validates range, rolls damage, and applies it to the target.
/// </summary>
public class AttackAction : Action
{
    private readonly BaseEntity _target;
    private readonly int _attackIndex;

    public override string Name => "Attack";

    public AttackAction(BaseEntity target, int attackIndex = 0)
    {
        _target = target;
        _attackIndex = attackIndex;
    }

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || _target == null || context == null)
        {
            return false;
        }

        // Validate attacker has attack component
        var attackComponent = actor.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
        {
            return false;
        }

        // Validate attack index
        var attackData = attackComponent.GetAttack(_attackIndex);
        if (attackData == null)
        {
            return false;
        }

        // Validate target has health
        var targetHealth = _target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null)
        {
            return false;
        }

        // Check if target is already dead
        if (!targetHealth.IsAlive())
        {
            return false;
        }

        // Validate attack range
        int distance = DistanceHelper.ChebyshevDistance(actor.GridPosition, _target.GridPosition);
        if (distance > attackData.Range)
        {
            return false;
        }

        return true;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (!CanExecute(actor, context))
        {
            return ActionResult.CreateFailure("Cannot attack target.");
        }

        var attackComponent = actor.GetNodeOrNull<AttackComponent>("AttackComponent");
        var targetHealth = _target.GetNodeOrNull<HealthComponent>("HealthComponent");
        var attackData = attackComponent!.GetAttack(_attackIndex);

        // Roll damage
        int damage = GD.RandRange(attackData!.MinDamage, attackData.MaxDamage);

        // Apply damage
        targetHealth!.TakeDamage(damage);

        // Emit combat feedback through CombatSystem
        context.CombatSystem.EmitAttackFeedback(actor, _target, damage, attackData.Name);

        return ActionResult.CreateSuccess();
    }
}
