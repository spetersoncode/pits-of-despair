using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// System that validates and executes combat between entities.
/// Coordinates AttackComponents and HealthComponents.
/// </summary>
public partial class CombatSystem : Node
{
    /// <summary>
    /// Emitted when an attack occurs (attacker, target, damage, attackName)
    /// </summary>
    [Signal]
    public delegate void AttackExecutedEventHandler(BaseEntity attacker, BaseEntity target, int damage, string attackName);

    /// <summary>
    /// Register an AttackComponent to listen for attack requests.
    /// Called by GameLevel or EntityManager when entities with AttackComponents are created.
    /// </summary>
    /// <param name="component">The AttackComponent to register.</param>
    public void RegisterAttackComponent(AttackComponent component)
    {
        // Use lambda to capture the component reference in a closure
        component.AttackRequested += (target, attackIndex) => OnAttackRequested(component, target, attackIndex);
    }

    /// <summary>
    /// Handle attack requests from AttackComponents.
    /// Validates the attack and applies damage to target's HealthComponent.
    /// </summary>
    /// <param name="component">The AttackComponent that requested the attack.</param>
    /// <param name="target">The target entity.</param>
    /// <param name="attackIndex">Index of the attack to use.</param>
    private void OnAttackRequested(AttackComponent component, BaseEntity target, int attackIndex)
    {
        var attacker = component.GetEntity();
        if (attacker == null)
        {
            GD.PushWarning("CombatSystem: AttackComponent has no parent entity");
            return;
        }

        // Validate target has health
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null)
        {
            GD.PushWarning($"CombatSystem: Target {target.DisplayName} has no HealthComponent");
            return;
        }

        // Check if target is already dead
        if (!targetHealth.IsAlive())
        {
            return;
        }

        // Get attack data
        var attackData = component.GetAttack(attackIndex);
        if (attackData == null)
        {
            GD.PushWarning($"CombatSystem: Invalid attack index {attackIndex}");
            return;
        }

        // Validate attack range
        int distance = DistanceHelper.ChebyshevDistance(attacker.GridPosition, target.GridPosition);
        if (distance > attackData.Range)
        {
            GD.PushWarning($"CombatSystem: Target out of range (distance: {distance}, range: {attackData.Range})");
            return;
        }

        // Roll damage
        int damage = GD.RandRange(attackData.MinDamage, attackData.MaxDamage);

        // Apply damage
        targetHealth.TakeDamage(damage);

        // Emit combat feedback
        EmitSignal(SignalName.AttackExecuted, attacker, target, damage, attackData.Name);
    }

    /// <summary>
    /// Emit attack feedback for actions that execute combat directly.
    /// Used by the Action system to maintain consistent combat event signaling.
    /// </summary>
    public void EmitAttackFeedback(BaseEntity attacker, BaseEntity target, int damage, string attackName)
    {
        EmitSignal(SignalName.AttackExecuted, attacker, target, damage, attackName);
    }
}
