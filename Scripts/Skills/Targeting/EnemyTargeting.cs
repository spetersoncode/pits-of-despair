using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills.Targeting;

/// <summary>
/// Targeting handler for skills that target enemy entities within range.
/// </summary>
public class EnemyTargeting : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Enemy;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        SkillDefinition skill,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;
        int range = skill.Range > 0 ? skill.Range : 1;

        // Get all entities and filter to enemies within range
        foreach (var entity in context.EntityManager.GetAllEntities())
        {
            if (entity == caster)
                continue;

            // Check if enemy
            if (!IsEnemy(caster, entity))
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

        // Check if there's an enemy at this position
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        if (entity == null)
        {
            return false;
        }

        return IsEnemy(caster, entity);
    }

    /// <summary>
    /// Determines if the target is an enemy of the caster.
    /// </summary>
    private bool IsEnemy(BaseEntity caster, BaseEntity target)
    {
        // Different factions are enemies
        return caster.Faction != target.Faction;
    }
}
