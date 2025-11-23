using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Skills.Effects;

/// <summary>
/// Skill effect that deals damage to the target.
/// Supports flat damage, dice-based damage, and stat scaling.
/// </summary>
public class DamageSkillEffect : SkillEffect
{
    public override string Type => "damage";

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

    public DamageSkillEffect() { }

    public DamageSkillEffect(SkillEffectDefinition definition)
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
                $"{targetName} cannot be damaged.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate base damage
        int damage = Amount;

        // Add dice roll if specified
        if (!string.IsNullOrEmpty(Dice))
        {
            damage += DiceRoller.Roll(Dice);
        }

        // Add stat scaling
        if (!string.IsNullOrEmpty(ScalingStat))
        {
            int statValue = context.GetCasterStat(ScalingStat);
            damage += (int)(statValue * ScalingMultiplier);
        }

        // Ensure minimum of 1 damage if we're supposed to deal damage
        if (damage < 1 && (Amount > 0 || !string.IsNullOrEmpty(Dice)))
        {
            damage = 1;
        }

        if (damage <= 0)
        {
            return SkillEffectResult.CreateFailure(
                $"{context.Skill.Name} has no effect on {targetName}.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply damage with caster as source for kill attribution
        int actualDamage = healthComponent.TakeDamage(damage, DamageType, context.Caster);

        string message = $"{targetName} takes {actualDamage} damage!";

        return SkillEffectResult.CreateSuccess(
            message,
            Palette.ToHex(Palette.CombatDamage),
            target
        );
    }
}
