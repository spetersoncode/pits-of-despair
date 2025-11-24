using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that damages the target and heals the caster by half the damage dealt (rounded down).
/// Used by vampiric touch wands and similar life-draining effects.
/// </summary>
public class VampiricTouchEffect : Effect
{
    public override string Type => "vampiric_touch";
    public override string Name => "Vampiric Touch";

    /// <summary>
    /// Base damage amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for damage (e.g., "2d6").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Damage type for the attack. Defaults to Necrotic for life-draining effects.
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Necrotic;

    public VampiricTouchEffect() { }

    public VampiricTouchEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;

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
        var caster = context.Caster;

        if (target == null)
        {
            return EffectResult.CreateFailure(
                "No target for vampiric touch.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null)
        {
            return EffectResult.CreateFailure(
                $"{target.DisplayName} cannot be drained.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate and apply damage
        int damage = Amount;
        if (!string.IsNullOrEmpty(Dice))
        {
            damage += DiceRoller.Roll(Dice);
        }
        damage = System.Math.Max(1, damage);

        int actualDamage = targetHealth.TakeDamage(damage, DamageType, caster);

        // Heal caster for half damage dealt (rounded down)
        int healing = actualDamage / 2;
        int actualHealing = 0;

        if (healing > 0 && caster != null)
        {
            var casterHealth = caster.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (casterHealth != null)
            {
                int oldHealth = casterHealth.CurrentHealth;
                casterHealth.Heal(healing);
                actualHealing = casterHealth.CurrentHealth - oldHealth;
            }
        }

        string message;
        if (actualHealing > 0)
        {
            message = $"{target.DisplayName} takes {actualDamage} damage! {caster?.DisplayName ?? "You"} drain {actualHealing} life.";
        }
        else
        {
            message = $"{target.DisplayName} takes {actualDamage} damage!";
        }

        var result = EffectResult.CreateSuccess(message, Palette.ToHex(Palette.Crimson), target);
        result.DamageDealt = actualDamage;
        return result;
    }
}
