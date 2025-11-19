using System;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

/// <summary>
/// Status that temporarily increases an entity's strength.
/// </summary>
public class StrengthBuffStatus : Status
{
    /// <summary>
    /// Amount of strength bonus provided by this status.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Unique source identifier for this status instance.
    /// Used to track the modifier in StatsComponent.
    /// </summary>
    private string? _sourceId;

    public override string Name => "Strength Buff";

    public override string TypeId => "strength_buff";

    /// <summary>
    /// Parameterless constructor with default values.
    /// </summary>
    public StrengthBuffStatus()
    {
        Amount = 1;
        Duration = 10;
    }

    /// <summary>
    /// Parameterized constructor for creating specific strength buffs.
    /// </summary>
    public StrengthBuffStatus(int amount, int duration)
    {
        Amount = amount;
        Duration = duration;
    }

    public override StatusMessage OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"StrengthBuffStatus: {target.DisplayName} has no StatsComponent");
            return StatusMessage.Empty;
        }

        // Create unique source ID for this status instance
        _sourceId = $"status_strength_buff_{Guid.NewGuid()}";

        // Add strength modifier
        stats.AddStrengthModifier(_sourceId, Amount);

        return new StatusMessage(
            $"{target.DisplayName} feels mighty! (+{Amount} STR for {Duration} turns)",
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

        // Remove strength modifier
        stats.RemoveStrengthModifier(_sourceId);

        return new StatusMessage(
            $"{target.DisplayName}'s strength returns to normal.",
            Palette.ToHex(Palette.Default)
        );
    }
}
