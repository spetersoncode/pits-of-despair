using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using System.Globalization;

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
    /// Atmospheric description of this entity.
    /// Used for examine command and entity details.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    private string _glyph = "?";

    /// <summary>
    /// Character or symbol representing this entity (supports Unicode).
    /// Must be a single character (grapheme cluster).
    /// </summary>
    public string Glyph
    {
        get => _glyph;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                GD.PushWarning($"BaseEntity: Attempted to set empty glyph for '{DisplayName}', using '?'");
                _glyph = "?";
                return;
            }

            // Use StringInfo to count grapheme clusters (user-perceived characters)
            var textElementEnumerator = System.Globalization.StringInfo.GetTextElementEnumerator(value);
            int count = 0;
            while (textElementEnumerator.MoveNext())
                count++;

            if (count != 1)
            {
                GD.PushWarning($"BaseEntity: Glyph for '{DisplayName}' must be a single character, got '{value}' ({count} characters), using first character");
                textElementEnumerator.Reset();
                textElementEnumerator.MoveNext();
                _glyph = textElementEnumerator.GetTextElement();
            }
            else
            {
                _glyph = value;
            }
        }
    }

    /// <summary>
    /// Color to render the glyph.
    /// </summary>
    public Color GlyphColor { get; set; } = Palette.Default;

    /// <summary>
    /// Whether other entities can move through this entity.
    /// True for items, false for creatures.
    /// </summary>
    public bool IsWalkable { get; set; } = false;

    /// <summary>
    /// Item data if this entity is a collectible item.
    /// Null for non-item entities (creatures, player, etc.).
    /// </summary>
    public ItemInstance? ItemData { get; set; } = null;

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
