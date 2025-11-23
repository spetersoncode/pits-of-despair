using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Targeting;

namespace PitsOfDespair.Systems;

/// <summary>
/// Unified cursor targeting system supporting both examine mode and action targeting.
/// Handles cursor movement, target validation, creature cycling, and visual feedback.
/// </summary>
public partial class CursorTargetingSystem : Node
{
	/// <summary>
	/// Targeting mode types
	/// </summary>
	public enum TargetingMode
	{
		/// <summary>Read-only examination of entities</summary>
		Examine,
		/// <summary>Ranged attack targeting</summary>
		RangedAttack,
		/// <summary>Reach attack targeting (melee with range > 1)</summary>
		ReachAttack,
		/// <summary>Targeted item usage (wands, scrolls, etc.)</summary>
		TargetedItem
	}

	[Signal]
	public delegate void CursorStartedEventHandler(int mode);

	[Signal]
	public delegate void CursorMovedEventHandler(BaseEntity entity);

	[Signal]
	public delegate void CursorCanceledEventHandler(int mode);

	[Signal]
    public delegate void EntityExaminedEventHandler(BaseEntity entity);

	[Signal]
	public delegate void TargetConfirmedEventHandler(Vector2I targetPosition);

    /// <summary>
    /// Examines the target at the current cursor position.
    /// Emits EntityExamined signal if an entity is present.
    /// </summary>
    public void ExamineTarget()
    {
        if (!_isActive || _currentMode != TargetingMode.Examine)
            return;

        var entity = GetEntityAtCursor();
        if (entity != null)
        {
            EmitSignal(SignalName.EntityExamined, entity);
        }
    }

	private GridPosition _cursorPosition;
	private GridPosition _originPosition;
	private TargetingMode _currentMode;
	private bool _isActive = false;

	// Action mode state
	private int _maxRange;
	private HashSet<GridPosition> _validTiles;           // Full range for overlay display
	private HashSet<GridPosition> _validCursorPositions; // Valid positions for cursor movement
	private List<BaseEntity> _validCreatureTargets;
	private int _currentCreatureIndex = -1;
	private bool _requiresCreature = true;
	private bool _useGridDistance = false;

	// Unified targeting state
	private TargetingDefinition? _targetingDefinition;
	private TargetingHandler? _targetingHandler;
	private BaseEntity? _caster;
	private ActionContext? _actionContext;
	private Key? _initiatingKey;

	// Dependencies
	private PlayerVisionSystem _visionSystem;
	private MapSystem _mapSystem;
	private EntityManager _entityManager;

	/// <summary>
	/// Gets whether targeting is currently active.
	/// </summary>
	public bool IsActive => _isActive;

	/// <summary>
	/// Gets the current targeting mode.
	/// </summary>
	public TargetingMode CurrentMode => _currentMode;

	/// <summary>
	/// Gets the current cursor position.
	/// </summary>
	public GridPosition CursorPosition => _cursorPosition;

	/// <summary>
	/// Gets the origin position (for action modes).
	/// </summary>
	public GridPosition OriginPosition => _originPosition;

	/// <summary>
	/// Gets all valid tiles (for action modes). Null for examine mode.
	/// </summary>
	public HashSet<GridPosition> ValidTiles => _validTiles;

	/// <summary>
	/// Gets whether this targeting mode requires a creature target.
	/// Only relevant for action modes.
	/// </summary>
	public bool RequiresCreature => _requiresCreature;

	/// <summary>
	/// Gets the current targeting definition, if using unified targeting.
	/// </summary>
	public TargetingDefinition? TargetingDefinition => _targetingDefinition;

	/// <summary>
	/// Gets the current targeting handler, if using unified targeting.
	/// </summary>
	public TargetingHandler? TargetingHandler => _targetingHandler;

	/// <summary>
	/// Gets the key that initiated targeting (for confirmation).
	/// </summary>
	public Key? InitiatingKey => _initiatingKey;

	/// <summary>
	/// Gets the current affected positions for area preview.
	/// </summary>
	public List<GridPosition>? AffectedPositions { get; private set; }

