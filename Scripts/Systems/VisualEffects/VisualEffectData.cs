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
    }
}
