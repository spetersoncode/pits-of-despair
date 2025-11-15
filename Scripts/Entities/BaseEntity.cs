using Godot;
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
    /// ASCII character representing this entity.
    /// </summary>
    public char Glyph { get; set; } = '?';

    /// <summary>
    /// Color to render the glyph.
    /// </summary>
    public Color GlyphColor { get; set; } = Colors.White;

    /// <summary>
    /// Updates the entity's grid position and emits PositionChanged signal.
    /// </summary>
    /// <param name="newPosition">The new grid position.</param>
    public void SetGridPosition(GridPosition newPosition)
    {
        GridPosition = newPosition;
        EmitSignal(SignalName.PositionChanged, newPosition.X, newPosition.Y);
    }
}
