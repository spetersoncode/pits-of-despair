using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;
using System.Collections.Generic;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that teleports the target to a random valid position within range.
/// Uses Chebyshev distance (diagonal-friendly) for range calculation.
/// Similar to TeleportEffect but always range-limited (short-range blink).
/// </summary>
public class BlinkEffect : Effect
{
    public override string Type => "blink";
    public override string Name => "Blink";

    /// <summary>
    /// Maximum distance to teleport (in Chebyshev distance).
    /// </summary>
    public int Range { get; set; }

    public BlinkEffect()
    {
        Range = 5;
    }

    public BlinkEffect(int range)
    {
        // Default to 5 if not specified or invalid
        Range = range > 0 ? range : 5;
    }

    /// <summary>
    /// Creates a blink effect from a unified effect definition.
    /// </summary>
    public BlinkEffect(EffectDefinition definition)
    {
        Range = definition.Range > 0 ? definition.Range : 5;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;
        var currentPos = target.GridPosition;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

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
                if (!mapSystem.IsWalkable(checkPos))
                    continue;

                if (entityManager.IsPositionOccupied(checkPos))
                    continue;

                // This position is valid!
                validPositions.Add(checkPos);
            }
        }

        // If no valid positions found, fail (but still consume scroll)
        if (validPositions.Count == 0)
        {
            return EffectResult.CreateFailure(
                $"{targetName} tries to blink, but the magic fizzles!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Pick a random valid position
        int randomIndex = GD.RandRange(0, validPositions.Count - 1);
        var newPos = validPositions[randomIndex];

        // Teleport the target
        target.SetGridPosition(newPos);

        return EffectResult.CreateSuccess(
            $"{targetName} blinks to a new location!",
            Palette.ToHex(Palette.ScrollBlink),
            target
        );
    }
}
