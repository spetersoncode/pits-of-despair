using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Effects;

/// <summary>
/// Teleports the target to a random unoccupied location.
/// Supports range-limited teleportation (for skills) or full-map teleportation (for items).
/// If TeleportCompanions is true and the target is the player, companions are teleported nearby.
/// </summary>
public class TeleportEffect : Effect
{
    private const int CompanionTeleportRange = 3;

    public override string Type => "teleport";
    public override string Name => "Teleport";

    /// <summary>
    /// Range for teleportation. 0 = anywhere on the map.
    /// </summary>
    public int Range { get; set; } = 0;

    /// <summary>
    /// Whether to teleport companions when the target is the player.
    /// Typically true for item teleports, false for skill teleports.
    /// </summary>
    public bool TeleportCompanions { get; set; } = true;

    public TeleportEffect() { }

    /// <summary>
    /// Creates a teleport effect from a unified effect definition.
    /// </summary>
    public TeleportEffect(EffectDefinition definition)
    {
        Range = definition.Range > 0 ? definition.Range : definition.Amount;
        // Default: teleport companions for items (no scaling stat), don't for skills
        TeleportCompanions = string.IsNullOrEmpty(definition.ScalingStat);
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;
        var currentPos = target.GridPosition;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        List<GridPosition> validPositions;

        if (Range <= 0)
        {
            // Teleport to any valid position on the map
            validPositions = mapSystem.GetAllWalkableTiles()
                .Where(pos => pos != currentPos && !entityManager.IsPositionOccupied(pos))
                .ToList();
        }
        else
        {
            // Teleport within range
            validPositions = FindValidPositionsInRange(currentPos, Range, context.ActionContext);
        }

        if (validPositions.Count == 0)
        {
            return EffectResult.CreateFailure(
                $"{targetName} tries to teleport, but the magic fizzles!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Pick a random destination from all valid positions
        int randomIndex = GD.RandRange(0, validPositions.Count - 1);
        var newPos = validPositions[randomIndex];

        // Teleport the target
        target.SetGridPosition(newPos);

        // If the target is the player and we should teleport companions
        if (TeleportCompanions && target is Player)
        {
            TeleportCompanionsNearby(target, newPos, context.ActionContext);
        }

        string message = Range > 0
            ? $"{targetName} teleports!"
            : $"{targetName} teleports to a distant location!";

        return EffectResult.CreateSuccess(
            message,
            Palette.ToHex(Palette.ScrollTeleport),
            target
        );
    }

    /// <summary>
    /// Finds all valid (walkable, unoccupied) positions within range of the center position.
    /// </summary>
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

    /// <summary>
    /// Teleports all player faction companions to positions near the player's new location.
    /// </summary>
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
