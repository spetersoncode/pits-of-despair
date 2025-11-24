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
    private Dictionary<string, Shader> _loadedShaders = new();
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
        // Pre-load shaders for all defined effects
        foreach (var definition in VisualEffectDefinitions.GetAll())
        {
            LoadShader(definition.ShaderPath);
        }
    }

    /// <summary>
    /// Loads and caches a shader from the given path.
    /// </summary>
    private Shader? LoadShader(string shaderPath)
    {
        if (_loadedShaders.TryGetValue(shaderPath, out var cached))
        {
            return cached;
        }

        var shader = GD.Load<Shader>(shaderPath);
        if (shader == null)
        {
            GD.PrintErr($"VisualEffectSystem: Failed to load shader: {shaderPath}");
            return null;
        }

        _loadedShaders[shaderPath] = shader;
        return shader;
    }

    /// <summary>
    /// Gets a cached shader or loads it if not yet cached.
    /// </summary>
    private Shader? GetShader(string shaderPath)
    {
        if (_loadedShaders.TryGetValue(shaderPath, out var shader))
        {
            return shader;
        }
        return LoadShader(shaderPath);
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

    #region Public Spawn Methods

    /// <summary>
    /// Spawns a visual effect using a definition ID.
    /// </summary>
    /// <param name="definitionId">The ID of the effect definition to use.</param>
    /// <param name="position">Center position in grid coordinates.</param>
    /// <param name="radius">Radius in tiles (for radial effects).</param>
    /// <param name="target">Target position (for beam effects).</param>
    /// <param name="durationOverride">Optional duration override.</param>
    public void SpawnEffect(
        string definitionId,
        GridPosition position,
        float radius = 1.0f,
        GridPosition? target = null,
        float? durationOverride = null)
    {
        var definition = VisualEffectDefinitions.GetById(definitionId);
        if (definition == null)
        {
            GD.PrintErr($"VisualEffectSystem: Unknown effect definition: {definitionId}");
            return;
        }

        SpawnEffect(definition, position, radius, target, durationOverride);
    }

    /// <summary>
    /// Spawns a visual effect using a definition.
    /// </summary>
    public void SpawnEffect(
        VisualEffectDefinition definition,
        GridPosition position,
        float radius = 1.0f,
        GridPosition? target = null,
        float? durationOverride = null)
    {
        if (_renderer == null) return;

        float duration = durationOverride ?? definition.Duration;

        if (definition.Type == VisualEffectType.Beam && target.HasValue)
        {
            SpawnBeamEffect(definition, position, target.Value, duration);
        }
        else
        {
            SpawnRadialEffect(definition, position, radius, duration);
        }
    }

    /// <summary>
    /// Spawns a fireball impact at the specified position.
    /// </summary>
    public void SpawnFireballImpact(GridPosition position, float radius, float? duration = null)
    {
        SpawnEffect(VisualEffectDefinitions.Fireball, position, radius, null, duration);
    }

    /// <summary>
    /// Spawns a tunneling beam from origin to target.
    /// </summary>
    public void SpawnTunnelingBeam(GridPosition origin, GridPosition target, float? duration = null)
    {
        SpawnEffect(VisualEffectDefinitions.Tunneling, origin, 1.0f, target, duration);
    }

    /// <summary>
    /// Spawns an explosion effect at the specified position.
    /// Legacy method - prefer SpawnFireball.
    /// </summary>
    public void SpawnExplosion(GridPosition position, float radius, Color? color = null, float duration = 0.6f)
    {
        SpawnEffect(VisualEffectDefinitions.Fireball, position, radius, null, duration);
    }

    /// <summary>
    /// Spawns an explosion effect from a Vector2I position.
    /// Legacy method.
    /// </summary>
    public void SpawnExplosion(Vector2I position, float radius, Color? color = null, float duration = 0.6f)
    {
        SpawnExplosion(GridPosition.FromVector2I(position), radius, color, duration);
    }

    /// <summary>
    /// Spawns a beam effect traveling from origin to target.
    /// Legacy method - prefer SpawnTunneling.
    /// </summary>
    public void SpawnBeam(GridPosition origin, GridPosition target, Color? color = null, float duration = 0.5f)
    {
        SpawnEffect(VisualEffectDefinitions.Tunneling, origin, 1.0f, target, duration);
    }

    #endregion

    #region Effect Creation

    /// <summary>
    /// Spawns a radial effect (explosion, impact, etc.).
    /// </summary>
    private void SpawnRadialEffect(VisualEffectDefinition definition, GridPosition position, float radius, float duration)
    {
        var effect = new VisualEffectData(
            definition.Type,
            position,
            duration,
            definition.MidColor,
            definition.OuterColor,
            radius);

        CreateRadialShaderNode(effect, definition);
        _activeEffects.Add(effect);
        AnimateEffect(effect);
    }

    /// <summary>
    /// Spawns a beam effect from origin to target.
    /// </summary>
    private void SpawnBeamEffect(VisualEffectDefinition definition, GridPosition origin, GridPosition target, float duration)
    {
        if (_renderer == null) return;

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
            definition.MidColor,
            definition.OuterColor,
            beamLength,
            rotation);

        CreateBeamShaderNode(effect, definition);
        _activeEffects.Add(effect);
        AnimateEffect(effect);
    }

    /// <summary>
    /// Creates a ColorRect with a radial shader for GPU-accelerated rendering.
    /// </summary>
    private void CreateRadialShaderNode(VisualEffectData effect, VisualEffectDefinition definition)
    {
        var shader = GetShader(definition.ShaderPath);
        if (shader == null || _renderer == null) return;

        var material = new ShaderMaterial();
        material.Shader = shader;

        // Set uniforms from definition
        material.SetShaderParameter("progress", 0.0f);
        material.SetShaderParameter("radius", effect.Radius * _tileSize);
        material.SetShaderParameter("inner_color", definition.InnerColor);
        material.SetShaderParameter("mid_color", definition.MidColor);
        material.SetShaderParameter("outer_color", definition.OuterColor);

        // Apply any additional shader parameters
        foreach (var param in definition.ShaderParams)
        {
            material.SetShaderParameter(param.Key, param.Value);
        }

        var colorRect = new ColorRect();
        colorRect.Material = material;

        // Size the rect to match the effect area
        float sizePixels = (effect.Radius * 2 + 1) * _tileSize * 1.2f;
        colorRect.Size = new Vector2(sizePixels, sizePixels);
        colorRect.ZIndex = 100;

        effect.ShaderNode = colorRect;
        effect.Material = material;

        _renderer.AddChild(colorRect);
        UpdateRadialNodePosition(effect);
    }

    /// <summary>
    /// Creates a ColorRect with a beam shader for GPU-accelerated rendering.
    /// </summary>
    private void CreateBeamShaderNode(VisualEffectData effect, VisualEffectDefinition definition)
    {
        var shader = GetShader(definition.ShaderPath);
        if (shader == null || _renderer == null) return;

        var material = new ShaderMaterial();
        material.Shader = shader;

        // Set uniforms from definition
        material.SetShaderParameter("progress", 0.0f);
        material.SetShaderParameter("beam_length", effect.BeamLength);
        material.SetShaderParameter("beam_width", 6.0f);
        material.SetShaderParameter("core_color", definition.InnerColor);
        material.SetShaderParameter("mid_color", definition.MidColor);
        material.SetShaderParameter("outer_color", definition.OuterColor);

        // Apply any additional shader parameters
        foreach (var param in definition.ShaderParams)
        {
            material.SetShaderParameter(param.Key, param.Value);
        }

        var colorRect = new ColorRect();
        colorRect.Material = material;

        // Size the rect: width is beam length, height is beam width with extra for glow
        float beamHeight = 24.0f;
        colorRect.Size = new Vector2(effect.BeamLength + 20.0f, beamHeight);

        // Apply rotation around origin
        colorRect.PivotOffset = new Vector2(0, beamHeight / 2.0f);
        colorRect.Rotation = effect.Rotation;
        colorRect.ZIndex = 100;

        effect.ShaderNode = colorRect;
        effect.Material = material;

        _renderer.AddChild(colorRect);
        UpdateBeamNodePosition(effect);
    }

    #endregion

    #region Position Updates

    /// <summary>
    /// Updates the radial shader node position based on camera/scroll offset.
    /// </summary>
    private void UpdateRadialNodePosition(VisualEffectData effect)
    {
        if (effect.ShaderNode == null || _renderer == null) return;

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

    #endregion

    #region Animation

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
            UpdateRadialNodePosition(effect);
        }

        _renderer?.QueueRedraw();
    }

    /// <summary>
    /// Called when an effect completes its animation.
    /// </summary>
    private void OnEffectComplete(VisualEffectData effect)
    {
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

    #endregion

    #region Cleanup

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

    #endregion
}
