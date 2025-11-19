using System;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

/// <summary>
/// Status that temporarily increases an entity's agility.
/// </summary>
public class AgilityBuffStatus : Status
{
    /// <summary>
    /// Amount of agility bonus provided by this status.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Unique source identifier for this status instance.
    /// Used to track the modifier in StatsComponent.
    /// </summary>
    private string? _sourceId;

    public override string Name => "Agility Buff";

    public override string TypeId => "agility_buff";

    /// <summary>
    /// Parameterless constructor with default values.
    /// </summary>
    public AgilityBuffStatus()
    {
        Amount = 1;
        Duration = 10;
    }

    /// <summary>
    /// Parameterized constructor for creating specific agility buffs.
    /// </summary>
    public AgilityBuffStatus(int amount, int duration)
    {
        Amount = amount;
        Duration = duration;
    }

    public override StatusMessage OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"AgilityBuffStatus: {target.DisplayName} has no StatsComponent");
            return StatusMessage.Empty;
        }

        // Create unique source ID for this status instance
        _sourceId = $"status_agility_buff_{Guid.NewGuid()}";

        // Add agility modifier
        stats.AddAgilityModifier(_sourceId, Amount);

        return new StatusMessage(
            $"{target.DisplayName} moves with feline grace! (+{Amount} AGI for {Duration} turns)",
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

        // Remove agility modifier
        stats.RemoveAgilityModifier(_sourceId);

        return new StatusMessage(
            $"{target.DisplayName}'s agility returns to normal.",
            Palette.ToHex(Palette.Default)
        );
    }
}
