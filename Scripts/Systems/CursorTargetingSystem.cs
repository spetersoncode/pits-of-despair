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
		/// <summary>Action targeting (skills, items, attacks)</summary>
		Action
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

	private GridPosition _cursorPosition;
	private GridPosition _originPosition;
	private TargetingMode _currentMode;
	private bool _isActive = false;

	// Targeting state
	private HashSet<GridPosition> _rangeTiles;           // Tiles within range for cursor movement and overlay
	private List<BaseEntity> _validCreatureTargets;      // Valid creatures for Tab cycling
	private int _currentCreatureIndex = -1;
	private TargetingDefinition _targetingDefinition;
	private TargetingHandler _targetingHandler;
	private BaseEntity _caster;
	private ActionContext _actionContext;
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
	/// Gets the origin position (caster position for action modes).
	/// </summary>
	public GridPosition OriginPosition => _originPosition;

	/// <summary>
	/// Gets all tiles in range (for overlay display). Null for examine mode.
	/// </summary>
	public HashSet<GridPosition> ValidTiles => _rangeTiles;

	/// <summary>
	/// Gets the current targeting definition.
	/// </summary>
	public TargetingDefinition TargetingDefinition => _targetingDefinition;

	/// <summary>
	/// Gets the current targeting handler.
	/// </summary>
	public TargetingHandler TargetingHandler => _targetingHandler;

	/// <summary>
	/// Gets the key that initiated targeting (for confirmation).
	/// </summary>
	public Key? InitiatingKey => _initiatingKey;

	/// <summary>
	/// Gets the current affected positions for area preview.
	/// </summary>
	public List<GridPosition> AffectedPositions { get; private set; }

	/// <summary>
	/// Gets whether the cursor is currently on a valid target.
	/// Used by TextRenderer for green highlight.
	/// </summary>
	public bool IsOnValidTarget { get; private set; }

	/// <summary>
	/// Initializes the cursor targeting system with required dependencies.
	/// </summary>
	public void Initialize(PlayerVisionSystem visionSystem, MapSystem mapSystem, EntityManager entityManager)
	{
		_visionSystem = visionSystem;
		_mapSystem = mapSystem;
		_entityManager = entityManager;

		_entityManager.Connect(EntityManager.SignalName.EntityRemoved, Callable.From<BaseEntity>(OnEntityRemoved));
	}

	/// <summary>
	/// Handles entity removal by invalidating cached target references.
	/// </summary>
	private void OnEntityRemoved(BaseEntity entity)
	{
		if (!_isActive || _validCreatureTargets == null || !_validCreatureTargets.Contains(entity))
			return;

		int removedIndex = _validCreatureTargets.IndexOf(entity);
		_validCreatureTargets.Remove(entity);

		if (_validCreatureTargets.Count == 0)
		{
			_currentCreatureIndex = -1;
		}
		else if (removedIndex <= _currentCreatureIndex)
		{
			_currentCreatureIndex = Mathf.Max(0, _currentCreatureIndex - 1);
		}
	}

	/// <summary>
	/// Starts examine mode at the player's position.
	/// </summary>
	public void StartExamine(GridPosition playerPosition)
	{
		_currentMode = TargetingMode.Examine;
		_isActive = true;
		_cursorPosition = playerPosition;
		_originPosition = playerPosition;

		// Clear action mode state
		_rangeTiles = null;
		_validCreatureTargets = null;
		_currentCreatureIndex = -1;
		_targetingDefinition = null;
		_targetingHandler = null;
		_caster = null;
		_actionContext = null;
		AffectedPositions = null;
		IsOnValidTarget = false;

		EmitSignal(SignalName.CursorStarted, (int)_currentMode);
	}

	/// <summary>
	/// Examines the target at the current cursor position.
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

	/// <summary>
	/// Starts action targeting using a TargetingDefinition.
	/// </summary>
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
		_targetingHandler = Targeting.TargetingHandler.CreateForDefinition(definition);
		_initiatingKey = initiatingKey;

		_currentMode = TargetingMode.Action;
		_originPosition = caster.GridPosition;
		_isActive = true;

		// Calculate range tiles for overlay and cursor movement bounds
		int range = definition.Range > 0 ? definition.Range : 1;
		if (definition.RequiresLOS)
		{
			_rangeTiles = FOVCalculator.CalculateVisibleTiles(_originPosition, range, context.MapSystem, definition.Metric);
		}
		else
		{
			_rangeTiles = new HashSet<GridPosition>();
			for (int dx = -range; dx <= range; dx++)
			{
				for (int dy = -range; dy <= range; dy++)
				{
					var checkPos = new GridPosition(_originPosition.X + dx, _originPosition.Y + dy);
					if (DistanceHelper.IsInRange(_originPosition, checkPos, range, definition.Metric) &&
						context.MapSystem.IsInBounds(checkPos))
					{
						_rangeTiles.Add(checkPos);
					}
				}
			}
		}

		// Build creature target list for Tab cycling (creature-targeting filters only)
		_validCreatureTargets = new List<BaseEntity>();
		bool isCreatureTargeting = definition.Filter != TargetFilter.Tile;

		if (isCreatureTargeting)
		{
			var validTargetPositions = _targetingHandler.GetValidTargetPositions(caster, definition, context);
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
		}

		// Sort by distance and select initial target
		bool useGridDistance = definition.Metric == DistanceMetric.Chebyshev;
		if (isCreatureTargeting && _validCreatureTargets.Count > 0)
		{
			_validCreatureTargets = useGridDistance
				? _validCreatureTargets.OrderBy(e => DistanceHelper.ChebyshevDistance(_originPosition, e.GridPosition)).ToList()
				: _validCreatureTargets.OrderBy(e => DistanceHelper.EuclideanDistance(_originPosition, e.GridPosition)).ToList();

			_currentCreatureIndex = 0;
			_cursorPosition = _validCreatureTargets[0].GridPosition;
		}
		else
		{
			_currentCreatureIndex = -1;
			_cursorPosition = _originPosition;
		}

		UpdateAffectedPositions();
		UpdateIsOnValidTarget();

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
			_ => caster.Faction != target.Faction
		};
	}

	/// <summary>
	/// Updates the affected positions for area preview.
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
	/// Updates the IsOnValidTarget property based on current cursor position.
	/// </summary>
	private void UpdateIsOnValidTarget()
	{
		if (_targetingHandler == null || _targetingDefinition == null || _caster == null || _actionContext == null)
		{
			IsOnValidTarget = false;
			return;
		}

		IsOnValidTarget = _targetingHandler.IsValidTarget(_caster, _cursorPosition, _targetingDefinition, _actionContext);
	}

	/// <summary>
	/// Stops targeting mode and clears all state.
	/// </summary>
	public void Stop()
	{
		_isActive = false;
		_rangeTiles = null;
		_validCreatureTargets = null;
		_currentCreatureIndex = -1;
		_targetingDefinition = null;
		_targetingHandler = null;
		_caster = null;
		_actionContext = null;
		_initiatingKey = null;
		AffectedPositions = null;
		IsOnValidTarget = false;
	}

	/// <summary>
	/// Moves the cursor in the specified direction.
	/// Free movement within range - validation happens on confirm.
	/// </summary>
	public bool MoveCursor(Vector2I direction)
	{
		if (!_isActive)
			return false;

		var newPosition = _cursorPosition.Add(direction);

		bool isValid = _currentMode == TargetingMode.Examine
			? _visionSystem.IsVisible(newPosition)
			: _rangeTiles != null && _rangeTiles.Contains(newPosition);

		if (isValid)
		{
			_cursorPosition = newPosition;

			if (_currentMode == TargetingMode.Action)
			{
				UpdateCreatureIndexFromPosition();
				UpdateAffectedPositions();
				UpdateIsOnValidTarget();
			}

			var entity = _entityManager.GetEntityAtPosition(_cursorPosition);
			EmitSignal(SignalName.CursorMoved, entity);

			return true;
		}

		return false;
	}

	/// <summary>
	/// Cycles to the next valid creature target.
	/// </summary>
	public void CycleNextTarget()
	{
		if (!_isActive || _currentMode != TargetingMode.Action)
			return;

		if (_validCreatureTargets == null || _validCreatureTargets.Count == 0)
			return;

		_currentCreatureIndex = (_currentCreatureIndex + 1) % _validCreatureTargets.Count;
		_cursorPosition = _validCreatureTargets[_currentCreatureIndex].GridPosition;

		UpdateAffectedPositions();
		UpdateIsOnValidTarget();

		var entity = _validCreatureTargets[_currentCreatureIndex];
		EmitSignal(SignalName.CursorMoved, entity);
	}

	/// <summary>
	/// Cycles to the previous valid creature target.
	/// </summary>
	public void CyclePreviousTarget()
	{
		if (!_isActive || _currentMode != TargetingMode.Action)
			return;

		if (_validCreatureTargets == null || _validCreatureTargets.Count == 0)
			return;

		_currentCreatureIndex--;
		if (_currentCreatureIndex < 0)
			_currentCreatureIndex = _validCreatureTargets.Count - 1;

		_cursorPosition = _validCreatureTargets[_currentCreatureIndex].GridPosition;

		UpdateAffectedPositions();
		UpdateIsOnValidTarget();

		var entity = _validCreatureTargets[_currentCreatureIndex];
		EmitSignal(SignalName.CursorMoved, entity);
	}

	/// <summary>
	/// Confirms the current target.
	/// </summary>
	public void ConfirmTarget()
	{
		if (!_isActive || _currentMode != TargetingMode.Action)
			return;

		GridPosition targetPosition = _cursorPosition;
		Stop();
		EmitSignal(SignalName.TargetConfirmed, targetPosition.ToVector2I());
	}

	/// <summary>
	/// Cancels targeting.
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
	/// Gets the entity at the cursor position.
	/// </summary>
	public BaseEntity GetEntityAtCursor()
	{
		if (!_isActive)
			return null;

		return _entityManager.GetEntityAtPosition(_cursorPosition);
	}

	/// <summary>
	/// Gets the creature at the cursor position (entity with HealthComponent).
	/// </summary>
	public BaseEntity GetCreatureAtCursor()
	{
		var entity = GetEntityAtCursor();
		if (entity?.GetNodeOrNull<Components.HealthComponent>("HealthComponent") != null)
			return entity;
		return null;
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

		_currentCreatureIndex = -1;
	}

	public override void _ExitTree()
	{
		if (_entityManager != null && GodotObject.IsInstanceValid(_entityManager))
		{
			_entityManager.Disconnect(EntityManager.SignalName.EntityRemoved, Callable.From<BaseEntity>(OnEntityRemoved));
		}
	}
}
