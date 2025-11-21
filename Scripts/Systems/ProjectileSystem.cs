using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages projectiles (arrows, bolts, etc.) in flight.
/// Handles spawning, animation, and impact resolution.
/// Projectiles are rendered as animated lines/beams, not entities.
/// </summary>
public partial class ProjectileSystem : Node
{
    [Signal]
    public delegate void AllProjectilesCompletedEventHandler();

    private const float ProjectileSpeed = 30.0f; // Tiles per second
    private List<ProjectileData> _activeProjectiles = new();
    private CombatSystem _combatSystem;
    private Player _player;
    private TextRenderer _renderer;

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
    /// <param name="combatSystem">Combat system for damage application</param>
    public void Initialize(CombatSystem combatSystem)
    {
        _combatSystem = combatSystem;
    }

    /// <summary>
    /// Connects to the player's ranged attack signal.
    /// </summary>
    /// <param name="player">The player entity</param>
    public void ConnectToPlayer(Player player)
    {
        _player = player;
        player.Connect(Player.SignalName.RangedAttackRequested, Callable.From<Vector2I, Vector2I, BaseEntity, int>(OnRangedAttackRequested));
    }

    /// <summary>
    /// Sets the text renderer reference for forcing visual updates.
    /// </summary>
    /// <param name="renderer">The text renderer</param>
    public void SetTextRenderer(TextRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Spawns a projectile when a ranged attack is requested.
    /// </summary>
    private void OnRangedAttackRequested(Vector2I origin, Vector2I target, BaseEntity targetEntity, int attackIndex)
    {
        if (_player == null)
        {
            GD.PushError("ProjectileSystem: Player reference is null!");
            return;
        }

        SpawnProjectile(GridPosition.FromVector2I(origin), GridPosition.FromVector2I(target), targetEntity, _player, attackIndex);
    }

    /// <summary>
    /// Spawns and animates a projectile.
    /// </summary>
    /// <param name="origin">Starting position</param>
    /// <param name="target">Target position</param>
    /// <param name="targetEntity">Target entity (can be null)</param>
    /// <param name="attacker">Entity firing the projectile</param>
    /// <param name="attackIndex">Index of the attack</param>
    public void SpawnProjectile(GridPosition origin, GridPosition target, BaseEntity targetEntity, BaseEntity attacker, int attackIndex)
    {
        // Create projectile data
        var projectile = new ProjectileData(origin, target, targetEntity, attacker, attackIndex, Palette.ProjectileBeam);

        // Add to active list
        _activeProjectiles.Add(projectile);

        // Start animation
        AnimateProjectile(projectile);
    }

    /// <summary>
    /// Animates a projectile from origin to target.
    /// </summary>
    private void AnimateProjectile(ProjectileData projectile)
    {
        // Calculate distance for animation duration
        int distance = DistanceHelper.ChebyshevDistance(projectile.Origin, projectile.Target);
        float duration = distance / ProjectileSpeed;

        // Create tween for smooth animation
        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(progress => projectile.Progress = progress),
            0.0f,
            1.0f,
            duration
        );
        tween.SetTrans(Tween.TransitionType.Linear);
        tween.SetEase(Tween.EaseType.InOut);

        // When tween finishes, handle impact
        tween.TweenCallback(Callable.From(() => OnProjectileImpact(projectile)));
    }

    /// <summary>
    /// Handles projectile impact - applies damage and cleans up.
    /// </summary>
    private void OnProjectileImpact(ProjectileData projectile)
    {
        // Apply damage if there's a target entity
        if (projectile.TargetEntity != null && projectile.Attacker != null)
        {
            var attackComponent = projectile.Attacker.GetNodeOrNull<AttackComponent>("AttackComponent");
            if (attackComponent != null)
            {
                // Request attack through combat system
                attackComponent.RequestAttack(projectile.TargetEntity, projectile.AttackIndex);
            }
        }

        // Clean up projectile
        RemoveProjectile(projectile);
    }

    /// <summary>
    /// Removes a projectile from the active list.
    /// Forces a visual refresh to immediately clear the projectile from display.
    /// </summary>
    private void RemoveProjectile(ProjectileData projectile)
    {
        _activeProjectiles.Remove(projectile);

        // Force immediate visual update to prevent projectile from appearing stuck
        _renderer?.QueueRedraw();

        // If this was the last projectile, emit completion signal
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
        _activeProjectiles.Clear();
        _renderer?.QueueRedraw();
    }

    public override void _ExitTree()
    {
        // Disconnect from player
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.RangedAttackRequested, Callable.From<Vector2I, Vector2I, BaseEntity, int>(OnRangedAttackRequested));
        }

        // Clear projectiles
        ClearAllProjectiles();
    }
}
