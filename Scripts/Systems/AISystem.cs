using Godot;
using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.VisualEffects;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages AI behavior for non-player entities.
/// Processes individual creature turns based on energy scheduling.
/// Uses the action system for all creature actions.
/// </summary>
public partial class AISystem : Node
{
    private MapSystem _mapSystem;
    private Player _player;
    private EntityManager _entityManager;
    private CombatSystem _combatSystem;
    private EntityFactory _entityFactory;
    private VisualEffectSystem _visualEffectSystem;
    private ActionContext _actionContext;

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
    /// Sets the visual effect system dependency.
    /// </summary>
    public void SetVisualEffectSystem(VisualEffectSystem visualEffectSystem)
    {
        _visualEffectSystem = visualEffectSystem;
        UpdateActionContext();
    }

    /// <summary>
    /// Updates the cached action context when dependencies change.
    /// </summary>
    private void UpdateActionContext()
    {
        // Only create context if all dependencies are set
        if (_mapSystem != null && _entityManager != null && _player != null && _combatSystem != null && _entityFactory != null && _visualEffectSystem != null)
        {
            _actionContext = new ActionContext(_mapSystem, _entityManager, _player, _combatSystem, _entityFactory, _visualEffectSystem);
        }
    }

    /// <summary>
    /// Processes a single creature's turn.
    /// Called by TurnManager when the creature has enough energy to act.
    /// </summary>
    /// <param name="speedComponent">The speed component of the creature to process.</param>
    /// <returns>The delay cost of the action taken.</returns>
    public int ProcessSingleCreatureTurn(SpeedComponent speedComponent)
    {
        // Get the entity from the speed component
        BaseEntity entity = speedComponent.GetParent<BaseEntity>();
        if (entity == null || entity.IsDead)
        {
            return ActionDelay.Standard; // Default delay for invalid/dead entities
        }

        // Get the AI component
        var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent == null)
        {
            return ActionDelay.Standard; // No AI, default delay
        }

        // Build AI context
        var context = BuildAIContext(aiComponent, entity);

        // Process the goal stack and get the action result
        var result = ProcessGoalStack(aiComponent, context);

        // Calculate the actual delay based on the creature's speed
        int actualDelay = speedComponent.CalculateDelay(result.DelayCost);

        return actualDelay;
    }

    /// <summary>
    /// Processes the goal stack for a creature's turn.
    /// Removes finished goals, ensures stack is never empty, and executes the top goal.
    /// Goals that only push sub-goals don't consume a turn - we continue executing
    /// until a goal performs an actual action (movement, attack, etc.) or we hit a safety limit.
    /// </summary>
    /// <returns>The action result from the executed goal.</returns>
    private ActionResult ProcessGoalStack(AIComponent ai, AIContext context)
    {
        var stack = ai.GoalStack;
        const int maxIterations = 10; // Safety limit to prevent infinite loops
        int iterations = 0;

        while (iterations < maxIterations)
        {
            iterations++;

            // Remove any finished goals from the stack
            stack.RemoveFinished(context);

            // Ensure stack is never empty - BoredGoal is the default fallback
            if (stack.IsEmpty)
            {
                stack.Push(new BoredGoal());
            }

            // Remember stack state before execution
            int stackCountBefore = stack.Count;
            Goal currentGoal = stack.Peek();

            // Execute the top goal
            currentGoal.TakeAction(context);

            // If the stack grew (goal pushed a sub-goal), continue to let sub-goal execute
            // If the stack stayed same or shrunk, the goal either:
            // - Did nothing (waiting) - stop
            // - Performed an action and finished - stop
            // - Failed and popped goals - stop
            if (stack.Count <= stackCountBefore)
            {
                break;
            }
            // Stack grew - a sub-goal was pushed, loop to execute it immediately
        }

        // Return default action result (goals currently don't return results directly)
        // The action has already been executed, this is just for delay calculation
        return ActionResult.CreateSuccess();
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

}
