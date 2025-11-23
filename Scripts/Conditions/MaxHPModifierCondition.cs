using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that modifies an entity's maximum hit points.
/// Supports all duration modes: Temporary (potions), WhileEquipped (items), Permanent (passive skills).
/// </summary>
public class MaxHPModifierCondition : Condition
{
    /// <summary>
    /// Amount of max HP bonus provided by this condition.
    /// Positive values increase max HP, negative values decrease it.
    /// </summary>
    public int Amount { get; set; } = 1;

    /// <summary>
    /// Internal source ID used for modifier tracking.
    /// Generated from SourceId or auto-generated if not set.
    /// </summary>
    private string? _internalSourceId;

    public override string Name => "Max HP Modifier";

    public override string TypeId => "max_hp_modifier";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public MaxHPModifierCondition()
    {
        Amount = 1;
        Duration = "10";
    }

    /// <summary>
    /// Parameterized constructor for creating max HP modifier conditions.
    /// </summary>
    /// <param name="amount">Max HP bonus (positive = increase, negative = decrease).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking.</param>
    public MaxHPModifierCondition(int amount, string duration,
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
            GD.PrintErr($"MaxHPModifierCondition: {target.DisplayName} has no HealthComponent");
            return ConditionMessage.Empty;
        }

        // Use provided SourceId or generate unique one
        _internalSourceId = SourceId ?? $"condition_{TypeId}_{Guid.NewGuid()}";
        SourceId = _internalSourceId; // Store back for condition tracking

        // Apply max HP modifier
        health.AddMaxHPModifier(_internalSourceId, Amount);

        // Generate appropriate message based on duration mode and amount sign
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => Amount >= 0 ? $"Max HP +{Amount}" : $"Max HP {Amount}",
            ConditionDuration.Permanent => Amount >= 0
                ? $"Max HP permanently increased by {Amount}!"
                : $"Max HP permanently decreased by {Math.Abs(Amount)}!",
            _ => Amount >= 0
                ? $"Max HP increased by {Amount}!"
                : $"Max HP decreased by {Math.Abs(Amount)}!"
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

        // Remove max HP modifier
        health.RemoveMaxHPModifier(_internalSourceId);

        // Generate appropriate message based on duration mode
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => Amount >= 0
                ? "Max HP bonus removed"
                : "Max HP penalty removed",
            _ => Amount >= 0
                ? "Max HP buff has worn off."
                : "Max HP debuff has worn off."
        };

        return new ConditionMessage(message, Palette.ToHex(Palette.Default));
    }
}
