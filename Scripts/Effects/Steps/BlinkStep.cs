using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;
using System.Collections.Generic;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that teleports the target to a random valid position within range.
/// Uses Chebyshev distance (diagonal-friendly) for range calculation.
/// </summary>
public class BlinkStep : IEffectStep
{
    private readonly int _range;

    public BlinkStep(StepDefinition definition)
    {
        _range = definition.Range > 0 ? definition.Range : 5;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var currentPos = target.GridPosition;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        // Find all valid positions within range
        var validPositions = new List<GridPosition>();

        for (int dx = -_range; dx <= _range; dx++)
        {
            for (int dy = -_range; dy <= _range; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var checkPos = new GridPosition(currentPos.X + dx, currentPos.Y + dy);

                if (DistanceHelper.ChebyshevDistance(currentPos, checkPos) > _range)
                    continue;

                if (!mapSystem.IsWalkable(checkPos))
                    continue;

                if (entityManager.IsPositionOccupied(checkPos))
                    continue;

                validPositions.Add(checkPos);
            }
        }

        if (validPositions.Count == 0)
        {
            messages.Add($"{target.DisplayName} tries to blink, but the magic fizzles!", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Pick a random valid position
        int randomIndex = GD.RandRange(0, validPositions.Count - 1);
        var newPos = validPositions[randomIndex];

        // Teleport the target
        target.SetGridPosition(newPos);

        messages.Add($"{target.DisplayName} blinks to a new location!", Palette.ToHex(Palette.ScrollBlink));
        state.Success = true;
    }
}
