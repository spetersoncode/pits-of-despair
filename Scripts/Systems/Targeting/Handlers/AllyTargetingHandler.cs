using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for ally targeting.
/// Targets entities of the same faction within range.
/// Includes the caster as a valid target.
/// </summary>
public class AllyTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Ally;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        TargetingDefinition definition,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;
        int range = definition.Range > 0 ? definition.Range : 1;

        // Caster is always a valid ally target
        validPositions.Add(casterPos);

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

            if (!IsAlly(caster, entity))
                continue;

            var entityPos = entity.GridPosition;

            // Check range
            if (!DistanceHelper.IsInRange(casterPos, entityPos, range, definition.Metric))
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

        // Caster is always valid
        if (targetPosition == casterPos)
            return true;

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

        // Check if there's an ally at this position
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        return entity != null && IsAlly(caster, entity);
    }

    private bool IsAlly(BaseEntity caster, BaseEntity target)
    {
        return caster.Faction == target.Faction;
    }

}
