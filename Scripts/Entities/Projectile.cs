using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Entities;

/// <summary>
/// Represents a projectile (arrow, bolt, etc.) traveling through the air.
/// Animated by ProjectileSystem using tweens.
/// </summary>
public partial class Projectile : BaseEntity
{
    [Signal]
    public delegate void ImpactReachedEventHandler();

    public GridPosition Origin { get; set; }
    public GridPosition Target { get; set; }
    public BaseEntity TargetEntity { get; set; }
    public BaseEntity Attacker { get; set; }
    public int AttackIndex { get; set; }

    /// <summary>
    /// Progress of the projectile from 0.0 (origin) to 1.0 (target).
    /// </summary>
    public float Progress { get; set; } = 0.0f;

    /// <summary>
    /// Creates a new projectile.
    /// </summary>
    /// <param name="origin">Starting grid position</param>
    /// <param name="target">Target grid position</param>
    /// <param name="targetEntity">Target entity (can be null if shooting at empty tile)</param>
    /// <param name="attacker">Entity that fired this projectile</param>
    /// <param name="attackIndex">Index of the attack being used</param>
    /// <param name="glyph">Visual character for the projectile</param>
    /// <param name="color">Color of the projectile</param>
    public Projectile(GridPosition origin, GridPosition target, BaseEntity targetEntity, BaseEntity attacker, int attackIndex, string glyph = "→", Color? color = null)
    {
        Origin = origin;
        Target = target;
        TargetEntity = targetEntity;
        Attacker = attacker;
        AttackIndex = attackIndex;

        // Set visual properties
        Glyph = glyph;
        GlyphColor = color ?? Colors.White;
        DisplayName = "projectile";
        IsWalkable = true;  // Projectiles don't block movement

        // Start at origin
        GridPosition = origin;
    }

    /// <summary>
    /// Updates the projectile's progress along its path.
    /// </summary>
    /// <param name="progress">Value from 0.0 to 1.0</param>
    public void UpdateProgress(float progress)
    {
        Progress = Mathf.Clamp(progress, 0.0f, 1.0f);

        // Interpolate position
        int currentX = (int)Mathf.Lerp(Origin.X, Target.X, Progress);
        int currentY = (int)Mathf.Lerp(Origin.Y, Target.Y, Progress);
        GridPosition = new GridPosition(currentX, currentY);

        // Update glyph based on direction for more visual feedback
        UpdateGlyphDirection();

        // Check if we've reached the target
        if (Progress >= 1.0f)
        {
            EmitSignal(SignalName.ImpactReached);
        }
    }

    /// <summary>
    /// Updates the projectile glyph to point in the direction of travel.
    /// </summary>
    private void UpdateGlyphDirection()
    {
        int dx = Target.X - Origin.X;
        int dy = Target.Y - Origin.Y;

        // Determine primary direction
        if (Mathf.Abs(dx) > Mathf.Abs(dy))
        {
            // Horizontal
            Glyph = dx > 0 ? "→" : "←";
        }
        else
        {
            // Vertical
            Glyph = dy > 0 ? "↓" : "↑";
        }
    }
}
