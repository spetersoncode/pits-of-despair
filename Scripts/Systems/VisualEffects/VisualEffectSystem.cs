using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Manages visual effects like explosions, healing glows, and other animated overlays.
/// Effects are purely visual and don't affect gameplay.
/// Uses GPU shaders for high-impact visual effects.
/// </summary>
public partial class VisualEffectSystem : Node
{
    [Signal]
    public delegate void AllEffectsCompletedEventHandler();

    private List<VisualEffectData> _activeEffects = new();
    private TextRenderer _renderer;
    private Shader? _explosionShader;
    private Shader? _beamShader;
    private int _tileSize = 18;

    /// <summary>
    /// Gets all currently active visual effects for rendering.
    /// </summary>
    public IReadOnlyList<VisualEffectData> ActiveEffects => _activeEffects.AsReadOnly();

    /// <summary>
    /// Returns true if there are any effects currently animating.
    /// </summary>
    public bool HasActiveEffects => _activeEffects.Count > 0;

    public override void _Ready()
    {
        // Load the explosion shader
        _explosionShader = GD.Load<Shader>("res://Resources/Shaders/explosion.gdshader");
        if (_explosionShader == null)
        {
            GD.PrintErr("VisualEffectSystem: Failed to load explosion shader");
        }

        // Load the beam shader
        _beamShader = GD.Load<Shader>("res://Resources/Shaders/beam.gdshader");
        if (_beamShader == null)
        {
            GD.PrintErr("VisualEffectSystem: Failed to load beam shader");
        }
    }

    /// <summary>
    /// Sets the text renderer reference for forcing visual updates and positioning.
    /// </summary>
    public void SetTextRenderer(TextRenderer renderer)
    {
        _renderer = renderer;
        if (renderer != null)
        {
            _tileSize = renderer.TileSize;
        }
    }

    /// <summary>
    /// Spawns an explosion effect at the specified position.
    /// Creates a shader-based visual for maximum impact.
    /// </summary>
    /// <param name="position">Center of the explosion in grid coordinates.</param>
    /// <param name="radius">Radius of the explosion in tiles.</param>
    /// <param name="color">Color of the explosion (defaults to Fire palette).</param>
    /// <param name="duration">Duration of the effect in seconds.</param>
    public void SpawnExplosion(GridPosition position, float radius, Color? color = null, float duration = 0.6f)
    {
        var effectColor = color ?? Palette.Fire;
        var effect = new VisualEffectData(
            VisualEffectType.Explosion,
            position,
            duration,
            effectColor,
            effectColor.Darkened(0.6f),
            radius);

        // Create shader-based visual node
        CreateShaderNode(effect);

        _activeEffects.Add(effect);
        AnimateEffect(effect);
    }

    /// <summary>
    /// Creates a ColorRect with the explosion shader for GPU-accelerated rendering.
    /// </summary>
    private void CreateShaderNode(VisualEffectData effect)
    {
        if (_explosionShader == null || _renderer == null) return;

        // Create shader material
        var material = new ShaderMaterial();
        material.Shader = _explosionShader;

        // Set initial uniforms
        material.SetShaderParameter("progress", 0.0f);
        material.SetShaderParameter("radius", effect.Radius * _tileSize);

        // Set colors based on effect palette - dramatic fire colors
        // Hot inner color (bright white-yellow core)
        material.SetShaderParameter("inner_color", new Color(1.0f, 1.0f, 0.85f, 1.0f));
        // Mid color (bright orange - use primary with boosted saturation)
        var midColor = effect.PrimaryColor;
        midColor = new Color(
            Mathf.Min(midColor.R * 1.2f, 1.0f),
            midColor.G,
            midColor.B * 0.5f,
            1.0f);
        material.SetShaderParameter("mid_color", midColor);
        // Outer color (deep red/crimson)
        material.SetShaderParameter("outer_color", new Color(0.85f, 0.15f, 0.05f, 1.0f));

        // Create the visual node
        var colorRect = new ColorRect();
        colorRect.Material = material;

        // Size the rect to match the explosion area
        // Diameter is (radius * 2 + 1) tiles, with small padding for edge effects
        // The +1 accounts for the center tile
        float sizePixels = (effect.Radius * 2 + 1) * _tileSize * 1.2f;
        colorRect.Size = new Vector2(sizePixels, sizePixels);

        // Position will be updated in UpdateShaderNodePosition
        colorRect.ZIndex = 100; // Render above game elements

        // Store references
        effect.ShaderNode = colorRect;
        effect.Material = material;

        // Add to renderer as child so it renders in the same coordinate space
        _renderer.AddChild(colorRect);

        // Update position initially
        UpdateShaderNodePosition(effect);
    }

