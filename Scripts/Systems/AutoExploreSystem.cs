using Godot;
using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages automatic exploration for the player.
/// Finds nearest unexplored tiles or autopickup items and paths to them.
/// Stops on interrupts: enemies visible, items collected, no valid targets.
/// </summary>
public partial class AutoExploreSystem : Node
{
    /// <summary>
    /// Emitted when autoexplore starts.
    /// </summary>
    [Signal]
    public delegate void AutoExploreStartedEventHandler();

    /// <summary>
    /// Emitted when there's nothing left to explore.
    /// </summary>
    [Signal]
    public delegate void ExplorationCompleteEventHandler();

    private Player _player;
    private MapSystem _mapSystem;
    private EntityManager _entityManager;
    private PlayerVisionSystem _visionSystem;
    private TurnManager _turnManager;
    private ActionContext _actionContext;

    private bool _isActive = false;
    private Queue<GridPosition>? _currentPath;
    private GridPosition? _currentTarget;
    private bool _waitingForDelay = false;

    /// <summary>
    /// Delay between autoexplore steps in seconds.
    /// </summary>
    private const float StepDelay = 0.08f;

    /// <summary>
    /// Whether autoexplore is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    // Directions for Dijkstra expansion (8-directional)
    private static readonly GridPosition[] Directions =
    {
        new(-1, 0),  // Left
        new(1, 0),   // Right
        new(0, -1),  // Up
        new(0, 1),   // Down
        new(-1, -1), // Up-Left
        new(1, -1),  // Up-Right
        new(-1, 1),  // Down-Left
        new(1, 1)    // Down-Right
    };

