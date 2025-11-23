using Godot;

namespace PitsOfDespair.Systems.Projectiles;

/// <summary>
/// Visual shape types for projectile rendering.
/// All shapes use Godot drawing primitives for smooth sub-tile positioning.
/// </summary>
public enum ProjectileShape
{
    /// <summary>Line segment with optional trail (arrows, beams, rays)</summary>
    Line,
    /// <summary>Filled circle (fireballs, orbs, energy balls)</summary>
    Circle,
    /// <summary>Diamond/rhombus shape (magic bolts, shards)</summary>
    Diamond,
    /// <summary>Triangle pointing in direction of travel (directional projectiles)</summary>
    Triangle
}

/// <summary>
/// Defines the visual and behavioral properties of a projectile type.
/// Immutable configuration that can be shared across multiple projectile instances.
/// </summary>
public class ProjectileDefinition
{
    /// <summary>
    /// Unique identifier for this projectile type.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The visual shape used to render the projectile head.
    /// </summary>
    public ProjectileShape Shape { get; }

    /// <summary>
    /// Color of the projectile head (leading edge).
    /// </summary>
    public Color HeadColor { get; }

    /// <summary>
    /// Color of the projectile trail (fades from this color).
    /// If null, uses HeadColor with reduced alpha.
    /// </summary>
    public Color? TrailColor { get; }

    /// <summary>
    /// Speed in tiles per second. Higher = faster travel.
    /// </summary>
    public float Speed { get; }

    /// <summary>
    /// Number of trail segments to render behind the projectile head.
    /// 0 = no trail, higher = longer trail.
    /// </summary>
    public int TrailLength { get; }

    /// <summary>
    /// Size of the projectile shape in pixels.
    /// Interpretation varies by shape (radius for circle, side length for diamond/triangle).
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// Line thickness for Line shape projectiles.
    /// </summary>
    public float LineWidth { get; }

    public ProjectileDefinition(
        string id,
        ProjectileShape shape,
        Color headColor,
        Color? trailColor = null,
        float speed = 25.0f,
        int trailLength = 3,
        float size = 6.0f,
        float lineWidth = 3.0f)
    {
        Id = id;
        Shape = shape;
        HeadColor = headColor;
        TrailColor = trailColor;
        Speed = speed;
        TrailLength = trailLength;
        Size = size;
        LineWidth = lineWidth;
    }

    /// <summary>
    /// Gets the effective trail color, defaulting to head color with reduced alpha.
    /// </summary>
    public Color GetTrailColor()
    {
        if (TrailColor.HasValue)
            return TrailColor.Value;

        return new Color(HeadColor.R, HeadColor.G, HeadColor.B, HeadColor.A * 0.5f);
    }

    /// <summary>
    /// Gets the trail color at a specific segment index (for fading trails).
    /// </summary>
    /// <param name="segmentIndex">0 = closest to head, higher = further back</param>
    /// <param name="totalSegments">Total number of trail segments</param>
    public Color GetTrailColorAtSegment(int segmentIndex, int totalSegments)
    {
        if (totalSegments <= 0)
            return GetTrailColor();

        // Calculate fade factor: 1.0 at head, approaching 0 at tail
        float fadeFactor = 1.0f - ((float)segmentIndex / totalSegments);
        Color baseTrail = GetTrailColor();

        return new Color(baseTrail.R, baseTrail.G, baseTrail.B, baseTrail.A * fadeFactor);
    }
}
