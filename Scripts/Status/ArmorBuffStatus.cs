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

    public override StatusMessage OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"ArmorBuffStatus: {target.DisplayName} has no StatsComponent");
            return StatusMessage.Empty;
        }

        // Create unique source ID for this status instance
        _sourceId = $"status_armor_buff_{Guid.NewGuid()}";

        // Add armor modifier
        stats.AddArmorSource(_sourceId, Amount);

        return new StatusMessage(
            $"{target.DisplayName}'s skin hardens like bark! (+{Amount} Armor for {Duration} turns)",
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

        // Remove armor modifier
        stats.RemoveArmorSource(_sourceId);

        return new StatusMessage(
            $"{target.DisplayName}'s skin returns to normal.",
            Palette.ToHex(Palette.Default)
        );
    }
}
