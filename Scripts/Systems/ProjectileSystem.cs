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
/// </summary>
public partial class ProjectileSystem : Node
{
    private const float ProjectileSpeed = 30.0f; // Tiles per second
    private List<Projectile> _activeProjectiles = new();
    private CombatSystem _combatSystem;
    private Player _player;
    private TextRenderer _renderer;

    /// <summary>
    /// Gets all currently active projectiles for rendering.
    /// </summary>
    public IReadOnlyList<Projectile> ActiveProjectiles => _activeProjectiles.AsReadOnly();

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
        // Create projectile
        var projectile = new Projectile(origin, target, targetEntity, attacker, attackIndex);

        // Add to scene tree so it can use tweens
        AddChild(projectile);

        // Add to active list
        _activeProjectiles.Add(projectile);

        // Connect impact signal
        projectile.Connect(Projectile.SignalName.ImpactReached, Callable.From(() => OnProjectileImpact(projectile)));

        // Start animation
        AnimateProjectile(projectile);
    }

    /// <summary>
    /// Animates a projectile from origin to target.
    /// </summary>
    private void AnimateProjectile(Projectile projectile)
    {
        // Calculate distance for animation duration
        int distance = DistanceHelper.ChebyshevDistance(projectile.Origin, projectile.Target);
        float duration = distance / ProjectileSpeed;

        // Create tween for smooth animation
        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(projectile.UpdateProgress),
            0.0f,
            1.0f,
            duration
        );
        tween.SetTrans(Tween.TransitionType.Linear);
        tween.SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    /// Handles projectile impact - applies damage and cleans up.
    /// </summary>
    private void OnProjectileImpact(Projectile projectile)
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
    /// Removes a projectile from the active list and scene tree.
    /// Forces a visual refresh to immediately clear the projectile from display.
    /// </summary>
    private void RemoveProjectile(Projectile projectile)
    {
        _activeProjectiles.Remove(projectile);
        projectile.QueueFree();

        // Force immediate visual update to prevent projectile from appearing stuck
        _renderer?.QueueRedraw();
    }

    /// <summary>
    /// Clears all active projectiles (useful for scene transitions).
    /// </summary>
    public void ClearAllProjectiles()
    {
        foreach (var projectile in _activeProjectiles.ToArray())
        {
            projectile.QueueFree();
        }
        _activeProjectiles.Clear();
    }

    public override void _ExitTree()
    {
        // Disconnect from player
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.RangedAttackRequested, Callable.From<Vector2I, Vector2I, BaseEntity, int>(OnRangedAttackRequested));
        }

        // Disconnect from all active projectiles
        foreach (var projectile in _activeProjectiles.ToArray())
        {
            if (projectile != null && GodotObject.IsInstanceValid(projectile))
            {
                projectile.Disconnect(Projectile.SignalName.ImpactReached, Callable.From(() => OnProjectileImpact(projectile)));
            }
        }

        // Clear projectiles
        ClearAllProjectiles();
    }
}
