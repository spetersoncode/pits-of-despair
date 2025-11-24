using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Defines the visual and behavioral properties of a visual effect type.
/// Immutable configuration that can be shared across multiple effect instances.
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
    /// </summary>
    public float Duration { get; }

    /// <summary>
    /// Inner/core color of the effect.
    /// </summary>
    public Color InnerColor { get; }

    /// <summary>
    /// Mid-range color of the effect.
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
    }

    /// <summary>
    /// Creates a copy of this definition with different colors.
    /// Useful for creating color variants of the same effect.
    /// </summary>
    public VisualEffectDefinition WithColors(Color innerColor, Color midColor, Color outerColor)
    {
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
}
