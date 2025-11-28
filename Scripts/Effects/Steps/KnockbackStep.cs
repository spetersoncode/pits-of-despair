using System;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that pushes the target away from the caster.
/// </summary>
public class KnockbackStep : IEffectStep
{
    private readonly int _distance;
    private readonly string? _scalingStat;
    private readonly float _scalingMultiplier;

    public KnockbackStep(StepDefinition definition)
    {
        _distance = definition.Distance > 0 ? definition.Distance : 1;
        _scalingStat = definition.ScalingStat;
        _scalingMultiplier = definition.ScalingMultiplier;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;

        // Knockback requires a caster to determine direction
        if (context.Caster == null)
        {
            return;
        }

        var casterPos = context.Caster.GridPosition;
        var targetPos = target.GridPosition;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        // Calculate direction away from caster
        int dx = targetPos.X - casterPos.X;
        int dy = targetPos.Y - casterPos.Y;

        // Normalize to -1, 0, or 1
        int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        // If target is at same position as caster, pick a random direction
        if (dirX == 0 && dirY == 0)
        {
            dirX = GD.RandRange(-1, 1);
            dirY = GD.RandRange(-1, 1);
            if (dirX == 0 && dirY == 0)
                dirX = 1;
        }

        // Calculate final distance with scaling
        int finalDistance = _distance;
        if (!string.IsNullOrEmpty(_scalingStat) && context.Caster != null)
        {
            int statValue = context.GetCasterStat(_scalingStat);
            finalDistance += (int)(statValue * _scalingMultiplier);
        }

        if (finalDistance <= 0)
        {
            return;
        }

        // Try to push the target as far as possible
        var currentPos = targetPos;
        int tilesKnocked = 0;

        for (int i = 0; i < finalDistance; i++)
        {
            var nextPos = new GridPosition(currentPos.X + dirX, currentPos.Y + dirY);

            // Check if next position is valid
            if (!mapSystem.IsWalkable(nextPos) || entityManager.IsPositionOccupied(nextPos))
            {
                break;
            }

            currentPos = nextPos;
            tilesKnocked++;
        }

        if (tilesKnocked == 0)
        {
            messages.Add($"{target.DisplayName} cannot be pushed!", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Move the target to the final position
        target.SetGridPosition(currentPos);

        string message = tilesKnocked == 1
            ? $"{target.DisplayName} is knocked back!"
            : $"{target.DisplayName} is knocked back {tilesKnocked} tiles!";

        messages.Add(message, Palette.ToHex(Palette.CombatDamage));
        state.Success = true;
    }
}
