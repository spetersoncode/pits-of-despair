using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems;
using System.Collections.Generic;

namespace PitsOfDespair.AI;

/// <summary>
/// Context data bundle provided to goals for evaluation and execution.
/// Contains all information goals need to calculate scores and perform actions.
///
/// Core game systems (MapSystem, EntityManager, Player, etc.) are accessed via the
/// ActionContext property to avoid duplication. For example:
/// - context.ActionContext.MapSystem for terrain queries
/// - context.ActionContext.EntityManager for entity lookups
/// - context.ActionContext.Player for player reference
/// </summary>
public class AIContext
{
    /// <summary>
    /// The entity this AI is controlling.
    /// </summary>
    public BaseEntity Entity { get; set; }

    /// <summary>
    /// The AI component attached to this entity.
    /// </summary>
    public AIComponent AIComponent { get; set; }

    /// <summary>
    /// Action context for executing actions and accessing core game systems.
    /// Use this to access MapSystem, EntityManager, Player, CombatSystem, and EntityFactory.
    /// </summary>
    public ActionContext ActionContext { get; set; }

    /// <summary>
    /// Whether the player is currently visible to this entity.
    /// </summary>
    public bool IsPlayerVisible { get; set; }

    /// <summary>
    /// Current distance to the player (Chebyshev distance).
    /// </summary>
    public int DistanceToPlayer { get; set; }

    /// <summary>
    /// Vision component of this entity (if present).
    /// </summary>
    public VisionComponent VisionComponent { get; set; }

    /// <summary>
    /// Health component of this entity (if present).
    /// </summary>
    public HealthComponent HealthComponent { get; set; }

    /// <summary>
    /// Attack component of this entity (if present).
    /// </summary>
    public AttackComponent AttackComponent { get; set; }

    /// <summary>
    /// Gets the protection target for this entity (from AIComponent).
    /// Returns null if no protection target is set.
    /// </summary>
    public BaseEntity? ProtectionTarget => AIComponent?.ProtectionTarget;

    /// <summary>
    /// Gets all visible hostile entities within vision range.
    /// For Friendly faction, returns Hostile entities. For Hostile faction, returns Friendly entities.
    /// </summary>
    public List<BaseEntity> GetVisibleEnemies()
    {
        var enemies = new List<BaseEntity>();
        var entityManager = ActionContext?.EntityManager;
        var mapSystem = ActionContext?.MapSystem;

        if (entityManager == null || mapSystem == null || VisionComponent == null)
            return enemies;

        var myFaction = Entity.Faction;
        int visionRange = VisionComponent.VisionRange;

        // Calculate visible tiles once for efficiency
        var visibleTiles = FOVCalculator.CalculateVisibleTiles(Entity.GridPosition, visionRange, mapSystem);

        // Check all entities
        foreach (var entity in entityManager.GetAllEntities())
        {
            // Skip self
            if (entity == Entity)
                continue;

            // Skip non-hostile entities
            if (!myFaction.IsHostileTo(entity.Faction))
                continue;

            // Skip dead entities
            var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health != null && !health.IsAlive())
                continue;

            // Check if visible
            if (visibleTiles.Contains(entity.GridPosition))
            {
                enemies.Add(entity);
            }
        }

        // Also check the player
        var player = ActionContext?.Player;
        if (player != null && myFaction.IsHostileTo(player.Faction))
        {
            var playerHealth = player.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (playerHealth != null && playerHealth.IsAlive())
            {
                if (visibleTiles.Contains(player.GridPosition))
                {
                    enemies.Add(player);
                }
            }
        }

        return enemies;
    }

    /// <summary>
    /// Gets visible enemies that are near the protection target.
    /// Used by DefendTargetGoal to find threats to defend against.
    /// </summary>
    /// <param name="maxDistanceFromTarget">Maximum distance from protection target to consider.</param>
    public List<BaseEntity> GetEnemiesNearProtectionTarget(int maxDistanceFromTarget = 5)
    {
        var enemies = new List<BaseEntity>();
        var target = ProtectionTarget;

        if (target == null)
            return enemies;

        var visibleEnemies = GetVisibleEnemies();
        foreach (var enemy in visibleEnemies)
        {
            int distanceToTarget = DistanceHelper.ChebyshevDistance(enemy.GridPosition, target.GridPosition);
            if (distanceToTarget <= maxDistanceFromTarget)
            {
                enemies.Add(enemy);
            }
        }

        return enemies;
    }

    /// <summary>
    /// Gets the closest enemy from a list of enemies.
    /// </summary>
    public BaseEntity? GetClosestEnemy(List<BaseEntity> enemies)
    {
        if (enemies == null || enemies.Count == 0)
            return null;

        BaseEntity? closest = null;
        int closestDistance = int.MaxValue;

        foreach (var enemy in enemies)
        {
            int distance = DistanceHelper.ChebyshevDistance(Entity.GridPosition, enemy.GridPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }
}
