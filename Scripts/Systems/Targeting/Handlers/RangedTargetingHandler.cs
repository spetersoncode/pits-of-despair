using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for ranged attacks.
/// Uses Euclidean (circular) distance with line-of-sight.
/// Targets hostile creatures within range.
/// </summary>
public class RangedTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Ranged;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        TargetingDefinition definition,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;
        int range = definition.Range > 0 ? definition.Range : 1;

        // Ranged always uses Euclidean distance and requires LOS
        var visibleTiles = FOVCalculator.CalculateVisibleTiles(
            casterPos, range, context.MapSystem, DistanceMetric.Euclidean);

        foreach (var entity in context.EntityManager.GetAllEntities())
        {
            if (entity == caster)
                continue;

            // Must be a hostile creature (different faction with health)
            if (!IsValidCreatureTarget(caster, entity))
                continue;

            var entityPos = entity.GridPosition;

            // Must be visible (in FOV and LOS)
            if (!visibleTiles.Contains(entityPos))
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

        // Check Euclidean range
        if (DistanceHelper.EuclideanDistance(casterPos, targetPosition) > range)
            return false;

        // Check LOS
        var visibleTiles = FOVCalculator.CalculateVisibleTiles(
            casterPos, range, context.MapSystem, DistanceMetric.Euclidean);
        if (!visibleTiles.Contains(targetPosition))
            return false;

        // Check if there's a valid creature target
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        return entity != null && IsValidCreatureTarget(caster, entity);
    }

    private bool IsValidCreatureTarget(BaseEntity caster, BaseEntity target)
    {
        // Must have health component (be attackable)
        if (target.GetNodeOrNull<HealthComponent>("HealthComponent") == null)
            return false;

        // Must be hostile (different faction)
        return caster.Faction != target.Faction;
    }
}
