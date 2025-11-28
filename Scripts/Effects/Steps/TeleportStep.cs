using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that teleports the target to a random unoccupied location.
/// Supports range-limited teleportation or full-map teleportation.
/// </summary>
public class TeleportStep : IEffectStep
{
    private const int CompanionTeleportRange = 3;

    private readonly int _range;
    private readonly bool _teleportCompanions;

    public TeleportStep(StepDefinition definition)
    {
        _range = definition.Range;
        _teleportCompanions = definition.TeleportCompanions;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var currentPos = target.GridPosition;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        List<GridPosition> validPositions;

        if (_range <= 0)
        {
            // Teleport to any valid position on the map
            validPositions = mapSystem.GetAllWalkableTiles()
                .Where(pos => pos != currentPos && !entityManager.IsPositionOccupied(pos))
                .ToList();
        }
        else
        {
            // Teleport within range
            validPositions = FindValidPositionsInRange(currentPos, _range, context.ActionContext);
        }

        if (validPositions.Count == 0)
        {
            messages.Add($"{target.DisplayName} tries to teleport, but the magic fizzles!", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Pick a random destination
        int randomIndex = GD.RandRange(0, validPositions.Count - 1);
        var newPos = validPositions[randomIndex];

        // Teleport the target
        target.SetGridPosition(newPos);

        // If the target is the player and we should teleport companions
        if (_teleportCompanions && target is Player)
        {
            TeleportCompanionsNearby(target, newPos, context.ActionContext);
        }

        string message = _range > 0
            ? $"{target.DisplayName} teleports!"
            : $"{target.DisplayName} teleports to a distant location!";

        messages.Add(message, Palette.ToHex(Palette.ScrollTeleport));
        state.Success = true;
    }

    private List<GridPosition> FindValidPositionsInRange(GridPosition center, int range, ActionContext context)
    {
        var validPositions = new List<GridPosition>();
        var mapSystem = context.MapSystem;
        var entityManager = context.EntityManager;

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

    private void TeleportCompanionsNearby(BaseEntity player, GridPosition playerNewPos, ActionContext context)
    {
        var companions = context.EntityManager.GetAllEntities()
            .Where(e => e != player && e.Faction == Faction.Player)
            .ToList();

        if (companions.Count == 0)
            return;

        var nearbyPositions = FindValidPositionsInRange(playerNewPos, CompanionTeleportRange, context);

        foreach (var companion in companions)
        {
            if (nearbyPositions.Count == 0)
                break;

            int index = GD.RandRange(0, nearbyPositions.Count - 1);
            var companionNewPos = nearbyPositions[index];
            nearbyPositions.RemoveAt(index);

            companion.SetGridPosition(companionNewPos);
        }
    }
}
