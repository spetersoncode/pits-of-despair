using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;

namespace PitsOfDespair.Entities;

/// <summary>
/// Base class for all entities in the game (player, monsters, NPCs, etc.).
/// Entities are composed of child node components for behavior.
/// </summary>
public partial class BaseEntity : Node2D
{
    /// <summary>
    /// Emitted when the entity's grid position changes.
    /// Parameters: x (int), y (int)
    /// </summary>
    [Signal]
    public delegate void PositionChangedEventHandler(int x, int y);

    /// <summary>
    /// Current position on the game grid.
    /// </summary>
    public GridPosition GridPosition { get; set; }

    /// <summary>
    /// Display name of this entity (e.g., "Rat", "Goblin", "Player").
    /// </summary>
    public string DisplayName { get; set; } = "Unknown";

    /// <summary>
    /// ASCII character representing this entity.
    /// </summary>
    public char Glyph { get; set; } = '?';

    /// <summary>
    /// Color to render the glyph.
    /// </summary>
    public Color GlyphColor { get; set; } = Colors.White;

    /// <summary>
    /// Whether other entities can move through this entity.
    /// True for items, false for creatures.
    /// </summary>
    public bool Passable { get; set; } = false;

    /// <summary>
    /// Updates the entity's grid position and emits PositionChanged signal.
    /// </summary>
    /// <param name="newPosition">The new grid position.</param>
    public void SetGridPosition(GridPosition newPosition)
    {
        GridPosition = newPosition;
        EmitSignal(SignalName.PositionChanged, newPosition.X, newPosition.Y);
    }

    /// <summary>
    /// Execute an action using the action system.
    /// This is the unified entry point for all turn-consuming actions.
    /// Can be overridden by subclasses to add additional behavior (e.g., Player emits TurnCompleted).
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">The action context containing game systems and state.</param>
    /// <returns>The result of the action execution.</returns>
    public virtual ActionResult ExecuteAction(Action action, ActionContext context)
    {
        return action.Execute(this, context);
    }
}
