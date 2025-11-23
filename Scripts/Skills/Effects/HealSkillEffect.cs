using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Skills.Effects;

/// <summary>
/// Skill effect that heals the target.
/// Supports flat healing, dice-based healing, and stat scaling.
/// </summary>
public class HealSkillEffect : SkillEffect
{
    public override string Type => "heal";

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

    public HealSkillEffect() { }

    public HealSkillEffect(SkillEffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override SkillEffectResult Apply(BaseEntity target, SkillEffectContext context)
    {
        var targetName = target.DisplayName;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return SkillEffectResult.CreateFailure(
                $"{targetName} cannot be healed.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Check if already at full health
        if (healthComponent.CurrentHP >= healthComponent.MaxHP)
        {
            return SkillEffectResult.CreateFailure(
                $"{targetName} is already at full health.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate healing amount
        int healing = Amount;

        // Add dice roll if specified
        if (!string.IsNullOrEmpty(Dice))
        {
            healing += DiceRoller.Roll(Dice);
        }

        // Add stat scaling
        if (!string.IsNullOrEmpty(ScalingStat))
        {
            int statValue = context.GetCasterStat(ScalingStat);
            healing += (int)(statValue * ScalingMultiplier);
        }

        if (healing <= 0)
        {
            return SkillEffectResult.CreateFailure(
                $"{context.Skill.Name} has no effect.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply healing
        int oldHealth = healthComponent.CurrentHP;
        healthComponent.Heal(healing);
        int actualHealing = healthComponent.CurrentHP - oldHealth;

        return SkillEffectResult.CreateSuccess(
            $"{targetName} heals {actualHealing} HP.",
            Palette.ToHex(Palette.Success),
            target
        );
    }
}
