using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Generic condition that modifies an entity's speed.
/// Positive amounts increase speed (haste), negative amounts decrease speed (slow).
/// Follows the same pattern as StatModifierCondition.
/// </summary>
public class SpeedModifierCondition : Condition
{
    /// <summary>
    /// Amount to modify speed by (positive for haste, negative for slow).
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Internal source ID used for modifier tracking.
    /// Generated from SourceId or auto-generated if not set.
    /// </summary>
    private string? _internalSourceId;

    public override string Name => Amount >= 0 ? "Haste" : "Slow";

    public override string TypeId => "speed_modifier";

    public override string? ExamineDescription => Amount >= 0 ? "hasted" : "slowed";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public SpeedModifierCondition()
    {
        Amount = 5;
        Duration = "10";
    }

    /// <summary>
    /// Parameterized constructor for creating speed modifiers.
    /// </summary>
    /// <param name="amount">The amount to modify speed by (positive for haste, negative for slow).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking.</param>
    public SpeedModifierCondition(int amount, string duration,
        ConditionDuration durationMode = ConditionDuration.Temporary, string? sourceId = null)
    {
        Amount = amount;
        Duration = duration;
        DurationMode = durationMode;
        SourceId = sourceId;
    }

    public override ConditionMessage OnApplied(BaseEntity target)
    {
        var speed = target.GetNodeOrNull<SpeedComponent>("SpeedComponent");
        if (speed == null)
        {
            GD.PrintErr($"SpeedModifierCondition: {target.DisplayName} has no SpeedComponent");
            return ConditionMessage.Empty;
        }

        // Use provided SourceId or generate unique one
        _internalSourceId = SourceId ?? $"condition_{TypeId}_{Guid.NewGuid()}";
        SourceId = _internalSourceId;

        // Apply modifier
        speed.AddSpeedModifier(_internalSourceId, Amount);

        // Equipment bonuses don't need messages - they're shown in UI
        if (DurationMode == ConditionDuration.WhileEquipped)
        {
            return ConditionMessage.Empty;
        }

        // Generate appropriate message based on duration mode and amount sign
        string message = DurationMode switch
        {
            ConditionDuration.Permanent => Amount >= 0
                ? $"Speed permanently increased by {Amount}!"
                : $"Speed permanently decreased by {Math.Abs(Amount)}!",
            _ => Amount >= 0
                ? "You feel yourself speed up!"
                : "You feel yourself slow down!"
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

        var speed = target.GetNodeOrNull<SpeedComponent>("SpeedComponent");
        if (speed == null)
        {
            return ConditionMessage.Empty;
        }

        // Remove modifier
        speed.RemoveSpeedModifier(_internalSourceId);

        // Equipment bonuses don't need messages - they're shown in UI
        if (DurationMode == ConditionDuration.WhileEquipped)
        {
            return ConditionMessage.Empty;
        }

        // Generate appropriate message based on duration mode
        string message = Amount >= 0
            ? "Your haste wears off."
            : "You no longer feel slowed.";

        return new ConditionMessage(message, Palette.ToHex(Palette.Default));
    }
}
