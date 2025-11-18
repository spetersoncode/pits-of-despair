using System;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

/// <summary>
/// Status that temporarily increases an entity's endurance.
/// </summary>
public class EnduranceBuffStatus : Status
{
    /// <summary>
    /// Amount of endurance bonus provided by this status.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Unique source identifier for this status instance.
    /// Used to track the modifier in StatsComponent.
    /// </summary>
    private string? _sourceId;

    public override string Name => "Endurance Buff";

    public override string TypeId => "endurance_buff";

    /// <summary>
    /// Parameterless constructor with default values.
    /// </summary>
    public EnduranceBuffStatus()
    {
        Amount = 1;
        Duration = 10;
    }

    /// <summary>
    /// Parameterized constructor for creating specific endurance buffs.
    /// </summary>
    public EnduranceBuffStatus(int amount, int duration)
    {
        Amount = amount;
        Duration = duration;
    }

    public override string OnApplied(BaseEntity target)
    {
        var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (stats == null)
        {
            GD.PrintErr($"EnduranceBuffStatus: {target.DisplayName} has no StatsComponent");
            return string.Empty;
        }

        // Create unique source ID for this status instance
        _sourceId = $"status_endurance_buff_{Guid.NewGuid()}";

        // Add endurance modifier
        stats.AddEnduranceModifier(_sourceId, Amount);

        return $"{target.DisplayName} feels resilient! (+{Amount} END for {Duration} turns)";
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

        // Remove endurance modifier
        stats.RemoveEnduranceModifier(_sourceId);

        return $"{target.DisplayName}'s endurance returns to normal.";
    }
}
