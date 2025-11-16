using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System.Collections.Generic;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that teleports the target to a random valid position within range.
/// Uses Chebyshev distance (diagonal-friendly) for range calculation.
/// </summary>
public class BlinkEffect : Effect
{
    /// <summary>
    /// Maximum distance to teleport (in Chebyshev distance).
    /// </summary>
    public int Range { get; set; }

    public override string Name => "Blink";

    public BlinkEffect()
    {
        Range = 5;
    }

    public BlinkEffect(int range)
    {
        // Default to 5 if not specified or invalid
        Range = range > 0 ? range : 5;
    }

    public override EffectResult Apply(BaseEntity target, ActionContext context)
    {
        var name = target.DisplayName;
        var currentPos = target.GridPosition;

        // Find all valid positions within range
        var validPositions = new List<GridPosition>();

        // Check all positions in a square around the target
        for (int dx = -Range; dx <= Range; dx++)
        {
            for (int dy = -Range; dy <= Range; dy++)
            {
                // Skip current position
                if (dx == 0 && dy == 0)
                    continue;

                var checkPos = new GridPosition(currentPos.X + dx, currentPos.Y + dy);

                // Check if position is within range using Chebyshev distance
                if (DistanceHelper.ChebyshevDistance(currentPos, checkPos) > Range)
                    continue;

                // Check if position is valid (walkable, unoccupied, in bounds)
                if (!context.MapSystem.IsWalkable(checkPos))
                    continue;

                if (context.EntityManager.IsPositionOccupied(checkPos))
                    continue;

                // This position is valid!
                validPositions.Add(checkPos);
            }
        }

        // If no valid positions found, fail (but still consume scroll)
        if (validPositions.Count == 0)
        {
            return new EffectResult(
                false,
                $"{name} tries to blink, but the magic fizzles!",
                "#888888"
            );
        }

        // Pick a random valid position
        int randomIndex = GD.RandRange(0, validPositions.Count - 1);
        var newPos = validPositions[randomIndex];

        // Teleport the target
        target.SetGridPosition(newPos);

        return new EffectResult(
            true,
            $"{name} blinks to a new location!",
            "#00DDFF"  // Cyan for magic/teleportation
        );
    }
}
