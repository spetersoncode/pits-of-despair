using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that modifies an entity's regeneration rate.
/// DCSS formula: +80 per instance adds roughly 1 HP per 1.25 turns.
/// Supports all duration modes: Temporary (potions), WhileEquipped (rings), Permanent (passive skills).
/// </summary>
public class RegenCondition : Condition
{
    /// <summary>
    /// Amount of regen bonus provided by this condition.
    /// Default is 80 (DCSS Ring of Regeneration value).
    /// </summary>
    public int Amount { get; set; } = 80;

    /// <summary>
    /// Internal source ID used for modifier tracking.
    /// Generated from SourceId or auto-generated if not set.
    /// </summary>
    private string? _internalSourceId;

    public override string Name => "Regeneration";

    public override string TypeId => "regen_modifier";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public RegenCondition()
    {
        Amount = 80;
        Duration = "10";
    }

    /// <summary>
    /// Parameterized constructor for creating regen conditions.
    /// </summary>
    /// <param name="amount">Regen rate bonus (default 80 per DCSS).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking.</param>
    public RegenCondition(int amount, string duration,
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
            GD.PrintErr($"RegenCondition: {target.DisplayName} has no HealthComponent");
            return ConditionMessage.Empty;
        }

        // Use provided SourceId or generate unique one
        _internalSourceId = SourceId ?? $"condition_{TypeId}_{Guid.NewGuid()}";
        SourceId = _internalSourceId; // Store back for condition tracking

        // Apply regen modifier
        health.AddRegenModifier(_internalSourceId, Amount);

        // Generate appropriate message based on duration mode
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => $"Regen +{Amount}",
            ConditionDuration.Permanent => $"Regeneration permanently increased!",
            _ => $"Regeneration increased!"
        };

        return new ConditionMessage(message, Palette.ToHex(Palette.StatusBuff));
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

        // Remove regen modifier
        health.RemoveRegenModifier(_internalSourceId);

        // Generate appropriate message based on duration mode
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => "Regen bonus removed",
            _ => "Regeneration buff has worn off."
        };

        return new ConditionMessage(message, Palette.ToHex(Palette.Default));
    }
}