	/// <summary>
	/// Initializes the cursor targeting system with required dependencies.
	/// </summary>
	public void Initialize(PlayerVisionSystem visionSystem, MapSystem mapSystem, EntityManager entityManager)
	{
		_visionSystem = visionSystem;
		_mapSystem = mapSystem;
		_entityManager = entityManager;
	}

	/// <summary>
	/// Starts examine mode at the player's position.
	/// Uses vision-based validation with no range limits.
	/// </summary>
	/// <param name="playerPosition">The player's current position</param>
	public void StartExamine(GridPosition playerPosition)
	{
		_currentMode = TargetingMode.Examine;
		_isActive = true;
		_cursorPosition = playerPosition;
		_originPosition = playerPosition;

		// Clear action mode state
		_validTiles = null;
		_validCursorPositions = null;
		_validCreatureTargets = null;
		_currentCreatureIndex = -1;
		_requiresCreature = false;

		EmitSignal(SignalName.CursorStarted, (int)_currentMode);

		// Don't emit CursorMoved for initial position - player knows they're standing there
	}

	/// <summary>
	/// Starts action targeting mode (ranged attack, reach attack, or targeted item).
	/// Uses FOV-based range calculation with creature filtering.
	/// </summary>
	/// <param name="mode">The targeting mode (RangedAttack, ReachAttack, or TargetedItem)</param>
	/// <param name="origin">The position to target from (usually the player)</param>
	/// <param name="range">Maximum targeting range</param>
	/// <param name="requiresCreature">Whether targeting requires a creature (shows warning on empty tiles)</param>
	/// <param name="useGridDistance">Use Chebyshev (grid) distance instead of Euclidean</param>
	/// <param name="validPositions">Optional pre-calculated valid positions (overrides FOV calculation if provided)</param>
	/// <param name="initiatingKey">Optional key that initiated targeting (can be used to confirm)</param>
	public void StartActionTargeting(TargetingMode mode, GridPosition origin, int range, bool requiresCreature = true, bool useGridDistance = false, HashSet<GridPosition> validPositions = null, Key? initiatingKey = null)
	{
		if (mode == TargetingMode.Examine)
		{
			GD.PushError("Use StartExamine() for examine mode");
			return;
		}

		_currentMode = mode;
		_originPosition = origin;
		_maxRange = range;
		_requiresCreature = requiresCreature;
		_useGridDistance = useGridDistance;
		_initiatingKey = initiatingKey;
		_isActive = true;

		// Use provided valid positions or calculate using FOV
		if (validPositions != null)
		{
			_validTiles = validPositions;
		}
		else
		{
			var distanceMetric = useGridDistance ? DistanceMetric.Chebyshev : DistanceMetric.Euclidean;
			_validTiles = FOVCalculator.CalculateVisibleTiles(origin, range, _mapSystem, distanceMetric);
		}

		// For legacy action targeting, cursor positions match the overlay
		_validCursorPositions = _validTiles;

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
				// Only target hostile entities with health (attackable creatures)
				// Excludes player faction (allies) from hostile targeting
				if (entity.GetNodeOrNull<Components.HealthComponent>("HealthComponent") != null &&
					entity.Faction != Faction.Player)
				{
					_validCreatureTargets.Add(entity);
				}
			}
		}

		// Start with nearest creature if any exist
		if (_validCreatureTargets.Count > 0)
		{
			// Sort by distance from origin using the appropriate metric
			_validCreatureTargets = _useGridDistance
				? _validCreatureTargets.OrderBy(e => DistanceHelper.ChebyshevDistance(origin, e.GridPosition)).ToList()
				: _validCreatureTargets.OrderBy(e => DistanceHelper.EuclideanDistance(origin, e.GridPosition)).ToList();

			_currentCreatureIndex = 0;
			_cursorPosition = _validCreatureTargets[0].GridPosition;
		}
		else
		{
			// No creatures, start cursor at origin
			_currentCreatureIndex = -1;
			_cursorPosition = origin;
		}

		EmitSignal(SignalName.CursorStarted, (int)_currentMode);
	}

	/// <summary>
	/// Starts targeting using the unified targeting system with a TargetingDefinition.
	/// This is the preferred method for new code.
	/// </summary>
	/// <param name="caster">The entity doing the targeting</param>
	/// <param name="definition">The targeting definition</param>
	/// <param name="context">The action context for handler operations</param>
	/// <param name="initiatingKey">Optional key that initiated targeting (can be used to confirm)</param>
	public void StartTargeting(BaseEntity caster, TargetingDefinition definition, ActionContext context, Key? initiatingKey = null)
	{
		if (!definition.RequiresSelection)
		{
			GD.PushWarning("StartTargeting called for targeting type that doesn't require selection");
			return;
		}

		_caster = caster;
		_targetingDefinition = definition;
		_actionContext = context;
		_targetingHandler = TargetingHandler.CreateForDefinition(definition);
		_initiatingKey = initiatingKey;

		_currentMode = TargetingMode.TargetedItem; // Use generic mode for unified targeting
		_originPosition = caster.GridPosition;
		_maxRange = definition.Range;
		_requiresCreature = definition.Filter != TargetFilter.Tile;
		_useGridDistance = definition.Metric == DistanceMetric.Chebyshev;
		_isActive = true;

		// Calculate full visible range for the overlay (always shows entire targeting range)
		int range = definition.Range > 0 ? definition.Range : 1;
		_validTiles = FOVCalculator.CalculateVisibleTiles(_originPosition, range, context.MapSystem, definition.Metric);

		// Get valid target positions from the handler (may be a subset for creature-only targeting)
		var validTargetPositions = _targetingHandler.GetValidTargetPositions(caster, definition, context);

		// Set valid cursor positions based on filter type
		// Tile targeting allows free movement within range; creature targeting restricts to targets
		_validCursorPositions = definition.Filter == TargetFilter.Tile
			? _validTiles
			: new HashSet<GridPosition>(validTargetPositions);

		// Build creature target list from valid target positions
		_validCreatureTargets = new List<BaseEntity>();
		foreach (var tile in validTargetPositions)
		{
			if (tile == _originPosition)
				continue;

			var entity = _entityManager.GetEntityAtPosition(tile);
			if (entity != null && IsEntityValidForFilter(caster, entity, definition.Filter))
			{
				_validCreatureTargets.Add(entity);
			}
		}

		// Sort by distance and select initial target
		if (_validCreatureTargets.Count > 0)
		{
			_validCreatureTargets = _useGridDistance
				? _validCreatureTargets.OrderBy(e => DistanceHelper.ChebyshevDistance(_originPosition, e.GridPosition)).ToList()
				: _validCreatureTargets.OrderBy(e => DistanceHelper.EuclideanDistance(_originPosition, e.GridPosition)).ToList();

			_currentCreatureIndex = 0;
			_cursorPosition = _validCreatureTargets[0].GridPosition;
		}
		else if (_validTiles.Count > 0)
		{
			// No creatures, start at nearest valid tile
			_currentCreatureIndex = -1;
			_cursorPosition = _validTiles.OrderBy(t => DistanceHelper.ChebyshevDistance(_originPosition, t)).First();
		}
		else
		{
			// No valid targets at all
			_currentCreatureIndex = -1;
			_cursorPosition = _originPosition;
		}

		// Update area preview if applicable
		UpdateAffectedPositions();

		EmitSignal(SignalName.CursorStarted, (int)_currentMode);
	}

	/// <summary>
	/// Checks if an entity is valid for the given target filter.
	/// </summary>
	private bool IsEntityValidForFilter(BaseEntity caster, BaseEntity target, TargetFilter filter)
	{
		if (target.GetNodeOrNull<Components.HealthComponent>("HealthComponent") == null)
			return false;

		return filter switch
		{
			TargetFilter.Enemy => caster.Faction != target.Faction,
			TargetFilter.Ally => caster.Faction == target.Faction,
			TargetFilter.Creature => true,
			TargetFilter.Self => target == caster,
			TargetFilter.Tile => true,
			_ => caster.Faction != target.Faction // Default to enemy
		};
	}

	/// <summary>
	/// Updates the affected positions for area preview.
	/// UI can access via the AffectedPositions property after CursorMoved signal.
	/// </summary>
	private void UpdateAffectedPositions()
	{
		if (_targetingHandler == null || _targetingDefinition == null || _caster == null || _actionContext == null)
		{
			AffectedPositions = null;
			return;
		}

		AffectedPositions = _targetingHandler.GetAffectedPositions(_caster, _cursorPosition, _targetingDefinition, _actionContext);
	}

	/// <summary>
	/// Stops targeting mode and clears all state.
	/// </summary>
	public void Stop()
	{
		_isActive = false;
		_validTiles = null;
		_validCursorPositions = null;
		_validCreatureTargets = null;
		_currentCreatureIndex = -1;
		_targetingDefinition = null;
		_targetingHandler = null;
		_caster = null;
		_actionContext = null;
		_initiatingKey = null;
		AffectedPositions = null;
	}

	/// <summary>
	/// Moves the cursor in the specified direction.
	/// Validation depends on current mode (vision for examine, range for actions).
	/// </summary>
	/// <param name="direction">Direction vector to move</param>
	/// <returns>True if the cursor moved, false if blocked</returns>
	public bool MoveCursor(Vector2I direction)
	{
		if (!_isActive)
			return false;

		var newPosition = _cursorPosition.Add(direction);

		// Validate based on mode
		bool isValid = _currentMode == TargetingMode.Examine
			? _visionSystem.IsVisible(newPosition)
			: _validCursorPositions != null && _validCursorPositions.Contains(newPosition);

		if (isValid)
		{
			_cursorPosition = newPosition;

			// Update creature index for action modes
			if (_currentMode != TargetingMode.Examine)
			{
				UpdateCreatureIndexFromPosition();
				UpdateAffectedPositions();
			}

			// Emit cursor moved signal
			var entity = _entityManager.GetEntityAtPosition(_cursorPosition);
			EmitSignal(SignalName.CursorMoved, entity);

			return true;
		}

		return false;
	}

	/// <summary>
	/// Cycles to the next valid creature target (action modes only).
	/// </summary>
	public void CycleNextTarget()
	{
		if (!_isActive || _currentMode == TargetingMode.Examine)
			return;

		if (_validCreatureTargets == null || _validCreatureTargets.Count == 0)
			return;

		_currentCreatureIndex = (_currentCreatureIndex + 1) % _validCreatureTargets.Count;
		_cursorPosition = _validCreatureTargets[_currentCreatureIndex].GridPosition;

		UpdateAffectedPositions();

		// Emit cursor moved signal
		var entity = _validCreatureTargets[_currentCreatureIndex];
		EmitSignal(SignalName.CursorMoved, entity);
	}

	/// <summary>
	/// Cycles to the previous valid creature target (action modes only).
	/// </summary>
	public void CyclePreviousTarget()
	{
		if (!_isActive || _currentMode == TargetingMode.Examine)
			return;

		if (_validCreatureTargets == null || _validCreatureTargets.Count == 0)
			return;

		_currentCreatureIndex--;
		if (_currentCreatureIndex < 0)
			_currentCreatureIndex = _validCreatureTargets.Count - 1;

		_cursorPosition = _validCreatureTargets[_currentCreatureIndex].GridPosition;

		UpdateAffectedPositions();

		// Emit cursor moved signal
		var entity = _validCreatureTargets[_currentCreatureIndex];
		EmitSignal(SignalName.CursorMoved, entity);
	}

	/// <summary>
	/// Confirms the current target (action modes only).
	/// Emits TargetConfirmed signal and stops targeting.
	/// </summary>
	public void ConfirmTarget()
	{
		if (!_isActive || _currentMode == TargetingMode.Examine)
			return;

		GridPosition targetPosition = _cursorPosition;
		Stop();
		EmitSignal(SignalName.TargetConfirmed, targetPosition.ToVector2I());
	}

	/// <summary>
	/// Cancels targeting and emits the CursorCanceled signal.
	/// </summary>
	public void Cancel()
	{
		if (!_isActive)
			return;

		var mode = _currentMode;
		Stop();
		EmitSignal(SignalName.CursorCanceled, (int)mode);
	}

	/// <summary>
	/// Gets the entity at the cursor position, if any.
	/// </summary>
	public BaseEntity GetEntityAtCursor()
	{
		if (!_isActive)
			return null;

		return _entityManager.GetEntityAtPosition(_cursorPosition);
	}

	/// <summary>
	/// Gets the creature at the cursor position (entity with HealthComponent), if any.
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
	/// Updates the creature index when cursor is moved manually (action modes only).
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
