using PitsOfDespair.Actions;
using PitsOfDespair.Data;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;
using PitsOfDespair.Systems.Vision;
using PitsOfDespair.Targeting;
using PitsOfDespair.Generation.Spawning;

namespace PitsOfDespair.Debug;

/// <summary>
/// Context object providing access to game systems for debug command execution.
/// Composes ActionContext for core game systems and adds debug-specific systems.
///
/// Core game systems (Player, MapSystem, EntityManager, EntityFactory, CombatSystem) are
/// accessed via ActionContext. For example:
/// - context.ActionContext.Player for player reference
/// - context.ActionContext.MapSystem for map queries
/// - context.ActionContext.EntityManager for entity management
/// - context.ActionContext.EntityFactory for entity creation
/// </summary>
public class DebugContext
{
    /// <summary>
    /// Action context for accessing core game systems.
    /// Use this to access Player, MapSystem, EntityManager, EntityFactory, and CombatSystem.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// Turn manager for controlling turn flow (debug-specific).
    /// </summary>
    public TurnManager TurnManager { get; }

    /// <summary>
    /// Player vision system for manipulating visibility (debug-specific).
    /// </summary>
    public PlayerVisionSystem VisionSystem { get; }

    /// <summary>
    /// Cursor targeting system for interactive tile selection.
    /// </summary>
    public CursorTargetingSystem TargetingSystem { get; }

    /// <summary>
    /// Data loader for looking up creature and item definitions.
    /// </summary>
    public DataLoader DataLoader { get; }

    /// <summary>
    /// AI system for registering spawned creature AI components.
    /// </summary>
    public AISystem AISystem { get; }

    /// <summary>
    /// Movement system for registering spawned creature movement components.
    /// </summary>
    public MovementSystem MovementSystem { get; }

    /// <summary>
    /// Time system for registering spawned creature speed components.
    /// </summary>
    public TimeSystem TimeSystem { get; }

    /// <summary>
    /// Spawn orchestrator for accessing spawn statistics.
    /// </summary>
    public SpawnOrchestrator SpawnOrchestrator { get; }

    public DebugContext(
        ActionContext actionContext,
        TurnManager turnManager,
        PlayerVisionSystem visionSystem,
        CursorTargetingSystem targetingSystem,
        DataLoader dataLoader,
        AISystem aiSystem,
        MovementSystem movementSystem,
        TimeSystem timeSystem,
        SpawnOrchestrator spawnOrchestrator)
    {
        ActionContext = actionContext;
        TurnManager = turnManager;
        VisionSystem = visionSystem;
        TargetingSystem = targetingSystem;
        DataLoader = dataLoader;
        AISystem = aiSystem;
        MovementSystem = movementSystem;
        TimeSystem = timeSystem;
        SpawnOrchestrator = spawnOrchestrator;
    }
}
