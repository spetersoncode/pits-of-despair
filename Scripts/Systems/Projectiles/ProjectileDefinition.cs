using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Systems.Projectiles;

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
    /// Path to the shader resource file.
    /// </summary>
    public string ShaderPath { get; }

    /// <summary>
    /// Primary color of the projectile (head/core).
    /// </summary>
    public Color HeadColor { get; }

    /// <summary>
    /// Color of the projectile trail.
    /// If null, uses HeadColor with reduced alpha.
    /// </summary>
    public Color? TrailColor { get; }

    /// <summary>
    /// Speed in tiles per second. Higher = faster travel.
    /// </summary>
    public float Speed { get; }

    /// <summary>
    /// Number of trail segments for the shader to render.
    /// 0 = no trail, higher = longer trail.
    /// </summary>
    public int TrailLength { get; }

    /// <summary>
    /// Size multiplier for the projectile visual.
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// Additional shader parameters specific to this projectile type.
    /// </summary>
    public IReadOnlyDictionary<string, Variant> ShaderParams { get; }

    public ProjectileDefinition(
        string id,
        string shaderPath,
        Color headColor,
        Color? trailColor = null,
        float speed = 25.0f,
        int trailLength = 3,
        float size = 1.0f,
        Dictionary<string, Variant>? shaderParams = null)
    {
        Id = id;
        ShaderPath = shaderPath;
        HeadColor = headColor;
        TrailColor = trailColor;
        Speed = speed;
        TrailLength = trailLength;
        Size = size;
        ShaderParams = shaderParams ?? new Dictionary<string, Variant>();
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
