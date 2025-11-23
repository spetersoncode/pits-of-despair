using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Skills.Effects;

/// <summary>
/// Skill effect that restores Willpower to the target.
/// </summary>
public class RestoreWillpowerEffect : SkillEffect
{
    public override string Type => "restore_willpower";

    /// <summary>
    /// Flat WP restoration amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable restoration (e.g., "1d6").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Stat to scale restoration with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    public RestoreWillpowerEffect() { }

    public RestoreWillpowerEffect(SkillEffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override SkillEffectResult Apply(BaseEntity target, SkillEffectContext context)
    {
        var targetName = target.DisplayName;
        var willpowerComponent = target.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");

        if (willpowerComponent == null)
        {
            return SkillEffectResult.CreateFailure(
                $"{targetName} has no Willpower.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Check if already at full WP
        if (willpowerComponent.CurrentWillpower >= willpowerComponent.MaxWillpower)
        {
            return SkillEffectResult.CreateFailure(
                $"{targetName} is already at full Willpower.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate restoration amount
        int restoration = Amount;

        // Add dice roll if specified
        if (!string.IsNullOrEmpty(Dice))
        {
            restoration += DiceRoller.Roll(Dice);
        }

        // Add stat scaling
        if (!string.IsNullOrEmpty(ScalingStat))
        {
            int statValue = context.GetCasterStat(ScalingStat);
            restoration += (int)(statValue * ScalingMultiplier);
        }

        if (restoration <= 0)
        {
            return SkillEffectResult.CreateFailure(
                $"{context.Skill.Name} has no effect.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply restoration
        int oldWP = willpowerComponent.CurrentWillpower;
        willpowerComponent.RestoreWillpower(restoration);
        int actualRestored = willpowerComponent.CurrentWillpower - oldWP;

        return SkillEffectResult.CreateSuccess(
            $"{targetName} restores {actualRestored} WP.",
            Palette.ToHex(Palette.StatusBuff),
            target
        );
    }
}
