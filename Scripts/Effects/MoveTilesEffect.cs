using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that moves the caster a specified number of tiles in a direction.
/// Direction is derived from caster position to target position.
/// Used by movement skills like Quick Step.
/// </summary>
public class MoveTilesEffect : Effect
{
    public override string Type => "move_tiles";
    public override string Name => "Move Tiles";

    /// <summary>
    /// Number of tiles to move.
    /// </summary>
    public int Amount { get; set; }

    public MoveTilesEffect()
    {
        Amount = 1;
    }

    public MoveTilesEffect(int amount)
    {
        Amount = amount > 0 ? amount : 1;
    }

    public MoveTilesEffect(EffectDefinition definition)
    {
        Amount = definition.Amount > 0 ? definition.Amount : 1;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var caster = context.Caster;
        if (caster == null)
        {
            return EffectResult.CreateFailure("No caster for movement effect.");
        }

        // Need target position to determine direction
        if (context.TargetPosition == null)
        {
            return EffectResult.CreateFailure("No direction specified.");
        }

        var casterPos = caster.GridPosition;
        var targetPos = context.TargetPosition.Value;

        // Calculate direction (normalize to -1, 0, or 1 per axis)
        int dx = System.Math.Sign(targetPos.X - casterPos.X);
        int dy = System.Math.Sign(targetPos.Y - casterPos.Y);

        if (dx == 0 && dy == 0)
        {
            return EffectResult.CreateFailure("Invalid direction.");
        }

        var direction = new Vector2I(dx, dy);
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        // Track how many tiles we actually move
        int tilesMoved = 0;
        var currentPos = casterPos;

        for (int i = 0; i < Amount; i++)
        {
            var nextPos = new GridPosition(currentPos.X + direction.X, currentPos.Y + direction.Y);

            // Check if tile is in bounds
            if (!mapSystem.IsInBounds(nextPos))
            {
                break;
            }

            // Check if tile is walkable
            if (!mapSystem.IsWalkable(nextPos))
            {
                break;
            }

            // Check for entity at next position
            var entityAtNext = entityManager.GetEntityAtPosition(nextPos);

            // Also check if player is at that position
            if (entityAtNext == null && context.ActionContext.Player?.GridPosition.Equals(nextPos) == true)
            {
                entityAtNext = context.ActionContext.Player;
            }

            if (entityAtNext != null)
            {
                // Can only interact with entities on the first tile
                if (i == 0)
                {
                    // Check if we can swap with friendly
                    if (caster.Faction.IsFriendlyTo(entityAtNext.Faction))
                    {
                        // Swap positions
                        entityAtNext.SetGridPosition(currentPos);
                        currentPos = nextPos;
                        tilesMoved++;
                        continue;
                    }
                    else
                    {
                        // Blocked by non-friendly entity on first tile - fail
                        break;
                    }
                }
                else
                {
                    // Blocked by entity on subsequent tile - stop here
                    break;
                }
            }

            // Move to next position
            currentPos = nextPos;
            tilesMoved++;
        }

        // First tile must be passable for skill to succeed
        if (tilesMoved == 0)
        {
            return EffectResult.CreateFailure(
                $"{caster.DisplayName} can't move in that direction!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Actually move the caster
        caster.SetGridPosition(currentPos);

        // Generate appropriate message
        string message;
        if (tilesMoved == Amount)
        {
            message = $"{caster.DisplayName} dashes forward!";
        }
        else
        {
            message = $"{caster.DisplayName} moves {tilesMoved} tile{(tilesMoved > 1 ? "s" : "")}.";
        }

        return EffectResult.CreateSuccess(
            message,
            Palette.ToHex(Palette.Default),
            caster
        );
    }
}
