using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that pushes the target away from the caster.
/// Supports stat scaling for knockback distance.
/// </summary>
public class KnockbackEffect : Effect
{
    public override string Type => "knockback";
    public override string Name => "Knockback";

    /// <summary>
    /// Number of tiles to push the target.
    /// </summary>
    public int Distance { get; set; } = 1;

    /// <summary>
    /// Stat to scale knockback distance with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    public KnockbackEffect() { }

    /// <summary>
    /// Creates a knockback effect with a fixed distance.
    /// </summary>
    public KnockbackEffect(int distance)
    {
        Distance = distance > 0 ? distance : 1;
    }

    /// <summary>
    /// Creates a knockback effect from a unified effect definition.
    /// </summary>
    public KnockbackEffect(EffectDefinition definition)
    {
        Distance = definition.Amount > 0 ? definition.Amount : 1;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;

        // Knockback requires a caster to determine direction
        if (context.Caster == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} cannot be knocked back without a source.",
                Palette.ToHex(Palette.Disabled)
            );
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

        // If target is at same position as caster (shouldn't happen), pick a random direction
        if (dirX == 0 && dirY == 0)
        {
            dirX = GD.RandRange(-1, 1);
            dirY = GD.RandRange(-1, 1);
            if (dirX == 0 && dirY == 0)
                dirX = 1; // Fallback
        }

        // Calculate final distance with scaling
        int finalDistance = CalculateScaledAmount(Distance, null, ScalingStat, ScalingMultiplier, context);

        if (finalDistance <= 0)
        {
            return EffectResult.CreateFailure(
                $"{targetName} is unmoved.",
                Palette.ToHex(Palette.Disabled)
            );
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
            return EffectResult.CreateFailure(
                $"{targetName} cannot be pushed!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Move the target to the final position
        target.SetGridPosition(currentPos);

        string message = tilesKnocked == 1
            ? $"{targetName} is knocked back!"
            : $"{targetName} is knocked back {tilesKnocked} tiles!";

        return EffectResult.CreateSuccess(
            message,
            Palette.ToHex(Palette.CombatDamage),
            target
        );
    }
}
