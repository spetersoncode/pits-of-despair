using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Defines the visual and behavioral properties of a visual effect type.
/// Immutable configuration that can be shared across multiple effect instances.
/// Supports stationary effects (explosions, beams) and moving effects (projectiles).
/// </summary>
public class VisualEffectDefinition
{
    /// <summary>
    /// Unique identifier for this effect type.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The category of visual effect (determines rendering behavior).
    /// </summary>
    public VisualEffectType Type { get; }

    /// <summary>
    /// Path to the shader resource file.
    /// </summary>
    public string ShaderPath { get; }

    /// <summary>
    /// Default duration of the effect in seconds.
    /// For projectiles, this is ignored (duration is calculated from speed and distance).
    /// </summary>
    public float Duration { get; }

    /// <summary>
    /// Inner/core color of the effect (for radial/beam effects).
    /// For projectiles, this is the head color.
    /// </summary>
    public Color InnerColor { get; }

    /// <summary>
    /// Mid-range color of the effect.
    /// For projectiles, this is used as the trail color if TrailColor is not set.
    /// </summary>
    public Color MidColor { get; }

    /// <summary>
    /// Outer/edge color of the effect.
    /// </summary>
    public Color OuterColor { get; }

    /// <summary>
    /// Additional shader parameters specific to this effect type.
    /// Keys are shader uniform names, values are the uniform values.
    /// </summary>
    public IReadOnlyDictionary<string, Variant> ShaderParams { get; }

    #region Projectile-Specific Properties

    /// <summary>
    /// Speed in tiles per second. Only used for Projectile type effects.
    /// </summary>
    public float Speed { get; }

    /// <summary>
    /// Number of trail segments for the shader to render. Only used for Projectile type effects.
    /// </summary>
    public int TrailLength { get; }

    /// <summary>
    /// Size multiplier for the projectile visual. Only used for Projectile type effects.
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// Explicit trail color for projectiles. If null, uses InnerColor with reduced alpha.
    /// </summary>
    public Color? TrailColor { get; }

    #endregion

    /// <summary>
    /// Creates a stationary effect definition (explosion, beam, etc.).
    /// </summary>
    public VisualEffectDefinition(
        string id,
        VisualEffectType type,
        string shaderPath,
        float duration,
        Color innerColor,
        Color midColor,
        Color outerColor,
        Dictionary<string, Variant>? shaderParams = null)
    {
        Id = id;
        Type = type;
        ShaderPath = shaderPath;
        Duration = duration;
        InnerColor = innerColor;
        MidColor = midColor;
        OuterColor = outerColor;
        ShaderParams = shaderParams ?? new Dictionary<string, Variant>();
        // Projectile defaults
        Speed = 25.0f;
        TrailLength = 3;
        Size = 1.0f;
        TrailColor = null;
    }

    /// <summary>
    /// Creates a projectile effect definition.
    /// </summary>
    public VisualEffectDefinition(
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
        Type = VisualEffectType.Projectile;
        ShaderPath = shaderPath;
        Duration = 0.0f; // Calculated from speed and distance
        InnerColor = headColor;
        MidColor = trailColor ?? new Color(headColor.R, headColor.G, headColor.B, headColor.A * 0.5f);
        OuterColor = trailColor ?? new Color(headColor.R, headColor.G, headColor.B, headColor.A * 0.3f);
        ShaderParams = shaderParams ?? new Dictionary<string, Variant>();
        Speed = speed;
        TrailLength = trailLength;
        Size = size;
        TrailColor = trailColor;
    }

    /// <summary>
    /// Gets the effective trail color for projectiles, defaulting to head color with reduced alpha.
    /// </summary>
    public Color GetTrailColor()
    {
        if (TrailColor.HasValue)
            return TrailColor.Value;

        return new Color(InnerColor.R, InnerColor.G, InnerColor.B, InnerColor.A * 0.5f);
    }

    /// <summary>
    /// Creates a copy of this definition with different colors.
    /// Useful for creating color variants of the same effect.
    /// </summary>
    public VisualEffectDefinition WithColors(Color innerColor, Color midColor, Color outerColor)
    {
        if (Type == VisualEffectType.Projectile)
        {
            return new VisualEffectDefinition(
                Id,
                ShaderPath,
                innerColor,
                midColor,
                Speed,
                TrailLength,
                Size,
                new Dictionary<string, Variant>(ShaderParams as Dictionary<string, Variant> ?? new()));
        }

        return new VisualEffectDefinition(
            Id,
            Type,
            ShaderPath,
            Duration,
            innerColor,
            midColor,
            outerColor,
            new Dictionary<string, Variant>(ShaderParams as Dictionary<string, Variant> ?? new()));
    }

    /// <summary>
    /// Creates a copy of this definition with a different duration.
    /// For projectiles, this adjusts speed to achieve the desired duration over a standard distance.
    /// </summary>
    public VisualEffectDefinition WithDuration(float duration)
    {
        return new VisualEffectDefinition(
            Id,
            Type,
            ShaderPath,
            duration,
            InnerColor,
            MidColor,
            OuterColor,
            new Dictionary<string, Variant>(ShaderParams as Dictionary<string, Variant> ?? new()));
    }

    /// <summary>
    /// Creates a copy of this projectile definition with a different speed.
    /// </summary>
    public VisualEffectDefinition WithSpeed(float speed)
    {
        if (Type != VisualEffectType.Projectile)
            return this;

        return new VisualEffectDefinition(
            Id,
            ShaderPath,
            InnerColor,
            TrailColor,
            speed,
            TrailLength,
            Size,
            new Dictionary<string, Variant>(ShaderParams as Dictionary<string, Variant> ?? new()));
    }
}
