using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems.Projectiles;

/// <summary>
/// Manages projectiles in flight.
/// Handles spawning, animation, shader-based rendering, impact resolution, and effect application.
/// </summary>
public partial class ProjectileSystem : Node
{
    [Signal]
    public delegate void AllProjectilesCompletedEventHandler();

    /// <summary>
    /// Emitted when a skill projectile deals damage.
    /// </summary>
    [Signal]
    public delegate void SkillDamageDealtEventHandler(Entities.BaseEntity caster, Entities.BaseEntity target, int damage, string skillName);

    private List<ProjectileData> _activeProjectiles = new();
    private Dictionary<string, Shader> _loadedShaders = new();
    private CombatSystem _combatSystem;
    private Player _player;
    private TextRenderer _renderer;
    private int _tileSize = 18;

    /// <summary>
    /// Gets all currently active projectiles for rendering.
    /// </summary>
    public IReadOnlyList<ProjectileData> ActiveProjectiles => _activeProjectiles.AsReadOnly();

    /// <summary>
    /// Returns true if there are any projectiles currently in flight.
    /// </summary>
    public bool HasActiveProjectiles => _activeProjectiles.Count > 0;

    /// <summary>
    /// Initializes the projectile system.
    /// </summary>
    public void Initialize(CombatSystem combatSystem)
    {
        _combatSystem = combatSystem;
    }

    /// <summary>
    /// Connects to the player's ranged attack signal.
    /// </summary>
    public void ConnectToPlayer(Player player)
    {
        _player = player;
        player.Connect(Player.SignalName.RangedAttackRequested, Callable.From<Vector2I, Vector2I, BaseEntity, int>(OnRangedAttackRequested));
    }

    /// <summary>
    /// Sets the text renderer reference for positioning and coordinate conversion.
    /// </summary>
    public void SetTextRenderer(TextRenderer renderer)
    {
        _renderer = renderer;
        if (renderer != null)
        {
            _tileSize = renderer.TileSize;
        }
    }

    #region Shader Management

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
            GD.PrintErr($"ProjectileSystem: Failed to load shader: {shaderPath}");
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

    #endregion

    #region Spawn Methods

    /// <summary>
    /// Spawns a projectile when a ranged attack is requested via player signal.
    /// </summary>
    private void OnRangedAttackRequested(Vector2I origin, Vector2I target, BaseEntity targetEntity, int attackIndex)
    {
        if (_player == null)
        {
            GD.PushError("ProjectileSystem: Player reference is null!");
            return;
        }

        SpawnAttackProjectile(GridPosition.FromVector2I(origin), GridPosition.FromVector2I(target), _player, targetEntity, attackIndex);
    }

    /// <summary>
    /// Spawns a projectile for ranged weapon attacks.
    /// </summary>
    public void SpawnAttackProjectile(
        GridPosition origin,
        GridPosition target,
        BaseEntity attacker,
        BaseEntity? targetEntity,
        int attackIndex,
        ProjectileDefinition? definition = null)
    {
        var projectile = new ProjectileData(
            origin,
            target,
            definition ?? ProjectileDefinitions.Arrow,
            attacker,
            targetEntity,
            attackIndex);

        CreateShaderNode(projectile);
        _activeProjectiles.Add(projectile);
        AnimateProjectile(projectile);
    }

    /// <summary>
    /// Spawns a projectile for ranged weapon attacks (convenience overload for existing code).
    /// </summary>
    public void SpawnProjectile(GridPosition origin, GridPosition target, BaseEntity? targetEntity, BaseEntity attacker, int attackIndex)
    {
        SpawnAttackProjectile(origin, target, attacker, targetEntity, attackIndex);
    }

