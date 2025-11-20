using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages examine mode cursor for inspecting entities on visible tiles.
/// Displays entity descriptions in the message log as the cursor moves.
/// </summary>
public partial class ExamineSystem : Node
{
	[Signal]
	public delegate void ExamineStartedEventHandler();

	[Signal]
	public delegate void ExamineUpdatedEventHandler(BaseEntity entity);

	[Signal]
	public delegate void ExamineCanceledEventHandler();

	private GridPosition _cursorPosition;
	private bool _isActive = false;

	private PlayerVisionSystem _visionSystem;
	private EntityManager _entityManager;

	/// <summary>
	/// Gets whether examine mode is currently active.
	/// </summary>
	public bool IsActive => _isActive;

	/// <summary>
	/// Gets the current cursor position.
	/// </summary>
	public GridPosition CursorPosition => _cursorPosition;

	/// <summary>
	/// Initializes the examine system with required dependencies.
	/// </summary>
	public void Initialize(PlayerVisionSystem visionSystem, EntityManager entityManager)
	{
		_visionSystem = visionSystem;
		_entityManager = entityManager;
	}

	/// <summary>
	/// Starts examine mode at the player's position.
	/// </summary>
	/// <param name="playerPosition">The player's current position</param>
	public void StartExamine(GridPosition playerPosition)
	{
		_isActive = true;
		_cursorPosition = playerPosition;

		// Emit started signal for status message
		EmitSignal(SignalName.ExamineStarted);

		// Emit initial examine update for player position
		var entity = _entityManager.GetEntityAtPosition(_cursorPosition);
		EmitSignal(SignalName.ExamineUpdated, entity);
	}

	/// <summary>
	/// Stops examine mode.
	/// </summary>
	public void StopExamine()
	{
		_isActive = false;
	}

	/// <summary>
	/// Moves the cursor in the specified direction.
	/// Emits ExamineUpdated signal if movement succeeds.
	/// </summary>
	/// <param name="direction">Direction vector to move</param>
	/// <returns>True if the cursor moved, false if blocked</returns>
	public bool MoveCursor(Vector2I direction)
	{
		if (!_isActive)
			return false;

		var newPosition = _cursorPosition.Add(direction);

		// Check if new position is visible
		if (_visionSystem.IsVisible(newPosition))
		{
			_cursorPosition = newPosition;

			// Emit signal with entity at new position (null if none)
			var entity = _entityManager.GetEntityAtPosition(_cursorPosition);
			EmitSignal(SignalName.ExamineUpdated, entity);

			return true;
		}

		return false;
	}

	/// <summary>
	/// Cancels examine mode and emits the ExamineCanceled signal.
	/// </summary>
	public void CancelExamine()
	{
		if (!_isActive)
			return;

		StopExamine();
		EmitSignal(SignalName.ExamineCanceled);
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
}
