using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that performs a melee attack against the target.
/// Uses weapon damage + optional bonus damage, with full attack roll resolution.
/// This is different from DamageEffect which applies flat/scaled damage without attack rolls.
/// </summary>
public class MeleeAttackEffect : Effect
{
    public override string Type => "melee_attack";
    public override string Name => "Melee Attack";

    /// <summary>
    /// Bonus damage added to the attack (on top of weapon + STR).
    /// </summary>
    public int BonusDamage { get; set; } = 0;

    /// <summary>
    /// Number of targets this attack can hit (for multi-target attacks like Cleave).
    /// Default is 1 for single-target attacks.
    /// </summary>
    public int Targets { get; set; } = 1;

    public MeleeAttackEffect() { }

    /// <summary>
    /// Creates a melee attack effect from an effect definition.
    /// </summary>
    public MeleeAttackEffect(EffectDefinition definition)
    {
        BonusDamage = definition.Amount;
        Targets = definition.Targets;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var caster = context.Caster;
        var targetName = target.DisplayName;
        var casterName = caster?.DisplayName ?? "Something";

        // Validate caster exists
        if (caster == null)
        {
            return EffectResult.CreateFailure(
                "No attacker for melee attack.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Validate target has health
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null || !targetHealth.IsAlive())
        {
            return EffectResult.CreateFailure(
                $"{targetName} cannot be attacked.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Get attack component for weapon data
        var attackComponent = caster.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{casterName} cannot attack.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Get the primary melee attack (index 0)
        var attackData = attackComponent.GetAttack(0);
        if (attackData == null || attackData.Type != AttackType.Melee)
        {
            return EffectResult.CreateFailure(
                $"{casterName} has no melee attack.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Get stats components for rolls
        var attackerStats = caster.GetNodeOrNull<StatsComponent>("StatsComponent");
        var targetStats = target.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Attacker must have stats to attack
        if (attackerStats == null)
        {
            return EffectResult.CreateFailure(
                "Missing stats for combat.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Get skill name for messaging
        var skillName = context.Skill?.Name ?? "attack";

        // PHASE 1: Opposed Attack Roll (2d6 + modifiers)
        // Targets without stats (decorations/objects) have -10 defense - essentially auto-hit
        int attackModifier = attackerStats.GetAttackModifier(isMelee: true);
        int defenseModifier = targetStats?.GetDefenseModifier() ?? -10;

        int attackRoll = DiceRoller.Roll(2, 6, attackModifier);
        int defenseRoll = DiceRoller.Roll(2, 6, defenseModifier);

        // Check if attack hits (attacker roll >= defender roll, ties go to attacker)
        bool hit = attackRoll >= defenseRoll;

        if (!hit)
        {
            // Attack missed - emit signal via CombatSystem
            context.ActionContext.CombatSystem?.EmitSignal(
                Systems.CombatSystem.SignalName.AttackMissed,
                caster, target, skillName);

            return EffectResult.CreateFailure(
                $"{casterName}'s {skillName} misses {targetName}!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // PHASE 2: Damage Calculation (weapon damage + STR + bonus - armor)
        // Targets without stats (decorations) have 0 armor
        int baseDamage = DiceRoller.Roll(attackData.DiceNotation);
        int strBonus = attackerStats.GetDamageBonus(isMelee: true);
        int armor = targetStats?.TotalArmor ?? 0;

        int finalDamage = Mathf.Max(0, baseDamage + strBonus + BonusDamage - armor);

        // PHASE 3: Apply damage and return result
        if (finalDamage > 0)
        {
            // Calculate actual damage after resistances/vulnerabilities
            int actualDamage = targetHealth.CalculateDamage(finalDamage, attackData.DamageType);

            // Emit hit signal BEFORE applying damage
            context.ActionContext.CombatSystem?.EmitSignal(
                Systems.CombatSystem.SignalName.AttackHit,
                caster, target, actualDamage, skillName);

            // Apply the damage
            targetHealth.TakeDamage(finalDamage, attackData.DamageType, caster);

            var result = EffectResult.CreateSuccess(
                $"{casterName}'s {skillName} hits {targetName} for {actualDamage} damage!",
                Palette.ToHex(Palette.CombatDamage),
                target
            );
            result.DamageDealt = actualDamage;
            return result;
        }
        else
        {
            // Hit but armor absorbed all damage
            context.ActionContext.CombatSystem?.EmitSignal(
                Systems.CombatSystem.SignalName.AttackBlocked,
                caster, target, skillName);

            return EffectResult.CreateSuccess(
                $"{casterName}'s {skillName} is blocked by {targetName}'s armor!",
                Palette.ToHex(Palette.CombatBlocked),
                target
            );
        }
    }
}
