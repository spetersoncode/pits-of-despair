using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using System.Linq;

namespace PitsOfDespair.Effects;

/// <summary>
/// Teleports the target to a random unoccupied location on the current map.
/// </summary>
public class TeleportEffect : Effect
{
    public override string Name => "Teleport";

    /// <summary>
    /// Applies the teleport effect to the target entity.
    /// Teleports the target to a random walkable, unoccupied position anywhere on the map.
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

        return new EffectResult(
            true,
            $"{target.DisplayName} teleports to a distant location!",
            Palette.ToHex(Palette.Magenta)
        );
    }
}
