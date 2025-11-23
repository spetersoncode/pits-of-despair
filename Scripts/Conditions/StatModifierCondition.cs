using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Generic condition that modifies an entity's stats (Armor, Strength, Agility, Endurance, Will, or Evasion).
/// Uses a unified implementation with the StatsComponent's AddStatModifier/RemoveStatModifier API.
/// Supports all duration modes: Temporary (potions), WhileEquipped (rings), Permanent (passive skills).
/// </summary>
public class StatModifierCondition : Condition
{
    /// <summary>
    /// Which stat this condition modifies.
    /// </summary>
    public StatType Stat { get; set; }

    /// <summary>
    /// Amount to modify the stat by (positive for buffs, negative for penalties).
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Internal source ID used for modifier tracking.
    /// Generated from SourceId or auto-generated if not set.
    /// </summary>
    private string? _internalSourceId;

    public override string Name => $"{Stat} Modifier";

    public override string TypeId => $"{Stat.ToString().ToLower()}_modifier";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public StatModifierCondition()
    {
        Stat = StatType.Armor;
        Amount = 1;
        Duration = "10";
    }

    /// <summary>
    /// Parameterized constructor for creating stat modifiers.
    /// </summary>
    /// <param name="stat">The stat type to modify.</param>
    /// <param name="amount">The amount to modify the stat by (positive for buffs, negative for penalties).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking.</param>
    public StatModifierCondition(StatType stat, int amount, string duration,
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
            GD.PrintErr($"StatModifierCondition: {target.DisplayName} has no StatsComponent");
            return ConditionMessage.Empty;
        }

        // Use provided SourceId or generate unique one
        _internalSourceId = SourceId ?? $"condition_{TypeId}_{Guid.NewGuid()}";
        SourceId = _internalSourceId; // Store back for condition tracking

        // Apply modifier using unified API
        stats.AddStatModifier(Stat, _internalSourceId, Amount);

        // Generate appropriate message based on duration mode and amount sign
        string statName = Stat.ToString();
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => Amount >= 0 ? $"{statName} +{Amount}" : $"{statName} {Amount}",
            ConditionDuration.Permanent => Amount >= 0
                ? $"{statName} permanently increased by {Amount}!"
                : $"{statName} permanently decreased by {Math.Abs(Amount)}!",
            _ => Amount >= 0
                ? $"{statName} increased by {Amount}!"
                : $"{statName} decreased by {Math.Abs(Amount)}!"
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

        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            return ConditionMessage.Empty;
        }

        // Remove modifier using unified API
        stats.RemoveStatModifier(Stat, _internalSourceId);

        // Generate appropriate message based on duration mode
        string statName = Stat.ToString();
        string message = DurationMode switch
        {
            ConditionDuration.WhileEquipped => Amount >= 0
                ? $"{statName} bonus removed"
                : $"{statName} penalty removed",
            _ => Amount >= 0
                ? $"{statName} buff has worn off."
                : $"{statName} debuff has worn off."
        };

        return new ConditionMessage(message, Palette.ToHex(Palette.Default));
    }
}
