using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
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
    /// Emitted when an attack hits and deals damage (attacker, target, damage, attackName)
    /// </summary>
    [Signal]
    public delegate void AttackHitEventHandler(BaseEntity attacker, BaseEntity target, int damage, string attackName);

    /// <summary>
    /// Emitted when an attack hits but deals no damage due to armor (attacker, target, attackName)
    /// </summary>
    [Signal]
    public delegate void AttackBlockedEventHandler(BaseEntity attacker, BaseEntity target, string attackName);

    /// <summary>
    /// Emitted when an attack misses (attacker, target, attackName)
    /// </summary>
    [Signal]
    public delegate void AttackMissedEventHandler(BaseEntity attacker, BaseEntity target, string attackName);

    /// <summary>
    /// Emitted when an attack occurs (attacker, target, damage, attackName)
    /// DEPRECATED: Use AttackHit, AttackBlocked, or AttackMissed instead
    /// </summary>
    [Signal]
    public delegate void AttackExecutedEventHandler(BaseEntity attacker, BaseEntity target, int damage, string attackName);

    /// <summary>
    /// Emitted when an entity performs an action with a message (actor, message, color)
    /// </summary>
    [Signal]
    public delegate void ActionMessageEventHandler(BaseEntity actor, string message, string color);

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
    /// Validates the attack and applies damage to target's HealthComponent using opposed 2d6 rolls.
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

        // Get stats components
        var attackerStats = attacker.GetNodeOrNull<StatsComponent>("StatsComponent");
        var targetStats = target.GetNodeOrNull<StatsComponent>("StatsComponent");

        if (attackerStats == null)
        {
            GD.PushWarning($"CombatSystem: Attacker {attacker.DisplayName} has no StatsComponent");
            return;
        }

        if (targetStats == null)
        {
            GD.PushWarning($"CombatSystem: Target {target.DisplayName} has no StatsComponent");
            return;
        }

        // Determine if this is a melee or ranged attack
        bool isMelee = attackData.Type == AttackType.Melee;

        // PHASE 1: Opposed Attack Roll (2d6 + modifiers)
        int attackModifier = attackerStats.GetAttackModifier(isMelee);
        int defenseModifier = targetStats.GetDefenseModifier();

        int attackRoll = DiceRoller.Roll(2, 6, attackModifier);
        int defenseRoll = DiceRoller.Roll(2, 6, defenseModifier);

        // Check if attack hits (attacker roll >= defender roll, ties go to attacker)
        bool hit = attackRoll >= defenseRoll;

        if (!hit)
        {
            // Attack missed
            EmitSignal(SignalName.AttackMissed, attacker, target, attackData.Name);
            EmitSignal(SignalName.AttackExecuted, attacker, target, 0, attackData.Name); // Legacy support
            return;
        }

        // PHASE 2: Damage Calculation (weapon damage + STR [if melee] - armor)
        int baseDamage = DiceRoller.Roll(attackData.DiceNotation);
        int damageBonus = attackerStats.GetDamageBonus(isMelee);
        int armor = targetStats.TotalArmor;

        int finalDamage = Mathf.Max(0, baseDamage + damageBonus - armor);

        // PHASE 3: Apply Damage and Emit Feedback
        if (finalDamage > 0)
        {
            targetHealth.TakeDamage(finalDamage);
            EmitSignal(SignalName.AttackHit, attacker, target, finalDamage, attackData.Name);
            EmitSignal(SignalName.AttackExecuted, attacker, target, finalDamage, attackData.Name); // Legacy support
        }
        else
        {
            // Hit but armor absorbed all damage
            EmitSignal(SignalName.AttackBlocked, attacker, target, attackData.Name);
            EmitSignal(SignalName.AttackExecuted, attacker, target, 0, attackData.Name); // Legacy support
        }
    }

    /// <summary>
    /// Emit attack feedback for actions that execute combat directly.
    /// Used by the Action system to maintain consistent combat event signaling.
    /// </summary>
    public void EmitAttackFeedback(BaseEntity attacker, BaseEntity target, int damage, string attackName)
    {
        EmitSignal(SignalName.AttackExecuted, attacker, target, damage, attackName);
    }

    /// <summary>
    /// Emit a general action message for display in the message log.
    /// Used by actions to provide feedback about non-combat activities.
    /// </summary>
    public void EmitActionMessage(BaseEntity actor, string message, string color = "#ffffff")
    {
        EmitSignal(SignalName.ActionMessage, actor, message, color);
    }
}
