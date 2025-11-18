using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages targeting cursor for ranged attacks and other targeted abilities.
/// Handles cursor movement, target cycling, and validation.
/// </summary>
public partial class TargetingSystem : Node
{
    [Signal]
    public delegate void TargetConfirmedEventHandler(Vector2I targetPosition);

    [Signal]
    public delegate void TargetCanceledEventHandler();

    private GridPosition _cursorPosition;
    private GridPosition _originPosition;
    private int _maxRange;
    private HashSet<GridPosition> _validTiles;
    private List<BaseEntity> _validCreatureTargets;
    private int _currentCreatureIndex = -1;
    private bool _isActive = false;
    private bool _requiresCreature = true;

    private MapSystem _mapSystem;
    private EntityManager _entityManager;

    /// <summary>
    /// Gets whether targeting mode is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets whether this targeting mode requires a creature target.
    /// </summary>
    public bool RequiresCreature => _requiresCreature;

    /// <summary>
    /// Gets the current cursor position.
    /// </summary>
    public GridPosition CursorPosition => _cursorPosition;

    /// <summary>
    /// Gets all valid tiles within range and line of sight.
    /// </summary>
    public HashSet<GridPosition> ValidTiles => _validTiles;

    /// <summary>
    /// Initializes the targeting system with required dependencies.
    /// </summary>
    public void Initialize(MapSystem mapSystem, EntityManager entityManager)
    {
        _mapSystem = mapSystem;
        _entityManager = entityManager;
    }

    /// <summary>
    /// Starts targeting mode from the specified origin position with the given range.
    /// </summary>
    /// <param name="origin">The position to target from (usually the player)</param>
    /// <param name="range">Maximum targeting range</param>
    /// <param name="requiresCreature">Whether targeting requires a creature (shows warning on empty tiles)</param>
    public void StartTargeting(GridPosition origin, int range, bool requiresCreature = true, bool useGridDistance = false)
    {
        _originPosition = origin;
        _maxRange = range;
        _requiresCreature = requiresCreature;
        _isActive = true;

        // Calculate valid tiles using FOV with appropriate distance metric
        // Grid distance (Chebyshev) for tactical targeting, Euclidean for vision-based targeting
        var distanceMetric = useGridDistance ? Helpers.DistanceMetric.Chebyshev : Helpers.DistanceMetric.Euclidean;
        _validTiles = FOVCalculator.CalculateVisibleTiles(origin, range, _mapSystem, distanceMetric);

        // Find all creatures in valid tiles
        _validCreatureTargets = new List<BaseEntity>();
        foreach (var tile in _validTiles)
        {
            // Skip the origin tile (don't target yourself)
            if (tile == origin)
                continue;

            var entity = _entityManager.GetEntityAtPosition(tile);
            if (entity != null)
            {
                // Only target entities with health (attackable creatures)
                if (entity.GetNodeOrNull<Components.HealthComponent>("HealthComponent") != null)
                {
                    _validCreatureTargets.Add(entity);
                }
            }
        }

        // Start with nearest creature if any exist
        if (_validCreatureTargets.Count > 0)
        {
            // Sort by distance from origin
            _validCreatureTargets = _validCreatureTargets
                .OrderBy(e => DistanceHelper.ChebyshevDistance(origin, e.GridPosition))
                .ToList();

            _currentCreatureIndex = 0;
            _cursorPosition = _validCreatureTargets[0].GridPosition;

            GD.Print($"[Targeting] Auto-selected: {_validCreatureTargets[0].DisplayName} at {_validCreatureTargets[0].GridPosition}");
            GD.Print($"[Targeting] Cursor set to: {_cursorPosition}");
        }
        else
        {
            // No creatures, start cursor at origin
            _currentCreatureIndex = -1;
            _cursorPosition = origin;
        }
    }

    /// <summary>
    /// Stops targeting mode.
    /// </summary>
    public void StopTargeting()
    {
        _isActive = false;
        _validTiles = null;
        _validCreatureTargets = null;
        _currentCreatureIndex = -1;
    }

    /// <summary>
    /// Moves the cursor in the specified direction.
    /// </summary>
    /// <param name="direction">Direction vector to move</param>
    /// <returns>True if the cursor moved, false if blocked</returns>
    public bool MoveCursor(Vector2I direction)
    {
        if (!_isActive)
            return false;

        var newPosition = _cursorPosition.Add(direction);

        // Check if new position is within valid tiles
        if (_validTiles.Contains(newPosition))
        {
            _cursorPosition = newPosition;

            // Update creature index if cursor is now on a creature
            UpdateCreatureIndexFromPosition();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Cycles to the next valid creature target.
    /// </summary>
    public void CycleNextTarget()
    {
        if (!_isActive || _validCreatureTargets == null || _validCreatureTargets.Count == 0)
            return;

        _currentCreatureIndex = (_currentCreatureIndex + 1) % _validCreatureTargets.Count;
        _cursorPosition = _validCreatureTargets[_currentCreatureIndex].GridPosition;
    }

    /// <summary>
    /// Cycles to the previous valid creature target.
    /// </summary>
    public void CyclePreviousTarget()
    {
        if (!_isActive || _validCreatureTargets == null || _validCreatureTargets.Count == 0)
            return;

        _currentCreatureIndex--;
        if (_currentCreatureIndex < 0)
            _currentCreatureIndex = _validCreatureTargets.Count - 1;

        _cursorPosition = _validCreatureTargets[_currentCreatureIndex].GridPosition;
    }

    /// <summary>
    /// Confirms the current target and emits the TargetConfirmed signal.
    /// </summary>
    public void ConfirmTarget()
    {
        if (!_isActive)
            return;

        GridPosition targetPosition = _cursorPosition;
        StopTargeting();
        EmitSignal(SignalName.TargetConfirmed, targetPosition.ToVector2I());
    }

    /// <summary>
    /// Cancels targeting and emits the TargetCanceled signal.
    /// </summary>
    public void CancelTarget()
    {
        if (!_isActive)
            return;

        StopTargeting();
        EmitSignal(SignalName.TargetCanceled);
    }

    /// <summary>
    /// Gets the creature at the cursor position, if any.
    /// </summary>
    public BaseEntity GetCreatureAtCursor()
    {
        if (!_isActive)
            return null;

        var entity = _entityManager.GetEntityAtPosition(_cursorPosition);
        if (entity != null)
        {
            if (entity.GetNodeOrNull<Components.HealthComponent>("HealthComponent") != null)
            {
                return entity;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the cursor is currently on a valid creature.
    /// </summary>
    public bool IsCursorOnCreature()
    {
        return GetCreatureAtCursor() != null;
    }

    /// <summary>
    /// Updates the creature index when cursor is moved manually.
    /// </summary>
    private void UpdateCreatureIndexFromPosition()
    {
        if (_validCreatureTargets == null || _validCreatureTargets.Count == 0)
        {
            _currentCreatureIndex = -1;
            return;
        }

        for (int i = 0; i < _validCreatureTargets.Count; i++)
        {
            if (_validCreatureTargets[i].GridPosition == _cursorPosition)
            {
                _currentCreatureIndex = i;
                return;
            }
        }

        // Cursor is not on a creature
        _currentCreatureIndex = -1;
    }
}
