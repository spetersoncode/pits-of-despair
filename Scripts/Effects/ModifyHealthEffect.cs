using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that modifies the target entity's health.
/// Supports both healing (positive) and damage (negative) amounts.
/// Supports flat amounts, dice-based amounts, and stat scaling.
/// </summary>
public class ModifyHealthEffect : Effect
{
    public override string Type => "modify_health";
    public override string Name => "Modify Health";

    /// <summary>
    /// Base amount to modify health by. Positive heals, negative damages.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable amount (e.g., "2d6").
    /// Added to the base amount.
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Stat to scale amount with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Whether Amount is a percentage of max HP.
    /// </summary>
    public bool Percent { get; set; } = false;

    public ModifyHealthEffect()
    {
        Amount = 0;
    }

    /// <summary>
    /// Creates a modify health effect with a fixed amount.
    /// Positive values heal, negative values damage.
    /// </summary>
    public ModifyHealthEffect(int amount)
    {
        Amount = amount;
    }

    /// <summary>
    /// Creates a modify health effect from a unified effect definition.
    /// Supports dice, stat scaling, and percentage mode from skills.
    /// </summary>
    public ModifyHealthEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
        Percent = definition.Percent;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} has no health to modify.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate amount with dice and scaling
        int amount;
        if (Percent)
        {
            // Percentage of max HP
            amount = (int)(healthComponent.MaxHealth * Amount / 100f);
        }
        else
        {
            amount = CalculateScaledAmount(Amount, Dice, ScalingStat, ScalingMultiplier, context);
        }

        if (amount == 0)
        {
            return EffectResult.CreateFailure(
                $"{context.GetSourceName()} has no effect.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        if (amount > 0)
        {
            // Healing
            int oldHealth = healthComponent.CurrentHealth;
            healthComponent.Heal(amount);
            int actualHealing = healthComponent.CurrentHealth - oldHealth;

            if (actualHealing == 0)
            {
                return EffectResult.CreateFailure(
                    $"{targetName} is already at full health.",
                    Palette.ToHex(Palette.Disabled)
                );
            }

            return EffectResult.CreateSuccess(
                $"{targetName} heals {actualHealing} Health.",
                Palette.ToHex(Palette.Success),
                target
            );
        }
        else
        {
            // Damage (negative amount)
            int damageAmount = -amount;
            int oldHealth = healthComponent.CurrentHealth;
            healthComponent.TakeDamage(damageAmount);
            int actualDamage = oldHealth - healthComponent.CurrentHealth;

            var result = EffectResult.CreateSuccess(
                $"{targetName} loses {actualDamage} Health.",
                Palette.ToHex(Palette.CombatDamage),
                target
            );
            result.DamageDealt = actualDamage;
            return result;
        }
    }
}
