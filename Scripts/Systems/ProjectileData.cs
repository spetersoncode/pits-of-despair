using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Represents projectile data for visual rendering.
/// Lightweight data class replacing the entity-based Projectile.
/// </summary>
public class ProjectileData
{
    public GridPosition Origin { get; set; }
    public GridPosition Target { get; set; }
    public BaseEntity TargetEntity { get; set; }
    public BaseEntity Attacker { get; set; }
    public int AttackIndex { get; set; }
    public Color Color { get; set; }

    /// <summary>
    /// Progress of the projectile from 0.0 (origin) to 1.0 (target).
    /// </summary>
    public float Progress { get; set; } = 0.0f;

    public ProjectileData(GridPosition origin, GridPosition target, BaseEntity targetEntity, BaseEntity attacker, int attackIndex, Color? color = null)
    {
        Origin = origin;
        Target = target;
        TargetEntity = targetEntity;
        Attacker = attacker;
        AttackIndex = attackIndex;
        Color = color ?? Colors.White;
    }

    /// <summary>
    /// Gets the current position based on progress.
    /// </summary>
    public Vector2 GetCurrentPosition()
    {
        return new Vector2(
            Mathf.Lerp(Origin.X, Target.X, Progress),
            Mathf.Lerp(Origin.Y, Target.Y, Progress)
        );
    }
}
