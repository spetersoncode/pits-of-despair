using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that moves the caster a specified number of tiles in a direction.
/// Direction is derived from caster position to target position.
/// </summary>
public class MoveTilesStep : IEffectStep
{
    private readonly int _amount;

    public MoveTilesStep(StepDefinition definition)
    {
        _amount = definition.Amount > 0 ? definition.Amount : 1;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var caster = context.Caster;
        if (caster == null)
        {
            messages.Add("No caster for movement effect.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Need target position to determine direction
        if (context.TargetPosition == null)
        {
            messages.Add("No direction specified.", Palette.ToHex(Palette.Disabled));
            return;
        }

        var casterPos = caster.GridPosition;
        var targetPos = context.TargetPosition.Value;

        // Calculate direction (normalize to -1, 0, or 1 per axis)
        int dx = System.Math.Sign(targetPos.X - casterPos.X);
        int dy = System.Math.Sign(targetPos.Y - casterPos.Y);

        if (dx == 0 && dy == 0)
        {
            messages.Add("Invalid direction.", Palette.ToHex(Palette.Disabled));
            return;
        }

        var direction = new Vector2I(dx, dy);
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        // Track how many tiles we actually move
        int tilesMoved = 0;
        var currentPos = casterPos;

        for (int i = 0; i < _amount; i++)
        {
            var nextPos = new GridPosition(currentPos.X + direction.X, currentPos.Y + direction.Y);

            // Check if tile is in bounds
            if (!mapSystem.IsInBounds(nextPos))
                break;

            // Check if tile is walkable
            if (!mapSystem.IsWalkable(nextPos))
                break;

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
            messages.Add($"{caster.DisplayName} can't move in that direction!", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Actually move the caster
        caster.SetGridPosition(currentPos);

        // Generate appropriate message
        string message = tilesMoved == _amount
            ? $"{caster.DisplayName} dashes forward!"
            : $"{caster.DisplayName} moves {tilesMoved} tile{(tilesMoved > 1 ? "s" : "")}.";

        messages.Add(message, Palette.ToHex(Palette.Default));
        state.Success = true;
    }
}
