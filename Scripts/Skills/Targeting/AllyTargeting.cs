using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills.Targeting;

/// <summary>
/// Targeting handler for skills that target allied entities within range.
/// Includes the caster as a valid target.
/// </summary>
public class AllyTargeting : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Ally;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        SkillDefinition skill,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;
        int range = skill.Range > 0 ? skill.Range : 1;

        // Include caster's position as valid
        validPositions.Add(casterPos);

        // Get all entities and filter to allies within range
        foreach (var entity in context.EntityManager.GetAllEntities())
        {
            if (entity == caster)
                continue;

            // Check if ally
            if (!IsAlly(caster, entity))
                continue;

            // Check range
            if (DistanceHelper.ChebyshevDistance(casterPos, entity.GridPosition) > range)
                continue;

            validPositions.Add(entity.GridPosition);
        }

        return validPositions;
    }

    public override bool IsValidTarget(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skill,
        ActionContext context)
    {
        var casterPos = caster.GridPosition;
        int range = skill.Range > 0 ? skill.Range : 1;

        // Check range
        if (DistanceHelper.ChebyshevDistance(casterPos, targetPosition) > range)
        {
            return false;
        }

        // Caster is always a valid ally target
        if (targetPosition == casterPos)
        {
            return true;
        }

        // Check if there's an ally at this position
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        if (entity == null)
        {
            return false;
        }

        return IsAlly(caster, entity);
    }

    /// <summary>
    /// Determines if the target is an ally of the caster.
    /// </summary>
    private bool IsAlly(BaseEntity caster, BaseEntity target)
    {
        // Same faction are allies
        return caster.Faction == target.Faction;
    }
}
