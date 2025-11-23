using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Generic condition that modifies an entity's stats (Armor, Strength, Agility, Endurance, or Evasion).
/// Uses a unified implementation to eliminate code duplication across stat-specific buff classes.
/// Supports all duration modes: Temporary (potions), WhileEquipped (rings), Permanent (passive skills).
/// </summary>
public class StatBuffCondition : Condition
{
    /// <summary>
    /// Which stat this condition buffs.
    /// </summary>
    public StatType Stat { get; set; }

    /// <summary>
    /// Amount of stat bonus provided by this condition.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Internal source ID used for modifier tracking.
    /// Generated from SourceId or auto-generated if not set.
    /// </summary>
    private string? _internalSourceId;

    public override string Name => Stat == StatType.Evasion ? "Evasion Buff" : $"{Stat} Buff";

    public override string TypeId => $"{Stat.ToString().ToLower()}_buff";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public StatBuffCondition()
    {
        Stat = StatType.Armor;
        Amount = 1;
        Duration = "10";
    }

    /// <summary>
    /// Parameterized constructor for creating specific stat buffs.
    /// </summary>
    /// <param name="stat">The stat type to buff.</param>
    /// <param name="amount">The amount to increase the stat by.</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking.</param>
    public StatBuffCondition(StatType stat, int amount, string duration,
        ConditionDuration durationMode = ConditionDuration.Temporary, string? sourceId = null)
    {
        Stat = stat;
        Amount = amount;
        Duration = duration;
        DurationMode = durationMode;
        SourceId = sourceId;
    }

    public override ConditionMessage OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"StatBuffCondition: {target.DisplayName} has no StatsComponent");
            return ConditionMessage.Empty;
        }

        // Use provided SourceId or generate unique one
        _internalSourceId = SourceId ?? $"condition_{TypeId}_{Guid.NewGuid()}";
        SourceId = _internalSourceId; // Store back for condition tracking

        // Apply modifier based on stat type
        switch (Stat)
        {
            case StatType.Armor:
                stats.AddArmorSource(_internalSourceId, Amount);
                break;
            case StatType.Strength:
                stats.AddStrengthModifier(_internalSourceId, Amount);
                break;
            case StatType.Agility:
                stats.AddAgilityModifier(_internalSourceId, Amount);
                break;
            case StatType.Endurance:
                stats.AddEnduranceModifier(_internalSourceId, Amount);
                break;
            case StatType.Evasion:
                // Positive evasion bonus (stored as negative penalty to add to evasion)
                stats.AddEvasionPenaltySource(_internalSourceId, Amount);
                break;
        }

        // Generate appropriate message based on duration mode
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => $"{GetStatDisplayName()} +{Amount}",
            ConditionDuration.Permanent => $"{GetStatDisplayName()} permanently increased by {Amount}!",
            _ => $"{GetStatDisplayName()} increased by {Amount}!"
        };

        return new ConditionMessage(message, Palette.ToHex(Palette.StatusBuff));
    }

    private string GetStatDisplayName() => Stat switch
    {
        StatType.Evasion => "Evasion",
        _ => Stat.ToString()
    };

    public override ConditionMessage OnRemoved(BaseEntity target)
    {
        if (string.IsNullOrEmpty(_internalSourceId))
        {
            return ConditionMessage.Empty;
        }

        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            return ConditionMessage.Empty;
        }

        // Remove modifier based on stat type
        switch (Stat)
        {
            case StatType.Armor:
                stats.RemoveArmorSource(_internalSourceId);
                break;
            case StatType.Strength:
                stats.RemoveStrengthModifier(_internalSourceId);
                break;
            case StatType.Agility:
                stats.RemoveAgilityModifier(_internalSourceId);
                break;
            case StatType.Endurance:
                stats.RemoveEnduranceModifier(_internalSourceId);
                break;
            case StatType.Evasion:
                stats.RemoveEvasionPenaltySource(_internalSourceId);
                break;
        }

        // Generate appropriate message based on duration mode
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => $"{GetStatDisplayName()} bonus removed",
            _ => $"{GetStatDisplayName()} buff has worn off."
        };

        return new ConditionMessage(message, Palette.ToHex(Palette.Default));
    }
}
