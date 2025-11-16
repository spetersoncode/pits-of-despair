using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems;

namespace PitsOfDespair.AI;

/// <summary>
/// Context data bundle provided to goals for evaluation and execution.
/// Contains all information goals need to calculate scores and perform actions.
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
    /// Action context for executing actions.
    /// </summary>
    public ActionContext ActionContext { get; set; }

    /// <summary>
    /// The player entity.
    /// </summary>
    public Player Player { get; set; }

    /// <summary>
    /// Map system for terrain queries.
    /// </summary>
    public MapSystem MapSystem { get; set; }

    /// <summary>
    /// Entity manager for entity queries.
    /// </summary>
    public EntityManager EntityManager { get; set; }

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
}
