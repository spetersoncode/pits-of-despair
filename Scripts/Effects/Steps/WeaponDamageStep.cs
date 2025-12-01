using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that performs a melee attack using the caster's equipped weapon.
/// Uses weapon damage + STR + optional bonus, with attack roll from AttackRollStep.
/// </summary>
public class WeaponDamageStep : IEffectStep
{
    private readonly int _bonusDamage;

    public WeaponDamageStep(StepDefinition definition)
    {
        _bonusDamage = definition.Amount;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Check if attack was already resolved by AttackRollStep
        if (state.AttackMissed)
        {
            // Attack already missed - don't apply damage
            return;
        }

        var target = context.Target;
        var caster = context.Caster;

        if (caster == null)
        {
            messages.Add("No attacker for weapon damage.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Get attack component for weapon data
        var attackComponent = caster.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
        {
            messages.Add($"{caster.DisplayName} cannot attack.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Get the primary melee attack (index 0)
        var attackData = attackComponent.GetAttack(0);
        if (attackData == null || attackData.Type != AttackType.Melee)
        {
            messages.Add($"{caster.DisplayName} has no melee attack.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Get target health
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null || !targetHealth.IsAlive())
        {
            messages.Add($"{target.DisplayName} cannot be attacked.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Get stats for damage calculation
        var attackerStats = caster.GetNodeOrNull<StatsComponent>("StatsComponent");
        var targetStats = target.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Calculate damage (weapon dice + STR bonus + skill bonus - armor)
        int baseDamage = DiceRoller.Roll(attackData.DiceNotation);
        int strBonus = attackerStats?.GetDamageBonus(isMelee: true) ?? 0;
        // Cap STR bonus at weapon's max base roll (lighter weapons benefit less from STR)
        int maxStrBonus = attackData.GetMaxStrengthBonus();
        strBonus = Mathf.Min(strBonus, maxStrBonus);
        int armor = targetStats?.TotalArmor ?? 0;

        int finalDamage = Mathf.Max(0, baseDamage + strBonus + _bonusDamage - armor);
        var skillName = context.Skill?.Name ?? "attack";

        if (finalDamage > 0)
        {
            // Calculate actual damage after resistances/vulnerabilities
            int actualDamage = targetHealth.CalculateDamage(finalDamage, attackData.DamageType);

            // Emit hit signal BEFORE applying damage
            context.ActionContext.CombatSystem?.EmitSignal(
                Systems.CombatSystem.SignalName.AttackHit,
                caster, target, actualDamage, skillName, (int)AttackType.Melee);

            // Apply the damage
            targetHealth.TakeDamage(finalDamage, attackData.DamageType, caster);

            messages.Add(
                $"{caster.DisplayName}'s {skillName} hits {target.DisplayName} for {actualDamage} damage!",
                Palette.ToHex(Palette.CombatDamage)
            );

            state.DamageDealt = actualDamage;
            state.Success = true;
        }
        else
        {
            // Hit but armor absorbed all damage
            context.ActionContext.CombatSystem?.EmitSignal(
                Systems.CombatSystem.SignalName.AttackBlocked,
                caster, target, skillName);

            messages.Add(
                $"{caster.DisplayName}'s {skillName} is blocked by {target.DisplayName}'s armor!",
                Palette.ToHex(Palette.CombatBlocked)
            );

            state.Success = true; // Attack succeeded, just blocked
        }
    }
}
