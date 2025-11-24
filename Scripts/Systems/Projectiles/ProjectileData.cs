using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems.Projectiles;

/// <summary>
/// Represents the runtime state of a projectile in flight.
/// </summary>
public class ProjectileData
{
    /// <summary>
    /// Starting grid position of the projectile.
    /// </summary>
    public GridPosition Origin { get; }

    /// <summary>
    /// Target grid position the projectile is traveling toward.
    /// </summary>
    public GridPosition Target { get; }

    /// <summary>
    /// Target entity (can be null for area effects).
    /// </summary>
    public BaseEntity? TargetEntity { get; }

    /// <summary>
    /// Entity that fired the projectile.
    /// </summary>
    public BaseEntity? Caster { get; }

    /// <summary>
    /// Visual definition for this projectile (shape, color, speed, etc.).
    /// </summary>
    public ProjectileDefinition Definition { get; }

    /// <summary>
    /// Animation progress from 0.0 (at origin) to 1.0 (at target).
    /// </summary>
    public float Progress { get; set; } = 0.0f;

    /// <summary>
    /// Effect to apply when projectile reaches target.
    /// Null for visual-only projectiles.
    /// </summary>
    public Effect? DeferredEffect { get; }

    /// <summary>
    /// Effect context for applying the deferred effect.
    /// Null for visual-only projectiles.
    /// </summary>
    public EffectContext? DeferredEffectContext { get; }

    /// <summary>
    /// Index of the attack for ranged weapon projectiles.
    /// -1 for skill/effect projectiles.
    /// </summary>
    public int AttackIndex { get; }

    /// <summary>
    /// Historical positions for trail rendering.
    /// Stores recent positions for drawing fading trail segments.
    /// </summary>
    public List<Vector2> TrailPositions { get; } = new();

    /// <summary>
    /// Maximum number of trail positions to store (based on definition).
    /// </summary>
    public int MaxTrailLength => Definition.TrailLength;

    /// <summary>
    /// Whether this projectile has a deferred effect to apply on impact.
    /// </summary>
    public bool HasDeferredEffect => DeferredEffect != null && DeferredEffectContext != null;

    /// <summary>
    /// Callback to execute on impact (for AOE effects that need custom handling).
    /// </summary>
    public System.Action? OnImpactCallback { get; set; }

    /// <summary>
    /// Whether this projectile has an impact callback.
    /// </summary>
    public bool HasImpactCallback => OnImpactCallback != null;

    /// <summary>
    /// The visual node used for shader-based rendering.
    /// Managed by ProjectileSystem.
    /// </summary>
    public ColorRect? ShaderNode { get; set; }

    /// <summary>
    /// The shader material applied to this projectile.
    /// </summary>
    public ShaderMaterial? Material { get; set; }

    /// <summary>
    /// Creates a projectile for skill/effect with deferred effect application.
    /// </summary>
    public ProjectileData(
        GridPosition origin,
        GridPosition target,
        ProjectileDefinition definition,
        BaseEntity? caster = null,
        BaseEntity? targetEntity = null,
        Effect? deferredEffect = null,
        EffectContext? deferredEffectContext = null)
    {
        Origin = origin;
        Target = target;
        Definition = definition;
        Caster = caster;
        TargetEntity = targetEntity;
        DeferredEffect = deferredEffect;
        DeferredEffectContext = deferredEffectContext;
        AttackIndex = -1;
    }

    /// <summary>
    /// Creates a projectile for ranged weapon attacks.
    /// </summary>
    public ProjectileData(
        GridPosition origin,
        GridPosition target,
        ProjectileDefinition definition,
        BaseEntity attacker,
        BaseEntity? targetEntity,
        int attackIndex)
    {
        Origin = origin;
        Target = target;
        Definition = definition;
        Caster = attacker;
        TargetEntity = targetEntity;
        AttackIndex = attackIndex;
    }

    /// <summary>
    /// Gets the current interpolated position based on progress.
    /// Returns world position as a Vector2 (with fractional tile coordinates).
    /// </summary>
    public Vector2 GetCurrentPosition()
    {
        return new Vector2(
            Mathf.Lerp(Origin.X, Target.X, Progress),
            Mathf.Lerp(Origin.Y, Target.Y, Progress)
        );
    }

    /// <summary>
    /// Gets the direction vector from origin to target (normalized).
    /// </summary>
    public Vector2 GetDirection()
    {
        Vector2 direction = new Vector2(
            Target.X - Origin.X,
            Target.Y - Origin.Y
        );
        return direction.Length() > 0 ? direction.Normalized() : Vector2.Right;
    }

    /// <summary>
    /// Gets the speed for this projectile from its definition.
    /// </summary>
    public float GetSpeed() => Definition.Speed;

    /// <summary>
    /// Gets the head color for this projectile from its definition.
    /// </summary>
    public Color GetHeadColor() => Definition.HeadColor;

    /// <summary>
    /// Updates the trail positions based on current position.
    /// Should be called each frame during animation.
    /// </summary>
    public void UpdateTrail()
    {
        if (MaxTrailLength <= 0)
            return;

        Vector2 currentPos = GetCurrentPosition();

        // Add current position to trail
        TrailPositions.Insert(0, currentPos);

        // Trim trail to max length
        while (TrailPositions.Count > MaxTrailLength)
        {
            TrailPositions.RemoveAt(TrailPositions.Count - 1);
        }
    }
}
