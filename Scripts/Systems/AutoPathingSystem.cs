using Godot;
using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Base class for automatic pathing systems (autoexplore, auto-stairs, etc.).
/// Handles turn-by-turn movement, interrupt checking, and path following.
/// Subclasses implement target finding and completion behavior.
/// </summary>
public abstract partial class AutoPathingSystem : Node
{
    /// <summary>
    /// Emitted when auto-pathing starts.
    /// </summary>
    [Signal]
    public delegate void StartedEventHandler();

    /// <summary>
    /// Emitted when auto-pathing stops (any reason).
    /// </summary>
    [Signal]
    public delegate void StoppedEventHandler();

    protected Player _player;
    protected MapSystem _mapSystem;
    protected EntityManager _entityManager;
    protected PlayerVisionSystem _visionSystem;
    protected TurnManager _turnManager;
    protected ActionContext _actionContext;

    protected bool _isActive = false;
    protected Queue<GridPosition>? _currentPath;
    protected GridPosition? _currentTarget;
    protected bool _waitingForDelay = false;

    /// <summary>
    /// Delay between steps in seconds.
    /// </summary>
    protected const float StepDelay = 0.08f;

    /// <summary>
    /// Whether auto-pathing is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    // Directions for pathfinding expansion (8-directional)
    protected static readonly GridPosition[] Directions =
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
    /// Initializes the auto-pathing system with required dependencies.
    /// </summary>
    public virtual void Initialize(
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

        _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnPlayerTurnStarted));
    }

    /// <summary>
    /// Starts auto-pathing mode.
    /// </summary>
    public virtual void Start()
    {
        if (_isActive)
            return;

        // Check for immediate interrupts before starting
        var visibleEnemy = GetVisibleEnemy();
        if (visibleEnemy != null)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, $"You spotted a {visibleEnemy.DisplayName}.", Palette.ToHex(Palette.Caution));
            return;
        }

        // Find initial target and path
        if (!FindNextTarget())
        {
            OnNoTargetFound();
            return;
        }

        _isActive = true;
        EmitSignal(SignalName.Started);
        OnStarted();

        // If it's already the player's turn, take the first step
        if (_turnManager.IsPlayerTurn)
        {
            TakeNextStep();
        }
    }

    /// <summary>
    /// Stops auto-pathing mode.
    /// </summary>
    public virtual void Stop()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _currentPath = null;
        _currentTarget = null;
        _waitingForDelay = false;
        EmitSignal(SignalName.Stopped);
    }

    /// <summary>
    /// Called when the player's turn starts.
    /// </summary>
    protected virtual void OnPlayerTurnStarted()
    {
        if (!_isActive || _waitingForDelay)
            return;

        // Check for interrupts each turn
        if (CheckInterrupts())
        {
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
    protected virtual void OnDelayComplete()
    {
        _waitingForDelay = false;

        if (!_isActive)
            return;

        // Re-check for interrupts after delay
        if (CheckInterrupts())
        {
            Stop();
            return;
        }

        TakeNextStep();
    }

    /// <summary>
    /// Checks for conditions that should interrupt auto-pathing.
    /// Returns true if an interrupt was found.
    /// </summary>
    protected virtual bool CheckInterrupts()
    {
        var visibleEnemy = GetVisibleEnemy();
        if (visibleEnemy != null)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, $"You spotted a {visibleEnemy.DisplayName}.", Palette.ToHex(Palette.Caution));
            return true;
        }

        if (HasHeardCreature())
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, "You hear something in the distance.", Palette.ToHex(Palette.Caution));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Takes the next step along the current path.
    /// </summary>
    protected virtual void TakeNextStep()
    {
        // Check if we need a new target (path exhausted or target reached)
        if (_currentPath == null || _currentPath.Count == 0)
        {
            if (!FindNextTarget())
            {
                OnTargetReached();
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
    /// Finds the next target and calculates a path to it.
    /// Returns true if a valid target and path were found.
    /// </summary>
    protected virtual bool FindNextTarget()
    {
        var target = FindTarget();
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
    /// Finds the target position to path to.
    /// Override in subclasses to implement specific target-finding logic.
    /// </summary>
    protected abstract GridPosition? FindTarget();

    /// <summary>
    /// Called when auto-pathing starts successfully.
    /// Override in subclasses for additional start behavior.
    /// </summary>
    protected virtual void OnStarted() { }

    /// <summary>
    /// Called when no target could be found.
    /// Override in subclasses to emit specific signals or messages.
    /// </summary>
    protected abstract void OnNoTargetFound();

    /// <summary>
    /// Called when the target has been reached (or no more targets exist).
    /// Override in subclasses to emit specific signals.
    /// </summary>
    protected abstract void OnTargetReached();

    /// <summary>
    /// Checks if any hostile creatures are visible to the player.
    /// </summary>
    protected BaseEntity? GetVisibleEnemy()
    {
        foreach (var entity in _entityManager.GetAllEntities())
        {
            if (entity.IsWalkable)
                continue;

            if (!Faction.Player.IsHostileTo(entity.Faction))
                continue;

            if (entity.IsDead)
                continue;

            if (_visionSystem.IsVisible(entity.GridPosition))
            {
                return entity;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if any creature is detected via Keen Hearing.
    /// </summary>
    protected bool HasHeardCreature()
    {
        if (_visionSystem == null)
            return false;

        foreach (var entity in _entityManager.GetAllEntities())
        {
            if (entity.Layer != EntityLayer.Creature)
                continue;

            if (_visionSystem.IsHeard(entity))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if it's safe to move to a position.
    /// </summary>
    protected bool IsSafeToMove(GridPosition position)
    {
        if (!_mapSystem.IsWalkable(position))
            return false;

        var entity = _entityManager.GetEntityAtPosition(position);
        if (entity != null && !entity.IsWalkable && Faction.Player.IsHostileTo(entity.Faction))
            return false;

        return true;
    }

    /// <summary>
    /// Cancels auto-pathing if any key is pressed.
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
    }
}
