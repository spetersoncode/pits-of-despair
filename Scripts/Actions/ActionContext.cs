using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Actions;

/// <summary>
/// Provides the minimal context required for actions to validate and execute.
/// Exposes only the systems that actions actually need, rather than the entire GameLevel.
/// </summary>
public class ActionContext
{
    /// <summary>
    /// The map system for terrain validation.
    /// </summary>
    public MapSystem MapSystem { get; }

    /// <summary>
    /// The entity manager for entity queries and management.
    /// </summary>
    public EntityManager EntityManager { get; }

    /// <summary>
    /// Reference to the player entity (used for bump-to-attack detection).
    /// </summary>
    public Player Player { get; }

    /// <summary>
    /// The combat system for emitting combat events.
    /// </summary>
    public CombatSystem CombatSystem { get; }

    /// <summary>
    /// The entity factory for creating entities (e.g., dropping items).
    /// </summary>
    public EntityFactory EntityFactory { get; }

    /// <summary>
    /// The projectile system for spawning ranged attack projectiles.
    /// </summary>
    public ProjectileSystem ProjectileSystem { get; }

    public ActionContext(
        MapSystem mapSystem,
        EntityManager entityManager,
        Player player,
        CombatSystem combatSystem,
        EntityFactory entityFactory,
        ProjectileSystem projectileSystem)
    {
        MapSystem = mapSystem;
        EntityManager = entityManager;
        Player = player;
        CombatSystem = combatSystem;
        EntityFactory = entityFactory;
        ProjectileSystem = projectileSystem;
    }
}
