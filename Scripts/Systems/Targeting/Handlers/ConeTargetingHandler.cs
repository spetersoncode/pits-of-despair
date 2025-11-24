using System;
using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for cone-shaped area targeting.
/// Player selects a target tile to define direction, effect affects all tiles in a cone from caster.
/// </summary>
public class ConeTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Cone;

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

        // For cone targeting, any tile in range can be a valid target
        // The cone is calculated from caster toward that target direction
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
        int range = definition.Range > 0 ? definition.Range : 3;
        int radius = definition.Radius > 0 ? definition.Radius : 2;

        return GetConePositions(casterPos, targetPosition, range, radius, context.MapSystem, definition.RequiresLOS);
    }

    /// <summary>
    /// Calculates all grid positions in a cone from origin toward target direction.
    /// The cone spreads out as it extends from the origin.
    /// </summary>
    /// <param name="origin">Starting position (excluded from result)</param>
    /// <param name="target">Target position defining cone direction</param>
    /// <param name="length">How far the cone extends</param>
    /// <param name="spread">How wide the cone spreads (tiles at max range)</param>
    /// <param name="mapSystem">Map system for bounds checking</param>
    /// <param name="stopAtWalls">If true, excludes tiles blocked by walls</param>
    /// <returns>List of grid positions within the cone (excluding origin)</returns>
    public static List<GridPosition> GetConePositions(
        GridPosition origin,
        GridPosition target,
        int length,
        int spread,
        Systems.MapSystem mapSystem,
        bool stopAtWalls = true)
    {
        var positions = new List<GridPosition>();

        // Calculate direction vector
        float dirX = target.X - origin.X;
        float dirY = target.Y - origin.Y;
        float dirLength = (float)Math.Sqrt(dirX * dirX + dirY * dirY);

        if (dirLength < 0.001f)
            return positions;

        // Normalize direction
        dirX /= dirLength;
        dirY /= dirLength;

        // Calculate perpendicular vector for width
        float perpX = -dirY;
        float perpY = dirX;

        // Calculate cone angle (wider spread = wider angle)
        // spread determines how many tiles wide at max range
        float halfAngle = (float)Math.Atan2(spread, length);

        // Check all tiles in a bounding box around the cone
        for (int dx = -length - spread; dx <= length + spread; dx++)
        {
            for (int dy = -length - spread; dy <= length + spread; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var checkPos = new GridPosition(origin.X + dx, origin.Y + dy);

                if (!mapSystem.IsInBounds(checkPos))
                    continue;

                // Calculate vector from origin to check position
                float toCheckX = checkPos.X - origin.X;
                float toCheckY = checkPos.Y - origin.Y;
                float distToCheck = (float)Math.Sqrt(toCheckX * toCheckX + toCheckY * toCheckY);

                if (distToCheck < 0.001f || distToCheck > length)
                    continue;

                // Normalize vector to check position
                float normToCheckX = toCheckX / distToCheck;
                float normToCheckY = toCheckY / distToCheck;

                // Calculate angle between direction and check position
                float dot = dirX * normToCheckX + dirY * normToCheckY;
                float angle = (float)Math.Acos(Math.Clamp(dot, -1.0f, 1.0f));

                // Check if within cone angle
                if (angle <= halfAngle)
                {
                    // Check LOS if required
                    if (stopAtWalls)
                    {
                        bool blocked = false;
                        var linePositions = LineTargetingHandler.GetLinePositions(
                            origin, checkPos, length, mapSystem, stopAtWalls: false);

                        foreach (var linePos in linePositions)
                        {
                            if (linePos == checkPos)
                                break;
                            if (!mapSystem.IsWalkable(linePos))
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (blocked)
                            continue;
                    }

                    positions.Add(checkPos);
                }
            }
        }

        return positions;
    }
}
