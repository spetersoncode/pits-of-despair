using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Skills.Effects;

/// <summary>
/// Skill effect that applies a condition (buff/debuff) to the target.
/// </summary>
public class ApplyConditionSkillEffect : SkillEffect
{
    public override string Type => "apply_condition";

    /// <summary>
    /// The type of condition to apply (e.g., "armor_buff", "confusion").
    /// </summary>
    public string? ConditionType { get; set; }

    /// <summary>
    /// The amount/magnitude of the condition effect.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Duration in turns (or dice notation).
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Stat to scale amount with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    public ApplyConditionSkillEffect() { }

    public ApplyConditionSkillEffect(SkillEffectDefinition definition)
    {
        ConditionType = definition.ConditionType;
        Amount = definition.Amount;
        Duration = definition.Duration;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override SkillEffectResult Apply(BaseEntity target, SkillEffectContext context)
    {
        var targetName = target.DisplayName;
        var conditionComponent = target.GetNodeOrNull<ConditionComponent>("ConditionComponent");

        if (conditionComponent == null)
        {
            return SkillEffectResult.CreateFailure(
                $"{targetName} cannot receive conditions.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate amount with scaling
        int finalAmount = Amount;
        if (!string.IsNullOrEmpty(ScalingStat))
        {
            int statValue = context.GetCasterStat(ScalingStat);
            finalAmount += (int)(statValue * ScalingMultiplier);
        }

        // Create the condition
        var condition = CreateCondition(ConditionType, finalAmount, Duration.ToString());
        if (condition == null)
        {
            GD.PrintErr($"ApplyConditionSkillEffect: Unknown condition type '{ConditionType}'");
            return SkillEffectResult.CreateFailure(
                $"Failed to apply condition.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Add condition to target (message will be emitted via signal)
        conditionComponent.AddCondition(condition);

        // Return success - the actual message is emitted by ConditionComponent signal
        return SkillEffectResult.CreateSuccess(
            string.Empty,
            Palette.ToHex(Palette.StatusBuff),
            target
        );
    }

    /// <summary>
    /// Factory method to create Condition instances from type string.
    /// </summary>
    private Condition? CreateCondition(string? conditionType, int amount, string duration)
    {
        if (string.IsNullOrEmpty(conditionType))
            return null;

        return conditionType.ToLower() switch
        {
            "armor_buff" => new StatBuffCondition(StatType.Armor, amount, duration),
            "strength_buff" => new StatBuffCondition(StatType.Strength, amount, duration),
            "agility_buff" => new StatBuffCondition(StatType.Agility, amount, duration),
            "endurance_buff" => new StatBuffCondition(StatType.Endurance, amount, duration),
            "confusion" => new ConfusionCondition(duration),
            _ => null
        };
    }
}
