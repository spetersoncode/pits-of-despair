using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills.Targeting;

/// <summary>
/// Targeting handler for area-of-effect skills.
/// Player selects a center position, skill affects all entities within area size.
/// </summary>
public class AreaTargeting : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Area;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        SkillDefinition skill,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;
        int range = skill.Range > 0 ? skill.Range : 1;

        // All positions within range are valid centers for the area
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                var checkPos = new GridPosition(casterPos.X + dx, casterPos.Y + dy);

                // Check range
                if (DistanceHelper.ChebyshevDistance(casterPos, checkPos) > range)
                    continue;

                // Check if tile is visible/walkable
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

        // For area targeting, any tile within range is valid
        return context.MapSystem.IsWalkable(targetPosition);
    }

    public override List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skill,
        ActionContext context)
    {
        var entities = new List<BaseEntity>();
        int areaSize = skill.AreaSize > 0 ? skill.AreaSize : 1;

        // Get all entities within the area
        for (int dx = -areaSize; dx <= areaSize; dx++)
        {
            for (int dy = -areaSize; dy <= areaSize; dy++)
            {
                var checkPos = new GridPosition(targetPosition.X + dx, targetPosition.Y + dy);

                // Check area radius (Chebyshev distance for square area)
                if (DistanceHelper.ChebyshevDistance(targetPosition, checkPos) > areaSize)
                    continue;

                var entity = context.EntityManager.GetEntityAtPosition(checkPos);
                if (entity != null && !entities.Contains(entity))
                {
                    entities.Add(entity);
                }
            }
        }

        return entities;
    }

    /// <summary>
    /// Gets all positions that would be affected by the area.
    /// Useful for UI highlighting.
    /// </summary>
    public List<GridPosition> GetAffectedPositions(
        GridPosition centerPosition,
        SkillDefinition skill,
        ActionContext context)
    {
        var positions = new List<GridPosition>();
        int areaSize = skill.AreaSize > 0 ? skill.AreaSize : 1;

        for (int dx = -areaSize; dx <= areaSize; dx++)
        {
            for (int dy = -areaSize; dy <= areaSize; dy++)
            {
                var checkPos = new GridPosition(centerPosition.X + dx, centerPosition.Y + dy);

                if (DistanceHelper.ChebyshevDistance(centerPosition, checkPos) > areaSize)
                    continue;

                positions.Add(checkPos);
            }
        }

        return positions;
    }
}
