using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for creature targeting (any entity with health).
/// Targets both enemies and allies within range.
/// </summary>
public class CreatureTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Creature;

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

            // Must be a creature (has health)
            if (!IsCreature(entity))
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

        // Check if there's a creature at this position
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        return entity != null && IsCreature(entity);
    }

    private bool IsCreature(BaseEntity entity)
    {
        return entity.GetNodeOrNull<HealthComponent>("HealthComponent") != null;
    }

}
