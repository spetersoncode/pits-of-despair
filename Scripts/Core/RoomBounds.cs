namespace PitsOfDespair.Core;

/// <summary>
/// Represents the rectangular bounds of a room in the dungeon.
/// Used for entity spawning and room-based mechanics.
/// </summary>
public struct RoomBounds
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public RoomBounds(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Get the center position of the room.
    /// </summary>
    public GridPosition GetCenter()
    {
        return new GridPosition(X + Width / 2, Y + Height / 2);
    }

    /// <summary>
    /// Check if a position is within this room's bounds.
    /// </summary>
    public bool Contains(GridPosition position)
    {
        return position.X >= X && position.X < X + Width &&
               position.Y >= Y && position.Y < Y + Height;
    }
}
