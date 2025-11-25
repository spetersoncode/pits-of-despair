using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Unified targeting handler for all creature-based targeting.
/// Uses the filter from TargetingDefinition to determine valid targets (Enemy/Ally/Creature).
/// Supports configurable range and distance metric.
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
        HashSet<GridPosition> visibleTiles = null;
        if (definition.RequiresLOS)
        {
            visibleTiles = FOVCalculator.CalculateVisibleTiles(
                casterPos, range, context.MapSystem, definition.Metric);
        }

        foreach (var entity in context.EntityManager.GetAllEntities())
        {
            // Skip self unless filter is Ally (which includes self)
            if (entity == caster && definition.Filter != TargetFilter.Ally)
                continue;

            // Must be a creature (has health)
            if (!IsCreature(entity))
                continue;

            // Check filter
            if (!PassesFilter(caster, entity, definition.Filter))
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

        // Check if there's a valid creature at this position
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        if (entity == null || !IsCreature(entity))
            return false;

        // Check filter
        return PassesFilter(caster, entity, definition.Filter);
    }

    private bool IsCreature(BaseEntity entity)
    {
        return entity.GetNodeOrNull<HealthComponent>("HealthComponent") != null;
    }

    private bool PassesFilter(BaseEntity caster, BaseEntity target, TargetFilter filter)
    {
        return filter switch
        {
            TargetFilter.Enemy => caster.Faction != target.Faction,
            TargetFilter.Ally => caster.Faction == target.Faction,
            TargetFilter.Creature => true,
            _ => true
        };
    }
}