    /// <summary>
    /// Updates the shader node position based on camera/scroll offset.
    /// </summary>
    private void UpdateShaderNodePosition(VisualEffectData effect)
    {
        if (effect.ShaderNode == null || _renderer == null) return;

        // Get position using renderer's centralized conversion
        var offset = _renderer.GetRenderOffset();
        var tileCenter = _renderer.GridToTileCenter(effect.Position);

        // Position the rect so its center is at the effect position
        float halfSize = effect.ShaderNode.Size.X / 2.0f;
        effect.ShaderNode.Position = new Vector2(
            offset.X + tileCenter.X - halfSize,
            offset.Y + tileCenter.Y - halfSize
        );
    }

    /// <summary>
    /// Spawns an explosion effect from a Vector2I position.
    /// </summary>
    public void SpawnExplosion(Vector2I position, float radius, Color? color = null, float duration = 0.6f)
    {
        SpawnExplosion(GridPosition.FromVector2I(position), radius, color, duration);
    }

    /// <summary>
    /// Spawns a beam effect traveling from origin to target.
    /// Creates a shader-based visual for dramatic beam/tunneling effects.
    /// </summary>
    /// <param name="origin">Starting position in grid coordinates.</param>
    /// <param name="target">End position in grid coordinates.</param>
    /// <param name="color">Color of the beam (defaults to Ochre for earth/tunneling).</param>
    /// <param name="duration">Duration of the effect in seconds.</param>
    public void SpawnBeam(GridPosition origin, GridPosition target, Color? color = null, float duration = 0.5f)
    {
        if (_renderer == null) return;

        var effectColor = color ?? Palette.Ochre;

        // Calculate beam geometry
        var originCenter = _renderer.GridToTileCenter(origin);
        var targetCenter = _renderer.GridToTileCenter(target);
        var delta = targetCenter - originCenter;
        float beamLength = delta.Length();
        float rotation = Mathf.Atan2(delta.Y, delta.X);

        var effect = new VisualEffectData(
            origin,
            target,
            duration,
            effectColor,
            effectColor.Darkened(0.4f),
            beamLength,
            rotation);

        // Create shader-based visual node
        CreateBeamShaderNode(effect);

        _activeEffects.Add(effect);
        AnimateEffect(effect);
    }

    /// <summary>
    /// Creates a ColorRect with the beam shader for GPU-accelerated rendering.
    /// </summary>
    private void CreateBeamShaderNode(VisualEffectData effect)
    {
        if (_beamShader == null || _renderer == null) return;

        // Create shader material
        var material = new ShaderMaterial();
        material.Shader = _beamShader;

        // Set initial uniforms
        material.SetShaderParameter("progress", 0.0f);
        material.SetShaderParameter("beam_length", effect.BeamLength);
        material.SetShaderParameter("beam_width", 6.0f);

        // Set colors - tunneling uses earthy tones
        material.SetShaderParameter("core_color", new Color(1.0f, 0.9f, 0.7f, 1.0f));
        material.SetShaderParameter("mid_color", effect.PrimaryColor);
        material.SetShaderParameter("outer_color", effect.SecondaryColor);

        // Create the visual node
        var colorRect = new ColorRect();
        colorRect.Material = material;

        // Size the rect: width is beam length, height is beam width with extra for particles
        float beamWidth = 24.0f; // Height for beam and debris particles
        colorRect.Size = new Vector2(effect.BeamLength + 20.0f, beamWidth);

        // Apply rotation around origin
        colorRect.PivotOffset = new Vector2(0, beamWidth / 2.0f);
        colorRect.Rotation = effect.Rotation;

        colorRect.ZIndex = 100; // Render above game elements

        // Store references
        effect.ShaderNode = colorRect;
        effect.Material = material;

        // Add to renderer
        _renderer.AddChild(colorRect);

        // Update position initially
        UpdateBeamNodePosition(effect);
    }

