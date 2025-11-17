using System;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

/// <summary>
/// Status that temporarily increases an entity's armor value.
/// </summary>
public class ArmorBuffStatus : Status
{
    /// <summary>
    /// Amount of armor bonus provided by this status.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Unique source identifier for this status instance.
    /// Used to track the modifier in StatsComponent.
    /// </summary>
    private string? _sourceId;

    public override string Name => "Armor Buff";

    public override string TypeId => "armor_buff";

    /// <summary>
    /// Parameterless constructor with default values.
    /// </summary>
    public ArmorBuffStatus()
    {
        Amount = 1;
        Duration = 10;
    }

    /// <summary>
    /// Parameterized constructor for creating specific armor buffs.
    /// </summary>
    public ArmorBuffStatus(int amount, int duration)
    {
        Amount = amount;
        Duration = duration;
    }

    public override string OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"ArmorBuffStatus: {target.DisplayName} has no StatsComponent");
            return string.Empty;
        }

        // Create unique source ID for this status instance
        _sourceId = $"status_armor_buff_{Guid.NewGuid()}";

        // Add armor modifier
        stats.AddArmorSource(_sourceId, Amount);

        return $"{target.DisplayName}'s skin hardens like bark! (+{Amount} Armor for {Duration} turns)";
    }

    public override string OnRemoved(BaseEntity target)
    {
        if (string.IsNullOrEmpty(_sourceId))
        {
            return string.Empty;
        }

        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            return string.Empty;
        }

        // Remove armor modifier
        stats.RemoveArmorSource(_sourceId);

        return $"{target.DisplayName}'s skin returns to normal.";
    }
}
