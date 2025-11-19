using System;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

/// <summary>
/// Generic status that temporarily increases an entity's stats (Armor, Strength, Agility, or Endurance).
/// Uses a unified implementation to eliminate code duplication across stat-specific buff classes.
/// </summary>
public class StatBuffStatus : Status
{
    /// <summary>
    /// Which stat this status buffs.
    /// </summary>
    public StatType Stat { get; set; }

    /// <summary>
    /// Amount of stat bonus provided by this status.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Unique source identifier for this status instance.
    /// Used to track the modifier in StatsComponent.
    /// </summary>
    private string? _sourceId;

    public override string Name => $"{Stat} Buff";

    public override string TypeId => $"{Stat.ToString().ToLower()}_buff";

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public StatBuffStatus()
    {
        Stat = StatType.Armor;
        Amount = 1;
        Duration = 10;
    }

    /// <summary>
    /// Parameterized constructor for creating specific stat buffs.
    /// </summary>
    public StatBuffStatus(StatType stat, int amount, int duration)
    {
        Stat = stat;
        Amount = amount;
        Duration = duration;
    }

    public override StatusMessage OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"StatBuffStatus: {target.DisplayName} has no StatsComponent");
            return StatusMessage.Empty;
        }

        // Create unique source ID for this status instance
        _sourceId = $"status_{TypeId}_{Guid.NewGuid()}";

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

        return new StatusMessage(
            $"{Stat} increased by {Amount}!",
            Palette.ToHex(Palette.StatusBuff)
        );
    }

    public override StatusMessage OnRemoved(BaseEntity target)
    {
        if (string.IsNullOrEmpty(_sourceId))
        {
            return StatusMessage.Empty;
        }

        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            return StatusMessage.Empty;
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

        return new StatusMessage(
            $"{Stat} buff has worn off.",
            Palette.ToHex(Palette.Default)
        );
    }
}
