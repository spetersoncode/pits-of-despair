using Godot;
using System;
using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Runtime data for an active visual effect.
/// Supports both stationary effects (explosions, beams) and moving effects (projectiles).
/// </summary>
public class VisualEffectData
{
    /// <summary>
    /// The type of visual effect.
    /// </summary>
    public VisualEffectType Type { get; }

    /// <summary>
    /// Center position of the effect in grid coordinates.
    /// For projectiles, this is the origin position.
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
    /// Target position for beam and projectile effects.
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
    /// Callback to execute when this effect completes.
    /// Used for triggering game logic after projectile arrival, etc.
    /// </summary>
    public Action? OnCompleteCallback { get; set; }

    #region Projectile-Specific Properties

    /// <summary>
    /// Historical positions for trail rendering.
    /// Stores recent positions for drawing fading trail segments.
    /// </summary>
    public List<Vector2> TrailPositions { get; } = new();

    /// <summary>
    /// Maximum number of trail positions to store.
    /// </summary>
    public int MaxTrailLength { get; }

    /// <summary>
    /// Speed in tiles per second for projectiles.
    /// </summary>
    public float Speed { get; }

    /// <summary>
    /// Size multiplier for projectile visuals.
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// The effect definition used to create this effect.
    /// </summary>
    public VisualEffectDefinition? Definition { get; }

    #endregion

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
        // Projectile defaults
        MaxTrailLength = 0;
        Speed = 0.0f;
        Size = 1.0f;
        Definition = null;
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
        // Projectile defaults
        MaxTrailLength = 0;
        Speed = 0.0f;
        Size = 1.0f;
        Definition = null;
    }

    /// <summary>
    /// Creates a projectile effect from origin to target.
    /// </summary>
    public VisualEffectData(
        GridPosition origin,
        GridPosition target,
        VisualEffectDefinition definition,
        float duration,
        Action? onCompleteCallback = null)
    {
        Type = VisualEffectType.Projectile;
        Position = origin;
        TargetPosition = target;
        Duration = duration;
        PrimaryColor = definition.InnerColor;
        SecondaryColor = definition.GetTrailColor();
        Radius = 0.0f;
        BeamLength = 0.0f;
        Rotation = 0.0f;
        Progress = 0.0f;
        // Projectile properties
        MaxTrailLength = definition.TrailLength;
        Speed = definition.Speed;
        Size = definition.Size;
        Definition = definition;
        OnCompleteCallback = onCompleteCallback;
    }

    /// <summary>
    /// Gets the current interpolated position based on progress (for projectiles).
    /// Returns world position as a Vector2 (with fractional tile coordinates).
    /// </summary>
    public Vector2 GetCurrentPosition()
    {
        if (TargetPosition == null)
            return new Vector2(Position.X, Position.Y);

        return new Vector2(
            Mathf.Lerp(Position.X, TargetPosition.Value.X, Progress),
            Mathf.Lerp(Position.Y, TargetPosition.Value.Y, Progress)
        );
    }

    /// <summary>
    /// Gets the direction vector from origin to target (normalized).
    /// </summary>
    public Vector2 GetDirection()
    {
        if (TargetPosition == null)
            return Vector2.Right;

        Vector2 direction = new Vector2(
            TargetPosition.Value.X - Position.X,
            TargetPosition.Value.Y - Position.Y
        );
        return direction.Length() > 0 ? direction.Normalized() : Vector2.Right;
    }

    /// <summary>
    /// Updates the trail positions based on current position.
    /// Should be called each frame during animation for projectiles.
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
