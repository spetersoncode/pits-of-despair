using Godot;
using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages AI behavior for non-player entities.
/// Processes creature turns, updates AI state, and commands movement.
/// Uses the action system for all creature actions.
/// </summary>
public partial class AISystem : Node
{
    private MapSystem _mapSystem;
    private Player _player;
    private EntityManager _entityManager;
    private TurnManager _turnManager;
    private CombatSystem _combatSystem;
    private EntityFactory _entityFactory;
    private ActionContext _actionContext;
    private List<AIComponent> _aiComponents = new List<AIComponent>();

    /// <summary>
    /// Sets the map system dependency.
    /// </summary>
    public void SetMapSystem(MapSystem mapSystem)
    {
        _mapSystem = mapSystem;
    }

    /// <summary>
    /// Sets the player reference.
    /// </summary>
    public void SetPlayer(Player player)
    {
        _player = player;
    }

    /// <summary>
    /// Sets the entity manager dependency.
    /// </summary>
    public void SetEntityManager(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Sets the combat system dependency.
    /// </summary>
    public void SetCombatSystem(CombatSystem combatSystem)
    {
        _combatSystem = combatSystem;
        UpdateActionContext();
    }

    /// <summary>
    /// Sets the entity factory dependency.
    /// </summary>
    public void SetEntityFactory(EntityFactory entityFactory)
    {
        _entityFactory = entityFactory;
        UpdateActionContext();
    }

    /// <summary>
    /// Updates the cached action context when dependencies change.
    /// </summary>
    private void UpdateActionContext()
    {
        // Only create context if all dependencies are set
        if (_mapSystem != null && _entityManager != null && _player != null && _combatSystem != null && _entityFactory != null)
        {
            _actionContext = new ActionContext(_mapSystem, _entityManager, _player, _combatSystem, _entityFactory);
        }
    }

    /// <summary>
    /// Sets the turn manager and subscribes to creature turn events.
    /// </summary>
    public void SetTurnManager(TurnManager turnManager)
    {
        // Disconnect from old turn manager if exists
        if (_turnManager != null)
        {
            _turnManager.Disconnect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnCreatureTurnsStarted));
        }

        _turnManager = turnManager;

        // Connect to new turn manager
        if (_turnManager != null)
        {
            _turnManager.Connect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnCreatureTurnsStarted));
        }
    }

    /// <summary>
    /// Registers an AI component for processing.
    /// </summary>
    public void RegisterAIComponent(AIComponent component)
    {
        if (!_aiComponents.Contains(component))
        {
            _aiComponents.Add(component);
        }
    }

    /// <summary>
    /// Called when creature turns start.
    /// Processes all creatures with AI components.
    /// </summary>
    private void OnCreatureTurnsStarted()
    {
        // Process each creature's AI
        foreach (AIComponent ai in _aiComponents)
        {
            // Skip if component or entity no longer valid
            if (!IsInstanceValid(ai) || ai.GetParent() == null)
            {
                continue;
            }

            ProcessCreatureTurn(ai);
        }

        // End creature turns after all have been processed
        _turnManager?.EndCreatureTurns();
    }

    /// <summary>
    /// Processes a single creature's turn using goal-based AI.
    /// </summary>
    private void ProcessCreatureTurn(AIComponent ai)
    {
        BaseEntity entity = ai.GetEntity();
        if (entity == null)
        {
            return;
        }

        // Check if no goals available
        if (ai.AvailableGoals == null || ai.AvailableGoals.Count == 0)
        {
            GD.PrintErr($"Entity {entity.DisplayName} has no goals available!");
            return;
        }

        // Build AI context
        var context = BuildAIContext(ai, entity);

        // Update state tracking
        UpdateStateTracking(ai, context);

        // Evaluate all goals and select the best one
        Goal bestGoal = GoalEvaluator.EvaluateBestGoal(ai.AvailableGoals, context);

        if (bestGoal == null)
        {
            GD.PrintErr($"No valid goals for entity {entity.DisplayName}");
            return;
        }

        // Handle goal transitions
        if (ai.CurrentGoal != bestGoal)
        {
            ai.CurrentGoal?.OnDeactivated(context);
            bestGoal.OnActivated(context);
            ai.CurrentGoal = bestGoal;
        }

        // Execute the selected goal
        bestGoal.Execute(context);
    }

    /// <summary>
    /// Builds the AI context for goal evaluation and execution.
    /// Reuses cached ActionContext and only rebuilds dynamic per-turn state.
    /// </summary>
    private AIContext BuildAIContext(AIComponent ai, BaseEntity entity)
    {
        bool playerVisible = IsPlayerVisible(entity);
        int distanceToPlayer = DistanceHelper.ChebyshevDistance(entity.GridPosition, _player.GridPosition);

        return new AIContext
        {
            Entity = entity,
            AIComponent = ai,
            ActionContext = _actionContext,
            IsPlayerVisible = playerVisible,
            DistanceToPlayer = distanceToPlayer,
            VisionComponent = entity.GetNodeOrNull<VisionComponent>("VisionComponent"),
            HealthComponent = entity.GetNodeOrNull<HealthComponent>("HealthComponent"),
            AttackComponent = entity.GetNodeOrNull<AttackComponent>("AttackComponent")
        };
    }

    /// <summary>
    /// Updates state tracking variables based on current context.
    /// </summary>
    private void UpdateStateTracking(AIComponent ai, AIContext context)
    {
        if (context.IsPlayerVisible)
        {
            // Reset counter when player is seen
            ai.TurnsSincePlayerSeen = 0;
            // Reset search turns to full when player spotted
            ai.SearchTurnsRemaining = ai.SearchTurns;
            // Reset flee turns to full when player spotted
            ai.FleeturnsRemaining = ai.FleeTurns;
        }
        else
        {
            // Increment counter when player not visible
            ai.TurnsSincePlayerSeen++;
            // Reset yell counter when player is lost
            ai.TurnsSinceLastYell = 0;
        }
    }

    /// <summary>
    /// Checks if the player is visible to the entity using shadowcasting.
    /// </summary>
    private bool IsPlayerVisible(BaseEntity entity)
    {
        VisionComponent vision = entity.GetNodeOrNull<VisionComponent>("VisionComponent");
        if (vision == null)
        {
            return false;
        }

        GridPosition playerPos = _player.GridPosition;
        GridPosition entityPos = entity.GridPosition;

        // Calculate visible tiles using shadowcasting
        HashSet<GridPosition> visiblePositions = FOVCalculator.CalculateVisibleTiles(
            entityPos,
            vision.VisionRange,
            _mapSystem
        );

        return visiblePositions.Contains(playerPos);
    }

    public override void _ExitTree()
    {
        if (_turnManager != null)
        {
            _turnManager.Disconnect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnCreatureTurnsStarted));
        }
    }
}
