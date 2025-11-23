using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for enemy targeting.
/// Targets entities of a different faction within range.
/// </summary>
public class EnemyTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Enemy;

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

        foreach (var entity in context.EntityManager.GetAllEntities())
        {
            if (entity == caster)
                continue;

            if (!IsEnemy(caster, entity))
                continue;

            var entityPos = entity.GridPosition;

            // Check range
            if (!IsInRange(casterPos, entityPos, range, definition.Metric))
                continue;

            // Check LOS if required
            if (visibleTiles != null && !visibleTiles.Contains(entityPos))
                continue;

            validPositions.Add(entityPos);
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

        // Check if there's an enemy at this position
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        return entity != null && IsEnemy(caster, entity);
    }

    private bool IsEnemy(BaseEntity caster, BaseEntity target)
    {
        return caster.Faction != target.Faction;
    }

    private bool IsInRange(GridPosition from, GridPosition to, int range, DistanceMetric metric)
    {
        return metric == DistanceMetric.Euclidean
            ? DistanceHelper.EuclideanDistance(from, to) <= range
            : DistanceHelper.ChebyshevDistance(from, to) <= range;
    }
}
