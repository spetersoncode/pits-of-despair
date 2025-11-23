using Godot;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that applies a condition (buff/debuff) to the target entity.
/// Supports stat scaling for amount calculation.
/// </summary>
public class ApplyConditionEffect : Effect
{
    public override string Type => "apply_condition";
    public override string Name => "Apply Condition";

    /// <summary>
    /// The type of condition to apply (e.g., "armor_buff", "poison", "paralyze", "confusion").
    /// </summary>
    public string? ConditionType { get; set; }

    /// <summary>
    /// The amount/magnitude of the condition effect.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Duration as dice notation (e.g., "10", "2d3").
    /// </summary>
    public string Duration { get; set; }

    /// <summary>
    /// Stat to scale amount with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    public ApplyConditionEffect()
    {
        ConditionType = string.Empty;
        Amount = 0;
        Duration = "1";
    }

    public ApplyConditionEffect(string conditionType, int amount, string duration)
    {
        ConditionType = conditionType;
        Amount = amount;
        Duration = duration;
    }

    /// <summary>
    /// Creates an apply condition effect from a unified effect definition.
    /// </summary>
    public ApplyConditionEffect(EffectDefinition definition)
    {
        ConditionType = definition.ConditionType;
        Amount = definition.Amount;
        Duration = definition.GetDurationString();
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;

        // Calculate amount with scaling
        int finalAmount = CalculateScaledAmount(Amount, null, ScalingStat, ScalingMultiplier, context);

        // Create condition using the factory
        var condition = ConditionFactory.Create(ConditionType, finalAmount, Duration);
        if (condition == null)
        {
            GD.PrintErr($"ApplyConditionEffect: Unknown condition type '{ConditionType}'");
            return EffectResult.CreateFailure(
                $"Failed to apply condition.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Add condition to target (conditions are now managed by BaseEntity directly)
        target.AddCondition(condition);

        // Return success - the actual message is emitted by BaseEntity's ConditionMessage signal
        return EffectResult.CreateSuccess(
            string.Empty,
            Palette.ToHex(Palette.StatusBuff),
            target
        );
    }
}
