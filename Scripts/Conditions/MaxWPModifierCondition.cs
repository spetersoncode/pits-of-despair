using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that modifies an entity's maximum willpower.
/// Supports all duration modes: Temporary (potions), WhileEquipped (items), Permanent (passive skills).
/// </summary>
public class MaxWPModifierCondition : Condition
{
    /// <summary>
    /// Amount of max WP bonus provided by this condition.
    /// Positive values increase max WP, negative values decrease it.
    /// </summary>
    public int Amount { get; set; } = 1;

    /// <summary>
    /// Internal source ID used for modifier tracking.
    /// Generated from SourceId or auto-generated if not set.
    /// </summary>
    private string? _internalSourceId;

    public override string Name => "Max WP Modifier";

    public override string TypeId => "max_wp_modifier";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public MaxWPModifierCondition()
    {
        Amount = 1;
        Duration = "10";
    }

    /// <summary>
    /// Parameterized constructor for creating max WP modifier conditions.
    /// </summary>
    /// <param name="amount">Max WP bonus (positive = increase, negative = decrease).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking.</param>
    public MaxWPModifierCondition(int amount, string duration,
        ConditionDuration durationMode = ConditionDuration.Temporary, string? sourceId = null)
    {
        Amount = amount;
        Duration = duration;
        DurationMode = durationMode;
        SourceId = sourceId;
    }

    public override ConditionMessage OnApplied(BaseEntity target)
    {
        var willpower = target.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        if (willpower == null)
        {
            GD.PrintErr($"MaxWPModifierCondition: {target.DisplayName} has no WillpowerComponent");
            return ConditionMessage.Empty;
        }

        // Use provided SourceId or generate unique one
        _internalSourceId = SourceId ?? $"condition_{TypeId}_{Guid.NewGuid()}";
        SourceId = _internalSourceId; // Store back for condition tracking

        // Apply max WP modifier
        willpower.AddMaxWPModifier(_internalSourceId, Amount);

        // Generate appropriate message based on duration mode and amount sign
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => Amount >= 0 ? $"Max WP +{Amount}" : $"Max WP {Amount}",
            ConditionDuration.Permanent => Amount >= 0
                ? $"Max WP permanently increased by {Amount}!"
                : $"Max WP permanently decreased by {Math.Abs(Amount)}!",
            _ => Amount >= 0
                ? $"Max WP increased by {Amount}!"
                : $"Max WP decreased by {Math.Abs(Amount)}!"
        };

        string color = Amount >= 0 ? Palette.ToHex(Palette.StatusBuff) : Palette.ToHex(Palette.StatusDebuff);
        return new ConditionMessage(message, color);
    }

    public override ConditionMessage OnRemoved(BaseEntity target)
    {
        if (string.IsNullOrEmpty(_internalSourceId))
        {
            return ConditionMessage.Empty;
        }

        var willpower = target.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        if (willpower == null)
        {
            return ConditionMessage.Empty;
        }

        // Remove max WP modifier
        willpower.RemoveMaxWPModifier(_internalSourceId);

        // Generate appropriate message based on duration mode
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => Amount >= 0
                ? "Max WP bonus removed"
                : "Max WP penalty removed",
            _ => Amount >= 0
                ? "Max WP buff has worn off."
                : "Max WP debuff has worn off."
        };

        return new ConditionMessage(message, Palette.ToHex(Palette.Default));
    }
}
