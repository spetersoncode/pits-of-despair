using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Conditions;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that applies a condition (buff/debuff) to the target entity.
/// </summary>
public class ApplyConditionEffect : Effect
{
    /// <summary>
    /// The type of condition to apply (e.g., "armor_buff", "poison", "paralyze", "confusion").
    /// </summary>
    public string ConditionType { get; set; }

    /// <summary>
    /// The amount/magnitude of the condition effect.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Duration as dice notation (e.g., "10", "2d3").
    /// Resolved by the Condition when applied.
    /// </summary>
    public string Duration { get; set; }

    public override string Name => "Apply Condition";

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

    public override EffectResult Apply(BaseEntity target, ActionContext context)
    {
        var name = target.DisplayName;
        var conditionComponent = target.GetNodeOrNull<ConditionComponent>("ConditionComponent");

        if (conditionComponent == null)
        {
            return new EffectResult(
                false,
                $"{name} cannot receive conditions.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Create the appropriate condition based on type
        // Duration resolution is handled by the Condition itself via ConditionComponent
        var condition = CreateCondition(ConditionType, Amount, Duration);
        if (condition == null)
        {
            GD.PrintErr($"ApplyConditionEffect: Unknown condition type '{ConditionType}'");
            return new EffectResult(
                false,
                $"Failed to apply condition.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Add condition to target (message will be emitted via signal)
        conditionComponent.AddCondition(condition);

        // Return success (the actual message is emitted by ConditionComponent signal)
        return new EffectResult(
            true,
            string.Empty,
            Palette.ToHex(Palette.Success)
        );
    }

    /// <summary>
    /// Factory method to create Condition instances from type string.
    /// </summary>
    private Condition? CreateCondition(string conditionType, int amount, string duration)
    {
        switch (conditionType.ToLower())
        {
            case "armor_buff":
                return new StatBuffCondition(StatType.Armor, amount, duration);

            case "strength_buff":
                return new StatBuffCondition(StatType.Strength, amount, duration);

            case "agility_buff":
                return new StatBuffCondition(StatType.Agility, amount, duration);

            case "endurance_buff":
                return new StatBuffCondition(StatType.Endurance, amount, duration);

            case "confusion":
                return new ConfusionCondition(duration);

            default:
                return null;
        }
    }
}