    /// <summary>
    /// Initializes the autoexplore system with required dependencies.
    /// </summary>
    public void Initialize(
        Player player,
        MapSystem mapSystem,
        EntityManager entityManager,
        PlayerVisionSystem visionSystem,
        TurnManager turnManager,
        ActionContext actionContext)
    {
        _player = player;
        _mapSystem = mapSystem;
        _entityManager = entityManager;
        _visionSystem = visionSystem;
        _turnManager = turnManager;
        _actionContext = actionContext;

        // Connect to turn manager to take actions each turn
        _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnPlayerTurnStarted));

        // Connect to player item pickup to detect autopickup events
        _player.Connect(Player.SignalName.ItemPickedUp, Callable.From<string, bool, string>(OnItemPickedUp));
    }

    /// <summary>
    /// Starts autoexplore mode.
    /// </summary>
    public void Start()
    {
        if (_isActive)
            return;

        // Check for immediate interrupts before starting - don't start if enemies visible
        var visibleEnemy = GetVisibleEnemy();
        if (visibleEnemy != null)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, $"You spotted a {visibleEnemy.DisplayName}.", Palette.ToHex(Palette.Caution));
            return;
        }

        // Find initial target and path
        if (!FindNextTarget())
        {
            EmitSignal(SignalName.ExplorationComplete);
            return;
        }

        _isActive = true;
        EmitSignal(SignalName.AutoExploreStarted);

        // If it's already the player's turn, take the first step
        if (_turnManager.IsPlayerTurn)
        {
            TakeNextStep();
        }
    }

    /// <summary>
    /// Stops autoexplore mode.
    /// </summary>
    public void Stop()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _currentPath = null;
        _currentTarget = null;
        _waitingForDelay = false;
    }

    /// <summary>
    /// Called when the player's turn starts. Takes the next autoexplore step if active.
    /// </summary>
    private void OnPlayerTurnStarted()
    {
        if (!_isActive || _waitingForDelay)
            return;

        // Check for interrupts each turn
        var visibleEnemy = GetVisibleEnemy();
        if (visibleEnemy != null)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, $"You spotted a {visibleEnemy.DisplayName}.", Palette.ToHex(Palette.Caution));
            Stop();
            return;
        }

        // Add delay for watchable pacing
        _waitingForDelay = true;
        GetTree().CreateTimer(StepDelay).Connect("timeout", Callable.From(OnDelayComplete), (uint)ConnectFlags.OneShot);
    }

    /// <summary>
    /// Called after the step delay completes.
    /// </summary>
    private void OnDelayComplete()
    {
        _waitingForDelay = false;

        if (!_isActive)
            return;

        // Re-check for enemies after delay (they may have moved)
        var visibleEnemy = GetVisibleEnemy();
        if (visibleEnemy != null)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, $"You spotted a {visibleEnemy.DisplayName}.", Palette.ToHex(Palette.Caution));
            Stop();
            return;
        }

        TakeNextStep();
    }

    /// <summary>
    /// Called when the player picks up an item.
    /// Stops autoexplore after autopickup so player can reassess.
    /// </summary>
    private void OnItemPickedUp(string itemName, bool success, string message)
    {
        if (_isActive && success)
        {
            Stop();
        }
    }

    /// <summary>
    /// Takes the next step along the current path.
    /// </summary>
    private void TakeNextStep()
    {
        // Check if we need a new target (path exhausted or target reached)
        if (_currentPath == null || _currentPath.Count == 0)
        {
            if (!FindNextTarget())
            {
                EmitSignal(SignalName.ExplorationComplete);
                Stop();
                return;
            }
        }

        // Get next position from path
        if (_currentPath == null || _currentPath.Count == 0)
        {
            Stop();
            return;
        }

        var nextPos = _currentPath.Peek();

        // Safety check: verify the next tile is still safe to move to
        if (!IsSafeToMove(nextPos))
        {
            // Recalculate path
            _currentPath = null;
            if (!FindNextTarget())
            {
                Stop();
                return;
            }

            if (_currentPath == null || _currentPath.Count == 0)
            {
                Stop();
                return;
            }

            nextPos = _currentPath.Peek();
        }

        // Dequeue the position we're about to move to
        _currentPath.Dequeue();

        // Calculate direction to next position
        var currentPos = _player.GridPosition;
        var direction = new Vector2I(nextPos.X - currentPos.X, nextPos.Y - currentPos.Y);

        // Execute movement action
        var moveAction = new MoveAction(direction);
        _player.ExecuteAction(moveAction, _actionContext);
    }

    /// <summary>
    /// Finds the next autoexplore target and calculates a path to it.
    /// Uses Dijkstra to find the closest unexplored tile or autopickup item.
    /// </summary>
    private bool FindNextTarget()
    {
        var target = FindClosestAutoExploreTarget();
        if (target == null)
        {
            return false;
        }

        _currentTarget = target;

        // Calculate path using A*
        _currentPath = AStarPathfinder.FindPath(
            _player.GridPosition,
            target.Value,
            _mapSystem,
            _entityManager,
            _player
        );

        return _currentPath != null && _currentPath.Count > 0;
    }

    /// <summary>
    /// Uses Dijkstra's algorithm to find the closest reachable target.
    /// Targets are: unexplored walkable tiles OR autopickup items.
    /// Returns null if no valid target exists.
    /// </summary>
    private GridPosition? FindClosestAutoExploreTarget()
    {
        var start = _player.GridPosition;
        var visited = new HashSet<GridPosition>();
        var queue = new PriorityQueue<GridPosition, int>();

        queue.Enqueue(start, 0);
        var distances = new Dictionary<GridPosition, int> { [start] = 0 };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            int currentDist = distances[current];

            // Check if this is a valid target (skip the starting position)
            if (current != start && IsAutoExploreTarget(current))
            {
                return current;
            }

            // Expand to neighbors
            foreach (var dir in Directions)
            {
                var neighbor = new GridPosition(current.X + dir.X, current.Y + dir.Y);

                if (visited.Contains(neighbor))
                    continue;

                if (!_mapSystem.IsInBounds(neighbor))
                    continue;

                // Only expand through explored, walkable tiles (safe pathing)
                // But allow targeting unexplored tiles adjacent to explored ones
                if (!_mapSystem.IsWalkable(neighbor))
                    continue;

                // Don't path through hostile creatures
                var entityAtPos = _entityManager.GetEntityAtPosition(neighbor);
                if (entityAtPos != null && !entityAtPos.IsWalkable && Faction.Player.IsHostileTo(entityAtPos.Faction))
                    continue;

                int newDist = currentDist + 1;

                if (!distances.TryGetValue(neighbor, out int existingDist) || newDist < existingDist)
                {
                    distances[neighbor] = newDist;
                    queue.Enqueue(neighbor, newDist);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a position is a valid autoexplore target.
    /// Valid targets: unexplored walkable tiles, autopickup items, or gold.
    /// </summary>
    private bool IsAutoExploreTarget(GridPosition position)
    {
        // Check for autopickup items and gold
        var entities = _entityManager.GetEntitiesAtPosition(position);
        foreach (var entity in entities)
        {
            // Gold piles
            if (entity is Entities.Gold)
            {
                return true;
            }

            // Autopickup items (potions, scrolls, ammo)
            if (entity.ItemData != null && entity.ItemData.AutoPickup)
            {
                return true;
            }
        }

        // Check if tile is unexplored and walkable
        if (!_visionSystem.IsExplored(position) && _mapSystem.IsWalkable(position))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if any hostile creatures are visible to the player.
    /// Returns the first visible hostile entity found, or null if none.
    /// Only considers actual creatures (non-walkable entities), not items.
    /// </summary>
    private BaseEntity? GetVisibleEnemy()
    {
        foreach (var entity in _entityManager.GetAllEntities())
        {
            // Skip walkable entities (items, stairs, etc.) - not threats
            if (entity.IsWalkable)
                continue;

            // Skip non-hostile entities
            if (!Faction.Player.IsHostileTo(entity.Faction))
                continue;

            // Skip dead entities
            if (entity.IsDead)
                continue;

            // Check if visible
            if (_visionSystem.IsVisible(entity.GridPosition))
            {
                return entity;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if it's safe to move to a position.
    /// </summary>
    private bool IsSafeToMove(GridPosition position)
    {
        // Must be walkable
        if (!_mapSystem.IsWalkable(position))
            return false;

        // Check for hostile creatures at the position
        var entity = _entityManager.GetEntityAtPosition(position);
        if (entity != null && !entity.IsWalkable && Faction.Player.IsHostileTo(entity.Faction))
            return false;

        return true;
    }

    /// <summary>
    /// Cancels autoexplore if any key is pressed.
    /// Called by InputHandler when autoexplore is active.
    /// </summary>
    public void OnAnyKeyPressed()
    {
        if (_isActive)
        {
            Stop();
        }
    }

    public override void _ExitTree()
    {
        if (_turnManager != null)
        {
            _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnPlayerTurnStarted));
        }

        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.ItemPickedUp, Callable.From<string, bool, string>(OnItemPickedUp));
        }
    }
}
