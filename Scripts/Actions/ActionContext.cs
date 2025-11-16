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

    public ActionContext(
        MapSystem mapSystem,
        EntityManager entityManager,
        Player player,
        CombatSystem combatSystem)
    {
        MapSystem = mapSystem;
        EntityManager = entityManager;
        Player = player;
        CombatSystem = combatSystem;
    }
}
