using PitsOfDespair.Entities;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;
using PitsOfDespair.Systems.Vision;
using PitsOfDespair.Systems.VisualEffects;

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
    /// The visual effect system for spawning explosions, projectiles, and other visual effects.
    /// </summary>
    public VisualEffectSystem VisualEffectSystem { get; }

    /// <summary>
    /// The tile hazard manager for creating persistent tile hazards.
    /// </summary>
    public TileHazardManager? TileHazardManager { get; }

    /// <summary>
    /// The player vision system for fog of war and map revelation.
    /// </summary>
    public PlayerVisionSystem? PlayerVisionSystem { get; }

    public ActionContext(
        MapSystem mapSystem,
        EntityManager entityManager,
        Player player,
        CombatSystem combatSystem,
        EntityFactory entityFactory,
        VisualEffectSystem visualEffectSystem,
        TileHazardManager? tileHazardManager = null,
        PlayerVisionSystem? playerVisionSystem = null)
    {
        MapSystem = mapSystem;
        EntityManager = entityManager;
        Player = player;
        CombatSystem = combatSystem;
        EntityFactory = entityFactory;
        VisualEffectSystem = visualEffectSystem;
        TileHazardManager = tileHazardManager;
        PlayerVisionSystem = playerVisionSystem;
    }
}