    /// <summary>
    /// Spawns a skill/effect-based projectile with deferred effect application.
    /// </summary>
    public void SpawnSkillProjectile(
        GridPosition origin,
        GridPosition target,
        ProjectileDefinition definition,
        Effect effect,
        EffectContext effectContext,
        BaseEntity? caster = null,
        BaseEntity? targetEntity = null)
    {
        var projectile = new ProjectileData(
            origin,
            target,
            definition,
            caster,
            targetEntity,
            effect,
            effectContext);

        CreateShaderNode(projectile);
        _activeProjectiles.Add(projectile);
        AnimateProjectile(projectile);
    }

    /// <summary>
    /// Spawns a visual-only projectile (no effect on impact).
    /// </summary>
    public void SpawnVisualProjectile(
        GridPosition origin,
        GridPosition target,
        ProjectileDefinition definition,
        BaseEntity? caster = null)
    {
        var projectile = new ProjectileData(
            origin,
            target,
            definition,
            caster);

        CreateShaderNode(projectile);
        _activeProjectiles.Add(projectile);
        AnimateProjectile(projectile);
    }

    /// <summary>
    /// Spawns a projectile with a callback to execute on impact.
    /// Used for AOE effects that need custom handling (e.g., fireball explosion).
    /// </summary>
    public void SpawnProjectileWithCallback(
        GridPosition origin,
        GridPosition target,
        ProjectileDefinition definition,
        System.Action onImpactCallback,
        BaseEntity? caster = null)
    {
        var projectile = new ProjectileData(
            origin,
            target,
            definition,
            caster)
        {
            OnImpactCallback = onImpactCallback
        };

        CreateShaderNode(projectile);
        _activeProjectiles.Add(projectile);
        AnimateProjectile(projectile);
    }

    #endregion

    #region Shader Node Creation

    /// <summary>
    /// Creates a ColorRect with the projectile shader for GPU-accelerated rendering.
    /// </summary>
    private void CreateShaderNode(ProjectileData projectile)
    {
        var shader = GetShader(projectile.Definition.ShaderPath);
        if (shader == null || _renderer == null) return;

        var material = new ShaderMaterial();
        material.Shader = shader;

        // Set uniforms from definition
        material.SetShaderParameter("progress", 0.0f);
        material.SetShaderParameter("head_color", projectile.Definition.HeadColor);
        material.SetShaderParameter("trail_color", projectile.Definition.GetTrailColor());
        material.SetShaderParameter("size", projectile.Definition.Size);
        material.SetShaderParameter("trail_length", projectile.Definition.TrailLength);
        material.SetShaderParameter("direction", projectile.GetDirection());

        // Apply any additional shader parameters
        foreach (var param in projectile.Definition.ShaderParams)
        {
            material.SetShaderParameter(param.Key, param.Value);
        }

        var colorRect = new ColorRect();
        colorRect.Material = material;

        // Size for the projectile visual - enough to show projectile and trail
        float visualSize = _tileSize * 2.5f * projectile.Definition.Size;
        colorRect.Size = new Vector2(visualSize, visualSize);
        colorRect.ZIndex = 100;

        projectile.ShaderNode = colorRect;
        projectile.Material = material;

        _renderer.AddChild(colorRect);
        UpdateShaderNodePosition(projectile);
    }

    /// <summary>
    /// Updates the shader node position based on current progress.
    /// </summary>
    private void UpdateShaderNodePosition(ProjectileData projectile)
    {
        if (projectile.ShaderNode == null || _renderer == null) return;

        var offset = _renderer.GetRenderOffset();
        var currentPos = projectile.GetCurrentPosition();
        var screenPos = _renderer.TilePosToCenter(currentPos);

        // Center the rect on the current position
        float halfSize = projectile.ShaderNode.Size.X / 2.0f;
        projectile.ShaderNode.Position = new Vector2(
            offset.X + screenPos.X - halfSize,
            offset.Y + screenPos.Y - halfSize
        );
    }

    #endregion

    #region Animation

