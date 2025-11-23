using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that deals damage to the target.
/// Supports flat damage, dice-based damage, and stat scaling.
/// </summary>
public class DamageEffect : Effect
{
    public override string Type => "damage";
    public override string Name => "Damage";

    /// <summary>
    /// Flat damage amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable damage (e.g., "2d6").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Stat to scale damage with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Damage type for resistance/vulnerability checks.
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Bludgeoning;

    public DamageEffect() { }

    /// <summary>
    /// Creates a simple damage effect with a fixed amount.
    /// </summary>
    public DamageEffect(int amount, DamageType damageType = DamageType.Bludgeoning)
    {
        Amount = amount;
        DamageType = damageType;
    }

    /// <summary>
    /// Creates a damage effect from a unified effect definition.
    /// </summary>
    public DamageEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;

        // Parse damage type if specified
        if (!string.IsNullOrEmpty(definition.DamageType))
        {
            if (System.Enum.TryParse<DamageType>(definition.DamageType, ignoreCase: true, out var dt))
            {
                DamageType = dt;
            }
        }
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} cannot be damaged.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate damage with dice and scaling
        int damage = CalculateScaledAmount(Amount, Dice, ScalingStat, ScalingMultiplier, context);

        // Ensure minimum of 1 damage if we're supposed to deal damage
        if (damage < 1 && (Amount > 0 || !string.IsNullOrEmpty(Dice)))
        {
            damage = 1;
        }

        if (damage <= 0)
        {
            return EffectResult.CreateFailure(
                $"{context.GetSourceName()} has no effect on {targetName}.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply damage with caster as source for kill attribution
        int actualDamage = healthComponent.TakeDamage(damage, DamageType, context.Caster);

        var result = EffectResult.CreateSuccess(
            $"{targetName} takes {actualDamage} damage!",
            Palette.ToHex(Palette.CombatDamage),
            target
        );
        result.DamageDealt = actualDamage;
        return result;
    }
}
