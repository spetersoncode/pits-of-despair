using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Generic condition that temporarily increases an entity's stats (Armor, Strength, Agility, or Endurance).
/// Uses a unified implementation to eliminate code duplication across stat-specific buff classes.
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
    /// Unique source identifier for this condition instance.
    /// Used to track the modifier in StatsComponent.
    /// </summary>
    private string? _sourceId;

    public override string Name => $"{Stat} Buff";

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
    public StatBuffCondition(StatType stat, int amount, string duration)
    {
        Stat = stat;
        Amount = amount;
        Duration = duration;
    }

    public override ConditionMessage OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"StatBuffCondition: {target.DisplayName} has no StatsComponent");
            return ConditionMessage.Empty;
        }

        // Create unique source ID for this condition instance
        _sourceId = $"condition_{TypeId}_{Guid.NewGuid()}";

        // Apply modifier based on stat type
        switch (Stat)
        {
            case StatType.Armor:
                stats.AddArmorSource(_sourceId, Amount);
                break;
            case StatType.Strength:
                stats.AddStrengthModifier(_sourceId, Amount);
                break;
            case StatType.Agility:
                stats.AddAgilityModifier(_sourceId, Amount);
                break;
            case StatType.Endurance:
                stats.AddEnduranceModifier(_sourceId, Amount);
                break;
        }

        return new ConditionMessage(
            $"{Stat} increased by {Amount}!",
            Palette.ToHex(Palette.StatusBuff)
        );
    }

    public override ConditionMessage OnRemoved(BaseEntity target)
    {
        if (string.IsNullOrEmpty(_sourceId))
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
                stats.RemoveArmorSource(_sourceId);
                break;
            case StatType.Strength:
                stats.RemoveStrengthModifier(_sourceId);
                break;
            case StatType.Agility:
                stats.RemoveAgilityModifier(_sourceId);
                break;
            case StatType.Endurance:
                stats.RemoveEnduranceModifier(_sourceId);
                break;
        }

        return new ConditionMessage(
            $"{Stat} buff has worn off.",
            Palette.ToHex(Palette.Default)
        );
    }
}
