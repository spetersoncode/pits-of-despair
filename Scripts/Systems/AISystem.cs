using Godot;
using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Systems;

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
    /// Updates the cached action context when dependencies change.
    /// </summary>
    private void UpdateActionContext()
    {
        // Only create context if all dependencies are set
        if (_mapSystem != null && _entityManager != null && _player != null && _combatSystem != null)
        {
            _actionContext = new ActionContext(_mapSystem, _entityManager, _player, _combatSystem);
        }
    }

    /// <summary>
    /// Sets the turn manager and subscribes to creature turn events.
    /// </summary>
    public void SetTurnManager(TurnManager turnManager)
    {
        // Unsubscribe from old turn manager if exists
        if (_turnManager != null)
        {
            _turnManager.CreatureTurnsStarted -= OnCreatureTurnsStarted;
        }

        _turnManager = turnManager;

        // Subscribe to new turn manager
        if (_turnManager != null)
        {
            _turnManager.CreatureTurnsStarted += OnCreatureTurnsStarted;
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
    /// Processes a single creature's turn.
    /// </summary>
    private void ProcessCreatureTurn(AIComponent ai)
    {
        BaseEntity entity = ai.GetEntity();
        if (entity == null)
        {
            return;
        }

        // Check if player is visible
        bool playerVisible = IsPlayerVisible(entity);

        // Update AI state based on visibility
        UpdateAIState(ai, entity, playerVisible);

        // Execute behavior based on current state
        ExecuteAIBehavior(ai, entity, playerVisible);
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
        HashSet<GridPosition> visiblePositions = ShadowcastingHelper.CalculateVisibleTiles(
            entityPos,
            vision.VisionRange,
            _mapSystem
        );

        return visiblePositions.Contains(playerPos);
    }

    /// <summary>
    /// Updates AI state based on player visibility.
    /// </summary>
    private void UpdateAIState(AIComponent ai, BaseEntity entity, bool playerVisible)
    {
        if (playerVisible)
        {
            // Player visible - switch to chasing
            if (ai.CurrentState != AIComponent.AIState.Chasing)
            {
                ai.CurrentState = AIComponent.AIState.Chasing;
                ai.ClearPath();
            }
            ai.LastKnownPlayerPosition = _player.GridPosition;
        }
        else if (ai.CurrentState == AIComponent.AIState.Chasing)
        {
            // Lost sight of player - start investigating
            ai.CurrentState = AIComponent.AIState.Investigating;
            ai.InvestigationTurnsRemaining = ai.SearchTurns;
            ai.ClearPath();
        }
        else if (ai.CurrentState == AIComponent.AIState.Investigating)
        {
            // Check if investigation time is up
            if (ai.InvestigationTurnsRemaining <= 0)
            {
                ai.CurrentState = AIComponent.AIState.Returning;
                ai.ClearPath();
            }
        }
        else if (ai.CurrentState == AIComponent.AIState.Returning)
        {
            // Check if reached spawn position
            if (entity.GridPosition.Equals(ai.SpawnPosition))
            {
                ai.CurrentState = AIComponent.AIState.Idle;
                ai.ClearPath();
            }
        }
    }

    /// <summary>
    /// Executes AI behavior based on current state.
    /// </summary>
    private void ExecuteAIBehavior(AIComponent ai, BaseEntity entity, bool playerVisible)
    {
        switch (ai.CurrentState)
        {
            case AIComponent.AIState.Idle:
                // Do nothing
                break;

            case AIComponent.AIState.Chasing:
                ChasePlayer(ai, entity);
                break;

            case AIComponent.AIState.Investigating:
                Investigate(ai, entity);
                break;

            case AIComponent.AIState.Returning:
                ReturnToSpawn(ai, entity);
                break;
        }
    }

    /// <summary>
    /// Chases the player by pathfinding and moving toward them.
    /// </summary>
    private void ChasePlayer(AIComponent ai, BaseEntity entity)
    {
        GridPosition target = _player.GridPosition;
        int distanceToPlayer = DistanceHelper.ChebyshevDistance(entity.GridPosition, target);

        // If adjacent to player, attack using action system
        if (distanceToPlayer <= 1)
        {
            var attackAction = new AttackAction(_player, 0);
            entity.ExecuteAction(attackAction, _actionContext);
            return;
        }

        // If we don't have a path or reached the end, calculate new path
        if (ai.CurrentPath.Count == 0)
        {
            var path = PathfindingHelper.FindPath(entity.GridPosition, target, _mapSystem, _entityManager, _player);
            if (path != null)
            {
                ai.CurrentPath = path;
            }
        }

        // Move along path
        MoveAlongPath(ai, entity, target);
    }

    /// <summary>
    /// Investigates the last known player position.
    /// </summary>
    private void Investigate(AIComponent ai, BaseEntity entity)
    {
        if (ai.LastKnownPlayerPosition == null)
        {
            // No last known position - return to spawn
            ai.CurrentState = AIComponent.AIState.Returning;
            return;
        }

        GridPosition lastKnown = ai.LastKnownPlayerPosition.Value;

        // If not at last known position, path there
        if (!entity.GridPosition.Equals(lastKnown))
        {
            if (ai.CurrentPath.Count == 0)
            {
                var path = PathfindingHelper.FindPath(entity.GridPosition, lastKnown, _mapSystem, _entityManager, _player);
                if (path != null)
                {
                    ai.CurrentPath = path;
                }
            }
            MoveAlongPath(ai, entity, lastKnown);
        }
        else
        {
            // At last known position - wander randomly
            WanderNearPosition(ai, entity, lastKnown);
            ai.InvestigationTurnsRemaining--;
        }
    }

    /// <summary>
    /// Returns to spawn position.
    /// </summary>
    private void ReturnToSpawn(AIComponent ai, BaseEntity entity)
    {
        if (ai.CurrentPath.Count == 0)
        {
            var path = PathfindingHelper.FindPath(entity.GridPosition, ai.SpawnPosition, _mapSystem, _entityManager, _player);
            if (path != null)
            {
                ai.CurrentPath = path;
            }
        }

        MoveAlongPath(ai, entity, ai.SpawnPosition);
    }

    /// <summary>
    /// Moves entity along its current path.
    /// Validates the next position is still available and repaths if blocked.
    /// </summary>
    /// <param name="goal">The ultimate destination for repathfinding if blocked</param>
    private void MoveAlongPath(AIComponent ai, BaseEntity entity, GridPosition goal)
    {
        GridPosition? nextPos = ai.GetNextPosition();
        if (nextPos == null)
        {
            return;
        }

        // Validate next position isn't occupied by another creature
        // (player occupancy will be handled by MovementSystem as a bump-to-attack)
        if (IsPositionOccupiedByCreature(nextPos.Value))
        {
            // Position is blocked - clear path and repath around the obstacle immediately
            ai.ClearPath();

            // Recalculate path around the blocking creature
            var newPath = PathfindingHelper.FindPath(entity.GridPosition, goal, _mapSystem, _entityManager, _player);
            if (newPath != null && newPath.Count > 0)
            {
                ai.CurrentPath = newPath;

                // Try to move on the new path (recursive call, but only one level deep)
                GridPosition? newNextPos = ai.GetNextPosition();
                if (newNextPos != null && !IsPositionOccupiedByCreature(newNextPos.Value))
                {
                    // New path is clear, move along it
                    Vector2I newDirection = new Vector2I(
                        newNextPos.Value.X - entity.GridPosition.X,
                        newNextPos.Value.Y - entity.GridPosition.Y
                    );

                    var repathMoveAction = new MoveAction(newDirection);
                    entity.ExecuteAction(repathMoveAction, _actionContext);
                }
            }
            return;
        }

        // Calculate direction to next position
        Vector2I direction = new Vector2I(
            nextPos.Value.X - entity.GridPosition.X,
            nextPos.Value.Y - entity.GridPosition.Y
        );

        // Execute movement via action system
        var moveAction = new MoveAction(direction);
        entity.ExecuteAction(moveAction, _actionContext);
    }

    /// <summary>
    /// Wanders randomly within search radius of a position.
    /// </summary>
    private void WanderNearPosition(AIComponent ai, BaseEntity entity, GridPosition center)
    {
        // Try random directions within search radius
        List<Vector2I> possibleDirections = new List<Vector2I>();

        Vector2I[] allDirections = {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right,
            new Vector2I(-1, -1), new Vector2I(1, -1),
            new Vector2I(-1, 1), new Vector2I(1, 1)
        };

        foreach (Vector2I dir in allDirections)
        {
            GridPosition newPos = new GridPosition(
                entity.GridPosition.X + dir.X,
                entity.GridPosition.Y + dir.Y
            );

            // Check if within search radius and walkable
            int distance = DistanceHelper.ChebyshevDistance(newPos, center);
            if (distance <= ai.SearchRadius && _mapSystem.IsWalkable(newPos))
            {
                possibleDirections.Add(dir);
            }
        }

        // Pick random direction
        if (possibleDirections.Count > 0)
        {
            int randomIndex = GD.RandRange(0, possibleDirections.Count - 1);
            Vector2I direction = possibleDirections[randomIndex];

            var moveAction = new MoveAction(direction);
            entity.ExecuteAction(moveAction, _actionContext);
        }
    }

    /// <summary>
    /// Checks if a position is occupied by another creature (not the player).
    /// </summary>
    private bool IsPositionOccupiedByCreature(GridPosition position)
    {
        return _entityManager.GetEntityAtPosition(position) != null;
    }

    public override void _ExitTree()
    {
        // Cleanup signal connections
        if (_turnManager != null)
        {
            _turnManager.CreatureTurnsStarted -= OnCreatureTurnsStarted;
        }
    }
}