    /// <summary>
    /// Updates the beam shader node position based on camera/scroll offset.
    /// </summary>
    private void UpdateBeamNodePosition(VisualEffectData effect)
    {
        if (effect.ShaderNode == null || _renderer == null) return;

        var offset = _renderer.GetRenderOffset();
        var originCenter = _renderer.GridToTileCenter(effect.Position);

        // Position at beam origin, centered vertically
        float halfHeight = effect.ShaderNode.Size.Y / 2.0f;
        effect.ShaderNode.Position = new Vector2(
            offset.X + originCenter.X,
            offset.Y + originCenter.Y - halfHeight
        );
    }

    /// <summary>
    /// Animates an effect using a tween.
    /// </summary>
    private void AnimateEffect(VisualEffectData effect)
    {
        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(progress => UpdateEffectProgress(effect, progress)),
            0.0f,
            1.0f,
            effect.Duration);
        tween.TweenCallback(Callable.From(() => OnEffectComplete(effect)));
    }

    /// <summary>
    /// Updates the progress of an effect during animation.
    /// </summary>
    private void UpdateEffectProgress(VisualEffectData effect, float progress)
    {
        effect.Progress = progress;

        // Update shader uniform
        if (effect.Material != null)
        {
            effect.Material.SetShaderParameter("progress", progress);
        }

        // Update position in case camera moved
        if (effect.Type == VisualEffectType.Beam)
        {
            UpdateBeamNodePosition(effect);
        }
        else
        {
            UpdateShaderNodePosition(effect);
        }

        _renderer?.QueueRedraw();
    }

    /// <summary>
    /// Called when an effect completes its animation.
    /// </summary>
    private void OnEffectComplete(VisualEffectData effect)
    {
        // Clean up shader node
        if (effect.ShaderNode != null)
        {
            effect.ShaderNode.QueueFree();
            effect.ShaderNode = null;
        }
        effect.Material = null;

        _activeEffects.Remove(effect);
        _renderer?.QueueRedraw();

        if (_activeEffects.Count == 0)
        {
            EmitSignal(SignalName.AllEffectsCompleted);
        }
    }

    /// <summary>
    /// Clears all active effects (useful for scene transitions).
    /// </summary>
    public void ClearAllEffects()
    {
        foreach (var effect in _activeEffects)
        {
            if (effect.ShaderNode != null)
            {
                effect.ShaderNode.QueueFree();
                effect.ShaderNode = null;
            }
            effect.Material = null;
        }
        _activeEffects.Clear();
        _renderer?.QueueRedraw();
    }

    /// <summary>
    /// Gets rendering data for an explosion effect.
    /// Returns multiple rings with calculated radii and alpha values.
    /// </summary>
    /// <param name="effect">The explosion effect data.</param>
    /// <param name="tileSize">Size of a tile in pixels.</param>
    /// <returns>List of (radius, color) tuples for each ring to draw.</returns>
    public static List<(float radius, Color color)> GetExplosionRings(VisualEffectData effect, float tileSize)
    {
        var rings = new List<(float radius, Color color)>();

        // Eased progress for smoother animation
        float easedProgress = EaseOutQuad(effect.Progress);

        // Calculate max radius in pixels
        float maxRadiusPixels = effect.Radius * tileSize;

        // Create 3 expanding rings
        int ringCount = 3;
        for (int i = 0; i < ringCount; i++)
        {
            // Each ring starts slightly later and expands at different rate
            float ringDelay = i * 0.1f;
            float ringProgress = Mathf.Clamp((easedProgress - ringDelay) / (1.0f - ringDelay), 0.0f, 1.0f);

            // Ring expands from 0 to max radius
            float ringRadius = ringProgress * maxRadiusPixels * (1.0f - i * 0.2f);

            // Alpha fades out as ring expands
            float baseAlpha = 0.6f - (i * 0.15f);
            float alpha = baseAlpha * (1.0f - easedProgress);

            if (alpha > 0.01f && ringRadius > 0)
            {
                Color ringColor = new Color(
                    Mathf.Lerp(effect.PrimaryColor.R, effect.SecondaryColor.R, ringProgress),
                    Mathf.Lerp(effect.PrimaryColor.G, effect.SecondaryColor.G, ringProgress),
                    Mathf.Lerp(effect.PrimaryColor.B, effect.SecondaryColor.B, ringProgress),
                    alpha);

                rings.Add((ringRadius, ringColor));
            }
        }

        return rings;
    }

    /// <summary>
    /// Quadratic ease-out function for smooth animation.
    /// </summary>
    private static float EaseOutQuad(float t)
    {
        return 1.0f - (1.0f - t) * (1.0f - t);
    }
}
