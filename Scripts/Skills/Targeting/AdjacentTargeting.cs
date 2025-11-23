using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills.Targeting;

/// <summary>
/// Targeting handler for skills that target adjacent tiles (8 directions).
/// </summary>
public class AdjacentTargeting : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Adjacent;

    private static readonly (int dx, int dy)[] AdjacentOffsets = new[]
    {
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1)
    };

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        SkillDefinition skill,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;

        foreach (var (dx, dy) in AdjacentOffsets)
        {
            var checkPos = new GridPosition(casterPos.X + dx, casterPos.Y + dy);

            // Check if there's a valid target at this position
            if (IsValidTarget(caster, checkPos, skill, context))
            {
                validPositions.Add(checkPos);
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

        // Check if position is adjacent (Chebyshev distance of 1)
        if (DistanceHelper.ChebyshevDistance(casterPos, targetPosition) != 1)
        {
            return false;
        }

        // Check if there's an entity at the position
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        return entity != null;
    }
}
