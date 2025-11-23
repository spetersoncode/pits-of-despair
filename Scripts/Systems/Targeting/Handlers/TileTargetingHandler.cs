using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for tile targeting.
/// Can target any walkable tile within range.
/// </summary>
public class TileTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Tile;

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

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                var checkPos = new GridPosition(casterPos.X + dx, casterPos.Y + dy);

                // Check range using the specified metric
                if (!IsInRange(casterPos, checkPos, range, definition.Metric))
                    continue;

                // Check LOS if required
                if (visibleTiles != null && !visibleTiles.Contains(checkPos))
                    continue;

                // Check if tile is walkable
                if (context.MapSystem.IsWalkable(checkPos))
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

        // Check range
        if (!IsInRange(casterPos, targetPosition, range, definition.Metric))
            return false;

        // Check LOS if required
        if (definition.RequiresLOS)
        {
            var visibleTiles = FOVCalculator.CalculateVisibleTiles(
                casterPos, range, context.MapSystem, definition.Metric);
            if (!visibleTiles.Contains(targetPosition))
                return false;
        }

        return context.MapSystem.IsWalkable(targetPosition);
    }

    protected bool IsInRange(GridPosition from, GridPosition to, int range, DistanceMetric metric)
    {
        return metric == DistanceMetric.Euclidean
            ? DistanceHelper.EuclideanDistance(from, to) <= range
            : DistanceHelper.ChebyshevDistance(from, to) <= range;
    }
}
