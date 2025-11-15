using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Entities;

/// <summary>
/// The player character.
/// </summary>
public partial class Player : Node2D
{
    [Export] public string Glyph { get; set; } = "@";
    [Export] public Color GlyphColor { get; set; } = Colors.Yellow;

    [Signal]
    public delegate void MovedEventHandler(int x, int y);

    [Signal]
    public delegate void TurnCompletedEventHandler();

    public GridPosition CurrentPosition { get; private set; }

    private MapSystem _mapSystem;

    /// <summary>
    /// Sets the map system reference for collision checking.
    /// </summary>
    public void SetMapSystem(MapSystem mapSystem)
    {
        _mapSystem = mapSystem;

        // Start at center of map
        CurrentPosition = new GridPosition(
            _mapSystem.MapWidth / 2,
            _mapSystem.MapHeight / 2
        );
    }

    /// <summary>
    /// Attempts to move the player in the specified direction.
    /// </summary>
    /// <param name="direction">Direction to move (use Vector2I for grid directions).</param>
    /// <returns>True if movement was successful, false if blocked.</returns>
    public bool TryMove(Vector2I direction)
    {
        if (_mapSystem == null)
        {
            GD.PrintErr("Player: MapSystem not set!");
            return false;
        }

        GridPosition targetPosition = CurrentPosition.Add(direction);

        // Check if target position is walkable
        if (!_mapSystem.IsWalkable(targetPosition))
        {
            return false;
        }

        // Move to new position
        CurrentPosition = targetPosition;
        EmitSignal(SignalName.Moved, CurrentPosition.X, CurrentPosition.Y);
        EmitSignal(SignalName.TurnCompleted);

        return true;
    }

    /// <summary>
    /// Gets the world position for rendering (based on tile size).
    /// </summary>
    public Vector2 GetWorldPosition(int tileSize)
    {
        return CurrentPosition.ToWorld(tileSize);
    }
}
