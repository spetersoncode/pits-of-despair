using PitsOfDespair.Actions;
using PitsOfDespair.Systems;

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

    public DebugContext(
        ActionContext actionContext,
        TurnManager turnManager,
        PlayerVisionSystem visionSystem)
    {
        ActionContext = actionContext;
        TurnManager = turnManager;
        VisionSystem = visionSystem;
    }
}
