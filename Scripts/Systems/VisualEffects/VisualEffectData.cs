using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Runtime data for an active visual effect.
/// </summary>
public class VisualEffectData
{
    /// <summary>
    /// The type of visual effect.
    /// </summary>
    public VisualEffectType Type { get; }

    /// <summary>
    /// Center position of the effect in grid coordinates.
    /// </summary>
    public GridPosition Position { get; }

    /// <summary>
    /// Animation progress from 0.0 (start) to 1.0 (complete).
    /// </summary>
    public float Progress { get; set; }

    /// <summary>
    /// Duration of the effect in seconds.
    /// </summary>
    public float Duration { get; }

    /// <summary>
    /// Primary color for the effect.
    /// </summary>
    public Color PrimaryColor { get; }

    /// <summary>
    /// Secondary color for the effect (e.g., fade color).
    /// </summary>
    public Color SecondaryColor { get; }

    /// <summary>
    /// Radius of the effect in tiles (for explosions, etc.).
    /// </summary>
    public float Radius { get; }

    /// <summary>
    /// Target position for beam effects (end point of the line).
    /// </summary>
    public GridPosition? TargetPosition { get; }

    /// <summary>
    /// Rotation angle in radians for beam effects.
    /// </summary>
    public float Rotation { get; }

    /// <summary>
    /// Length of the beam in pixels.
    /// </summary>
    public float BeamLength { get; }

    /// <summary>
    /// Whether this effect has completed its animation.
    /// </summary>
    public bool IsComplete => Progress >= 1.0f;

    /// <summary>
    /// The visual node used for shader-based rendering.
    /// Managed by VisualEffectSystem.
    /// </summary>
    public ColorRect? ShaderNode { get; set; }

    /// <summary>
    /// The shader material applied to this effect.
    /// </summary>
    public ShaderMaterial? Material { get; set; }

    /// <summary>
    /// Creates a radial effect (explosion, heal, etc.).
    /// </summary>
    public VisualEffectData(
        VisualEffectType type,
        GridPosition position,
        float duration,
        Color primaryColor,
        Color? secondaryColor = null,
        float radius = 1.0f)
    {
        Type = type;
        Position = position;
        Duration = duration;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor ?? primaryColor.Darkened(0.5f);
        Radius = radius;
        Progress = 0.0f;
        TargetPosition = null;
        Rotation = 0.0f;
        BeamLength = 0.0f;
    }

    /// <summary>
    /// Creates a beam effect from origin to target.
    /// </summary>
    public VisualEffectData(
        GridPosition origin,
        GridPosition target,
        float duration,
        Color primaryColor,
        Color? secondaryColor,
        float beamLength,
        float rotation)
    {
        Type = VisualEffectType.Beam;
        Position = origin;
        TargetPosition = target;
        Duration = duration;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor ?? primaryColor.Darkened(0.5f);
        Radius = 0.0f;
        BeamLength = beamLength;
        Rotation = rotation;
        Progress = 0.0f;
    }
}
