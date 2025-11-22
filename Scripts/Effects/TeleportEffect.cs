using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Effects;

/// <summary>
/// Teleports the target to a random unoccupied location on the current map.
/// If the target is the player, all player faction companions are teleported nearby.
/// </summary>
public class TeleportEffect : Effect
{
    private const int CompanionTeleportRange = 3;

    public override string Name => "Teleport";

    /// <summary>
    /// Applies the teleport effect to the target entity.
    /// Teleports the target to a random walkable, unoccupied position anywhere on the map.
    /// If the target is the player, companions are teleported nearby.
    /// </summary>
    /// <param name="target">The entity to teleport</param>
    /// <param name="context">The action context containing map and entity information</param>
    /// <returns>The result of the effect application</returns>
    public override EffectResult Apply(BaseEntity target, ActionContext context)
    {
        var currentPos = target.GridPosition;

        // Get all walkable tiles on the map
        var allWalkableTiles = context.MapSystem.GetAllWalkableTiles();

        // Filter out occupied positions and current position
        var validPositions = allWalkableTiles
            .Where(pos => pos != currentPos && !context.EntityManager.IsPositionOccupied(pos))
            .ToList();

        // If no valid positions found, the magic fizzles
        if (validPositions.Count == 0)
        {
            return new EffectResult(
                false,
                $"{target.DisplayName} tries to teleport, but the magic fizzles!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Pick a random destination from all valid positions
        int randomIndex = GD.RandRange(0, validPositions.Count - 1);
        var newPos = validPositions[randomIndex];

        // Teleport the target
        target.SetGridPosition(newPos);

        // If the target is the player, teleport companions nearby
        if (target is Player)
        {
            TeleportCompanionsNearby(target, newPos, context);
        }

        return new EffectResult(
            true,
            $"{target.DisplayName} teleports to a distant location!",
            Palette.ToHex(Palette.ScrollTeleport)
        );
    }

    /// <summary>
    /// Teleports all player faction companions to positions near the player's new location.
    /// </summary>
    private void TeleportCompanionsNearby(BaseEntity player, GridPosition playerNewPos, ActionContext context)
    {
        // Find all player faction entities (excluding the player)
        var companions = context.EntityManager.GetAllEntities()
            .Where(e => e != player && e.Faction == Faction.Player)
            .ToList();

        if (companions.Count == 0)
            return;

        // Find valid positions near the player
        var nearbyPositions = FindNearbyValidPositions(playerNewPos, CompanionTeleportRange, context);

        // Teleport each companion to a nearby position
        foreach (var companion in companions)
        {
            if (nearbyPositions.Count == 0)
                break;

            // Pick a random nearby position
            int index = GD.RandRange(0, nearbyPositions.Count - 1);
            var companionNewPos = nearbyPositions[index];
            nearbyPositions.RemoveAt(index);

            companion.SetGridPosition(companionNewPos);
        }
    }

    /// <summary>
    /// Finds all valid (walkable, unoccupied) positions within range of the center position.
    /// </summary>
    private List<GridPosition> FindNearbyValidPositions(GridPosition center, int range, ActionContext context)
    {
        var validPositions = new List<GridPosition>();

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var checkPos = new GridPosition(center.X + dx, center.Y + dy);

                // Check Chebyshev distance
                if (DistanceHelper.ChebyshevDistance(center, checkPos) > range)
                    continue;

                if (!context.MapSystem.IsWalkable(checkPos))
                    continue;

                if (context.EntityManager.IsPositionOccupied(checkPos))
                    continue;

                // Also check if player is at this position
                if (checkPos == center)
                    continue;

                validPositions.Add(checkPos);
            }
        }

        return validPositions;
    }
}
