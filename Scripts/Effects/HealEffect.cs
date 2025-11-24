using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that heals the target entity.
/// Supports flat healing, dice-based healing, and stat scaling.
/// </summary>
public class HealEffect : Effect
{
    public override string Type => "heal";
    public override string Name => "Heal";

    /// <summary>
    /// Flat healing amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable healing (e.g., "2d6").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Stat to scale healing with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    public HealEffect()
    {
        Amount = 0;
    }

    /// <summary>
    /// Creates a simple heal effect with a fixed amount.
    /// </summary>
    public HealEffect(int amount)
    {
        Amount = amount;
    }

    /// <summary>
    /// Creates a heal effect from a unified effect definition.
    /// Supports dice and stat scaling from skills.
    /// </summary>
    public HealEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} cannot be healed.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Check if already at full health
        if (healthComponent.CurrentHealth >= healthComponent.MaxHealth)
        {
            return EffectResult.CreateFailure(
                $"{targetName} is already at full health.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate healing amount with dice and scaling
        int healing = CalculateScaledAmount(Amount, Dice, ScalingStat, ScalingMultiplier, context);

        if (healing <= 0)
        {
            return EffectResult.CreateFailure(
                $"{context.GetSourceName()} has no effect.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply healing
        int oldHealth = healthComponent.CurrentHealth;
        healthComponent.Heal(healing);
        int actualHealing = healthComponent.CurrentHealth - oldHealth;

        return EffectResult.CreateSuccess(
            $"{targetName} heals {actualHealing} Health.",
            Palette.ToHex(Palette.Success),
            target
        );
    }
}
