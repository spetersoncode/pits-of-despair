using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for adjacent targeting (8 directions).
/// Targets entities in adjacent tiles based on the filter.
/// </summary>
public class AdjacentTargetingHandler : TargetingHandler
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
        TargetingDefinition definition,
        ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var casterPos = caster.GridPosition;

        foreach (var (dx, dy) in AdjacentOffsets)
        {
            var checkPos = new GridPosition(casterPos.X + dx, casterPos.Y + dy);

            if (IsValidTarget(caster, checkPos, definition, context))
            {
                validPositions.Add(checkPos);
            }
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

        // Must be adjacent (Chebyshev distance of 1)
        if (DistanceHelper.ChebyshevDistance(casterPos, targetPosition) != 1)
            return false;

        // Apply filter based on definition
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);

        return definition.Filter switch
        {
            TargetFilter.Enemy => entity != null && caster.Faction != entity.Faction,
            TargetFilter.Ally => entity != null && caster.Faction == entity.Faction,
            TargetFilter.Creature => entity != null,
            TargetFilter.Tile => context.MapSystem.IsWalkable(targetPosition),
            TargetFilter.Self => targetPosition == casterPos,
            _ => entity != null
        };
    }
}
