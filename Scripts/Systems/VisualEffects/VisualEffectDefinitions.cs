using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Static catalog of predefined visual effect types.
/// Effects reference these definitions for consistent visual behavior.
/// All colors are sourced from Palette for visual consistency.
/// </summary>
public static class VisualEffectDefinitions
{
    #region Impact Effects

    /// <summary>
    /// Fireball impact - dramatic fire explosion with expanding rings.
    /// </summary>
    public static readonly VisualEffectDefinition Fireball = new(
        id: "fireball",
        type: VisualEffectType.Explosion,
        shaderPath: "res://Resources/Shaders/Impacts/fireball.gdshader",
        duration: 0.6f,
        innerColor: new Color(1.0f, 1.0f, 0.85f, 1.0f),  // Hot white core
        midColor: new Color(
            Mathf.Min(Palette.Fire.R * 1.2f, 1.0f),
            Palette.Fire.G,
            Palette.Fire.B * 0.5f,
            1.0f),  // Bright orange
        outerColor: new Color(0.85f, 0.15f, 0.05f, 1.0f)  // Deep red/crimson
    );

    #endregion

    #region Beam Effects

    /// <summary>
    /// Tunneling beam - earthy beam for wall destruction effects.
    /// </summary>
    public static readonly VisualEffectDefinition Tunneling = new(
        id: "tunneling",
        type: VisualEffectType.Beam,
        shaderPath: "res://Resources/Shaders/Beams/tunneling.gdshader",
        duration: 0.5f,
        innerColor: new Color(1.0f, 0.9f, 0.7f, 1.0f),  // Hot white-yellow core
        midColor: Palette.Ochre,
        outerColor: Palette.Ochre.Darkened(0.4f)
    );

    #endregion

    /// <summary>
    /// Gets a visual effect definition by ID.
    /// Returns null if not found.
    /// </summary>
    public static VisualEffectDefinition? GetById(string id)
    {
        return id?.ToLower() switch
        {
            "fireball" => Fireball,
            "tunneling" => Tunneling,
            _ => null
        };
    }

    /// <summary>
    /// Gets all available visual effect definitions.
    /// </summary>
    public static IEnumerable<VisualEffectDefinition> GetAll()
    {
        yield return Fireball;
        yield return Tunneling;
    }
}
