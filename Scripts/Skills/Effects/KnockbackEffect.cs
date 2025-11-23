using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Skills.Effects;

/// <summary>
/// Skill effect that pushes the target away from the caster.
/// </summary>
public class KnockbackEffect : SkillEffect
{
    public override string Type => "knockback";

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

    public KnockbackEffect(SkillEffectDefinition definition)
    {
        Distance = definition.Amount > 0 ? definition.Amount : 1;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override SkillEffectResult Apply(BaseEntity target, SkillEffectContext context)
    {
        var targetName = target.DisplayName;
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
        int finalDistance = Distance;
        if (!string.IsNullOrEmpty(ScalingStat))
        {
            int statValue = context.GetCasterStat(ScalingStat);
            finalDistance += (int)(statValue * ScalingMultiplier);
        }

        if (finalDistance <= 0)
        {
            return SkillEffectResult.CreateFailure(
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
            return SkillEffectResult.CreateFailure(
                $"{targetName} cannot be pushed!",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Move the target to the final position
        target.SetGridPosition(currentPos);

        string message = tilesKnocked == 1
            ? $"{targetName} is knocked back!"
            : $"{targetName} is knocked back {tilesKnocked} tiles!";

        return SkillEffectResult.CreateSuccess(
            message,
            Palette.ToHex(Palette.CombatDamage),
            target
        );
    }
}
