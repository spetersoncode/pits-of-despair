using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills.Targeting;

/// <summary>
/// Targeting handler for skills that can target any tile within range.
/// </summary>
public class TileTargeting : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Tile;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        SkillDefinition skill,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;
        int range = skill.Range > 0 ? skill.Range : 1;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                var checkPos = new GridPosition(casterPos.X + dx, casterPos.Y + dy);

                if (IsValidTarget(caster, checkPos, skill, context))
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

        // Check if tile is walkable (for most purposes)
        return context.MapSystem.IsWalkable(targetPosition);
    }

    public override List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skill,
        ActionContext context)
    {
        var entities = new List<BaseEntity>();
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        if (entity != null)
        {
            entities.Add(entity);
        }
        return entities;
    }
}
