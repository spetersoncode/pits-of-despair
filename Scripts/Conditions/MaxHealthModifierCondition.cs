using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that modifies an entity's maximum health.
/// Supports all duration modes: Temporary (potions), WhileEquipped (items), Permanent (passive skills).
/// </summary>
public class MaxHealthModifierCondition : Condition
{
    /// <summary>
    /// Amount of max Health bonus provided by this condition.
    /// Positive values increase max Health, negative values decrease it.
    /// </summary>
    public int Amount { get; set; } = 1;

    /// <summary>
    /// Internal source ID used for modifier tracking.
    /// Generated from SourceId or auto-generated if not set.
    /// </summary>
    private string? _internalSourceId;

    public override string Name => "Max Health Modifier";

    public override string TypeId => "max_health_modifier";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public MaxHealthModifierCondition()
    {
        Amount = 1;
        Duration = "10";
    }

    /// <summary>
    /// Parameterized constructor for creating max Health modifier conditions.
    /// </summary>
    /// <param name="amount">Max Health bonus (positive = increase, negative = decrease).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking.</param>
    public MaxHealthModifierCondition(int amount, string duration,
        ConditionDuration durationMode = ConditionDuration.Temporary, string? sourceId = null)
    {
        Amount = amount;
        Duration = duration;
        DurationMode = durationMode;
        SourceId = sourceId;
    }

    public override ConditionMessage OnApplied(BaseEntity target)
    {
        var health = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null)
        {
            GD.PrintErr($"MaxHealthModifierCondition: {target.DisplayName} has no HealthComponent");
            return ConditionMessage.Empty;
        }

        // Use provided SourceId or generate unique one
        _internalSourceId = SourceId ?? $"condition_{TypeId}_{Guid.NewGuid()}";
        SourceId = _internalSourceId; // Store back for condition tracking

        // Apply max Health modifier
        health.AddMaxHealthModifier(_internalSourceId, Amount);

        // Equipment bonuses don't need messages - they're shown in UI
        if (DurationMode == ConditionDuration.WhileEquipped)
        {
            return ConditionMessage.Empty;
        }

        // Generate appropriate message based on duration mode and amount sign
        string message = DurationMode switch
        {
            ConditionDuration.Permanent => Amount >= 0
                ? $"Max Health permanently increased by {Amount}!"
                : $"Max Health permanently decreased by {Math.Abs(Amount)}!",
            _ => Amount >= 0
                ? $"Max Health increased by {Amount}!"
                : $"Max Health decreased by {Math.Abs(Amount)}!"
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

        var health = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null)
        {
            return ConditionMessage.Empty;
        }

        // Remove max Health modifier
        health.RemoveMaxHealthModifier(_internalSourceId);

        // Equipment bonuses don't need messages - they're shown in UI
        if (DurationMode == ConditionDuration.WhileEquipped)
        {
            return ConditionMessage.Empty;
        }

        // Generate appropriate message based on duration mode
        string message = Amount >= 0
            ? "Max Health buff has worn off."
            : "Max Health debuff has worn off.";

        return new ConditionMessage(message, Palette.ToHex(Palette.Default));
    }
}
