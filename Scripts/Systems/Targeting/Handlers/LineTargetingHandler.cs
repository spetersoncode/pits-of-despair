using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for line/beam targeting.
/// Player selects a target tile, effect affects all tiles in a line from caster to target.
/// Uses Bresenham's line algorithm for grid-based line calculation.
/// </summary>
public class LineTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Line;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        TargetingDefinition definition,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;
        int range = definition.Range > 0 ? definition.Range : 1;

        // Use FOV for LOS checking if required
        HashSet<GridPosition>? visibleTiles = null;
        if (definition.RequiresLOS)
        {
            visibleTiles = FOVCalculator.CalculateVisibleTiles(
                casterPos, range, context.MapSystem, definition.Metric);
        }

        // For line targeting, any tile in range can be a valid target
        // The line is calculated from caster to that target
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue; // Can't target self

                var checkPos = new GridPosition(casterPos.X + dx, casterPos.Y + dy);

                // Check range using the specified metric
                if (!DistanceHelper.IsInRange(casterPos, checkPos, range, definition.Metric))
                    continue;

                // Check LOS if required
                if (visibleTiles != null && !visibleTiles.Contains(checkPos))
                    continue;

                // For line targeting, we allow targeting any tile in range
                // (walls can be valid targets for effects like tunneling)
                if (context.MapSystem.IsInBounds(checkPos))
                {
                    validPositions.Add(checkPos);
                }
            }
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
        int range = definition.Range > 0 ? definition.Range : 1;

        // Can't target self
        if (targetPosition == casterPos)
            return false;

        // Check range
        if (!DistanceHelper.IsInRange(casterPos, targetPosition, range, definition.Metric))
            return false;

        // Check LOS if required
        if (definition.RequiresLOS)
        {
            var visibleTiles = FOVCalculator.CalculateVisibleTiles(
                casterPos, range, context.MapSystem, definition.Metric);
            if (!visibleTiles.Contains(targetPosition))
                return false;
        }

        return context.MapSystem.IsInBounds(targetPosition);
    }

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
            if (entity != null && !entities.Contains(entity))
            {
                entities.Add(entity);
            }
        }

        return entities;
    }

    public override List<GridPosition> GetAffectedPositions(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        var casterPos = caster.GridPosition;
        int range = definition.Range > 0 ? definition.Range : 1;

        // Calculate line from caster to target using Bresenham's algorithm
        return GetLinePositions(casterPos, targetPosition, range, context.MapSystem, definition.RequiresLOS);
    }

    /// <summary>
    /// Calculates all grid positions in a line from origin to target.
    /// Uses Bresenham's line algorithm for accurate grid-based line drawing.
    /// </summary>
    /// <param name="origin">Starting position (excluded from result)</param>
    /// <param name="target">Target position</param>
    /// <param name="maxRange">Maximum range in tiles</param>
    /// <param name="mapSystem">Map system for bounds checking</param>
    /// <param name="stopAtWalls">If true, stops the line when hitting a wall</param>
    /// <returns>List of grid positions along the line (excluding origin)</returns>
    public static List<GridPosition> GetLinePositions(
        GridPosition origin,
        GridPosition target,
        int maxRange,
        Systems.MapSystem mapSystem,
        bool stopAtWalls = true)
    {
        var positions = new List<GridPosition>();

        int x0 = origin.X;
        int y0 = origin.Y;
        int x1 = target.X;
        int y1 = target.Y;

        int dx = System.Math.Abs(x1 - x0);
        int dy = System.Math.Abs(y1 - y0);

        // Handle edge case: origin == target (no direction)
        // Use the target direction to extend the line to max range
        if (dx == 0 && dy == 0)
        {
            return positions; // No direction, return empty
        }

        int sx = x0 < x1 ? 1 : (x0 > x1 ? -1 : 0);
        int sy = y0 < y1 ? 1 : (y0 > y1 ? -1 : 0);
        int err = dx - dy;

        int currentX = x0;
        int currentY = y0;
        int tilesTraversed = 0;

        // Safety limit to prevent infinite loops
        int maxIterations = maxRange * 3;
        int iterations = 0;

        while (iterations++ < maxIterations)
        {
            int prevX = currentX;
            int prevY = currentY;

            // Bresenham step
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                currentX += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                currentY += sy;
            }

            // Safety check: ensure we moved
            if (currentX == prevX && currentY == prevY)
            {
                // Force movement in the dominant direction
                if (dx >= dy && sx != 0)
                    currentX += sx;
                else if (sy != 0)
                    currentY += sy;
                else
                    break; // Can't move, exit
            }

            var pos = new GridPosition(currentX, currentY);

            // Check bounds
            if (!mapSystem.IsInBounds(pos))
                break;

            // Check range
            tilesTraversed++;
            if (tilesTraversed > maxRange)
                break;

            positions.Add(pos);

            // If LOS is required, stop at walls
            if (stopAtWalls && !mapSystem.IsWalkable(pos))
                break;
        }

        return positions;
    }
}
