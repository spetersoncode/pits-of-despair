using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Skills.Effects;

/// <summary>
/// Skill effect that teleports the caster or target to a location.
/// Different from item TeleportEffect - this one can target specific positions.
/// </summary>
public class TeleportEffect : SkillEffect
{
    public override string Type => "teleport";

    /// <summary>
    /// Whether to teleport to a random location (true) or to target position (false).
    /// </summary>
    public bool RandomDestination { get; set; } = true;

    /// <summary>
    /// Range for random teleport destination.
    /// </summary>
    public int Range { get; set; } = 0;

    public TeleportEffect() { }

    public TeleportEffect(SkillEffectDefinition definition)
    {
        // Range of 0 means anywhere on the map
        Range = definition.Amount;
        RandomDestination = true;
    }

    public override SkillEffectResult Apply(BaseEntity target, SkillEffectContext context)
    {
        var targetName = target.DisplayName;
        var currentPos = target.GridPosition;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        List<GridPosition> validPositions;

        if (RandomDestination && Range <= 0)
        {
            // Teleport to any valid position on the map
            validPositions = mapSystem.GetAllWalkableTiles()
                .Where(pos => pos != currentPos && !entityManager.IsPositionOccupied(pos))
                .ToList();
        }
        else if (RandomDestination)
        {
            // Teleport within range
            validPositions = FindValidPositionsInRange(currentPos, Range, context);
        }
        else
        {
            // Targeted teleport - target position should be set externally
            // For now, treat as random within range
            validPositions = FindValidPositionsInRange(currentPos, Range > 0 ? Range : 5, context);
        }

        if (validPositions.Count == 0)
        {
            return SkillEffectResult.CreateFailure(
                $"{targetName} cannot find a place to teleport!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Pick a random destination
        int randomIndex = GD.RandRange(0, validPositions.Count - 1);
        var newPos = validPositions[randomIndex];

        // Teleport the target
        target.SetGridPosition(newPos);

        return SkillEffectResult.CreateSuccess(
            $"{targetName} teleports!",
            Palette.ToHex(Palette.Success),
            target
        );
    }

    private List<GridPosition> FindValidPositionsInRange(GridPosition center, int range, SkillEffectContext context)
    {
        var validPositions = new List<GridPosition>();
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var checkPos = new GridPosition(center.X + dx, center.Y + dy);

                if (DistanceHelper.ChebyshevDistance(center, checkPos) > range)
                    continue;

                if (!mapSystem.IsWalkable(checkPos))
                    continue;

                if (entityManager.IsPositionOccupied(checkPos))
                    continue;

                validPositions.Add(checkPos);
            }
        }

        return validPositions;
    }
}
