using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for cleave attacks.
/// Targets an adjacent tile and affects a 3-tile arc (target + 2 neighbors also adjacent to caster).
/// </summary>
public class CleaveTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Cleave;

    private static readonly (int dx, int dy)[] AdjacentOffsets = new[]
    {
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1)
    };

    // Clockwise order for arc calculation
    private static readonly (int dx, int dy)[] ClockwiseOffsets = new[]
    {
        (0, -1),   // N  (index 0)
        (1, -1),   // NE (index 1)
        (1, 0),    // E  (index 2)
        (1, 1),    // SE (index 3)
        (0, 1),    // S  (index 4)
        (-1, 1),   // SW (index 5)
        (-1, 0),   // W  (index 6)
        (-1, -1)   // NW (index 7)
    };

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        TargetingDefinition definition,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;

        // All 8 adjacent tiles are valid - free cursor movement
        foreach (var (dx, dy) in AdjacentOffsets)
        {
            validPositions.Add(new GridPosition(casterPos.X + dx, casterPos.Y + dy));
        }

        return validPositions;
    }

    public override bool IsValidTarget(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        var casterPos = caster.GridPosition;

        // Must be adjacent (Chebyshev distance of 1) - allows free movement within adjacent tiles
        return DistanceHelper.ChebyshevDistance(casterPos, targetPosition) == 1;
    }

    /// <summary>
    /// Returns the 3-tile cleave arc: target position + clockwise and counter-clockwise neighbors.
    /// </summary>
    public override List<GridPosition> GetAffectedPositions(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        var positions = new List<GridPosition> { targetPosition };
        var casterPos = caster.GridPosition;

        // Find the direction index of the target relative to caster
        var targetOffset = (targetPosition.X - casterPos.X, targetPosition.Y - casterPos.Y);
        int targetIndex = -1;
        for (int i = 0; i < ClockwiseOffsets.Length; i++)
        {
            if (ClockwiseOffsets[i] == targetOffset)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1)
            return positions; // Not adjacent, shouldn't happen

        // Add clockwise neighbor (index + 1)
        int cwIndex = (targetIndex + 1) % 8;
        var cwOffset = ClockwiseOffsets[cwIndex];
        positions.Add(new GridPosition(casterPos.X + cwOffset.dx, casterPos.Y + cwOffset.dy));

        // Add counter-clockwise neighbor (index - 1)
        int ccwIndex = (targetIndex + 7) % 8; // +7 is same as -1 mod 8
        var ccwOffset = ClockwiseOffsets[ccwIndex];
        positions.Add(new GridPosition(casterPos.X + ccwOffset.dx, casterPos.Y + ccwOffset.dy));

        return positions;
    }

    /// <summary>
    /// Returns all entities in the cleave arc.
    /// </summary>
    public override List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        var entities = new List<BaseEntity>();

        foreach (var pos in GetAffectedPositions(caster, targetPosition, definition, context))
        {
            var entity = context.EntityManager.GetEntityAtPosition(pos);
            if (entity != null && entity != caster && !entities.Contains(entity))
            {
                entities.Add(entity);
            }
        }

        return entities;
    }
}