    /// <summary>
    /// Animates a projectile from origin to target.
    /// </summary>
    private void AnimateProjectile(ProjectileData projectile)
    {
        int distance = DistanceHelper.ChebyshevDistance(projectile.Origin, projectile.Target);
        float speed = projectile.GetSpeed();
        float duration = distance / speed;

        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(progress => UpdateProjectileProgress(projectile, progress)),
            0.0f,
            1.0f,
            duration
        );
        tween.SetTrans(Tween.TransitionType.Linear);
        tween.SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(() => OnProjectileImpact(projectile)));
    }

    /// <summary>
    /// Updates projectile progress, shader uniforms, and position during animation.
    /// </summary>
    private void UpdateProjectileProgress(ProjectileData projectile, float progress)
    {
        projectile.Progress = progress;
        projectile.UpdateTrail();

        // Update shader uniforms
        if (projectile.Material != null)
        {
            projectile.Material.SetShaderParameter("progress", progress);
            projectile.Material.SetShaderParameter("direction", projectile.GetDirection());
        }

        // Update position
        UpdateShaderNodePosition(projectile);
    }

    #endregion

    #region Impact Handling

    /// <summary>
    /// Handles projectile impact - applies effects/damage and cleans up.
    /// </summary>
    private void OnProjectileImpact(ProjectileData projectile)
    {
        // Handle impact callback first (for AOE effects)
        if (projectile.HasImpactCallback)
        {
            projectile.OnImpactCallback?.Invoke();
        }
        else if (projectile.HasDeferredEffect)
        {
            ApplyDeferredEffect(projectile);
        }
        else if (projectile.AttackIndex >= 0 && projectile.TargetEntity != null && projectile.Caster != null)
        {
            ApplyAttackDamage(projectile);
        }

        RemoveProjectile(projectile);
    }

    /// <summary>
    /// Applies the deferred effect from a skill projectile.
    /// </summary>
    private void ApplyDeferredEffect(ProjectileData projectile)
    {
        if (projectile.DeferredEffect == null || projectile.DeferredEffectContext == null)
            return;

        var result = projectile.DeferredEffect.Apply(projectile.DeferredEffectContext);

        // Emit damage signal for message log if damage was dealt
        if (result.Success && result.DamageDealt > 0)
        {
            var context = projectile.DeferredEffectContext;
            string skillName = context.Skill?.Name ?? "skill";

            if (context.Caster != null && context.Target != null)
            {
                EmitSignal(SignalName.SkillDamageDealt, context.Caster, context.Target, result.DamageDealt, skillName);
            }
        }
    }

    /// <summary>
    /// Applies attack damage from ranged weapon projectiles.
    /// </summary>
    private void ApplyAttackDamage(ProjectileData projectile)
    {
        if (projectile.TargetEntity == null || projectile.Caster == null)
            return;

        var attackComponent = projectile.Caster.GetNodeOrNull<AttackComponent>("AttackComponent");
        attackComponent?.RequestAttack(projectile.TargetEntity, projectile.AttackIndex);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Removes a projectile from the active list.
    /// </summary>
    private void RemoveProjectile(ProjectileData projectile)
    {
        // Clean up shader node
        if (projectile.ShaderNode != null)
        {
            projectile.ShaderNode.QueueFree();
            projectile.ShaderNode = null;
        }
        projectile.Material = null;

        _activeProjectiles.Remove(projectile);
        _renderer?.QueueRedraw();

        if (_activeProjectiles.Count == 0)
        {
            EmitSignal(SignalName.AllProjectilesCompleted);
        }
    }

    /// <summary>
    /// Clears all active projectiles (useful for scene transitions).
    /// </summary>
    public void ClearAllProjectiles()
    {
        foreach (var projectile in _activeProjectiles)
        {
            if (projectile.ShaderNode != null)
            {
                projectile.ShaderNode.QueueFree();
                projectile.ShaderNode = null;
            }
            projectile.Material = null;
        }
        _activeProjectiles.Clear();
        _renderer?.QueueRedraw();
    }

    public override void _ExitTree()
    {
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.RangedAttackRequested, Callable.From<Vector2I, Vector2I, BaseEntity, int>(OnRangedAttackRequested));
        }

        ClearAllProjectiles();
    }

    #endregion
}
