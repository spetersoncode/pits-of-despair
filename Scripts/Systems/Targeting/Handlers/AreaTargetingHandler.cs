using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for area-of-effect targeting.
/// Player selects a center position, effect affects all entities within area size.
/// </summary>
public class AreaTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Area;

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

                // Check range
                if (!IsInRange(casterPos, checkPos, range, definition.Metric))
                    continue;

                // Check LOS if required
                if (visibleTiles != null && !visibleTiles.Contains(checkPos))
                    continue;

                // For area targeting, any walkable tile within range is valid
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

    public override List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        var entities = new List<BaseEntity>();
        int areaSize = definition.AreaSize > 0 ? definition.AreaSize : 1;

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
        var positions = new List<GridPosition>();
        int areaSize = definition.AreaSize > 0 ? definition.AreaSize : 1;

        for (int dx = -areaSize; dx <= areaSize; dx++)
        {
            for (int dy = -areaSize; dy <= areaSize; dy++)
            {
                var checkPos = new GridPosition(targetPosition.X + dx, targetPosition.Y + dy);

                // Check area radius (Chebyshev distance for square area)
                if (DistanceHelper.ChebyshevDistance(targetPosition, checkPos) > areaSize)
                    continue;

                positions.Add(checkPos);
            }
        }

        return positions;
    }

    private bool IsInRange(GridPosition from, GridPosition to, int range, DistanceMetric metric)
    {
        return metric == DistanceMetric.Euclidean
            ? DistanceHelper.EuclideanDistance(from, to) <= range
            : DistanceHelper.ChebyshevDistance(from, to) <= range;
    }
}
