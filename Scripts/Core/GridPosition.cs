using Godot;

namespace PitsOfDespair.Core;

/// <summary>
/// Represents a position on the game's tile grid.
/// </summary>
public struct GridPosition
{
    public int X { get; set; }
    public int Y { get; set; }

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Converts this grid position to world coordinates.
    /// </summary>
    /// <param name="tileSize">Size of each tile in pixels.</param>
    /// <returns>World position in pixels.</returns>
    public Vector2 ToWorld(int tileSize)
    {
        return new Vector2(X * tileSize, Y * tileSize);
    }

    /// <summary>
    /// Creates a GridPosition from a Vector2I.
    /// </summary>
    public static GridPosition FromVector2I(Vector2I vector)
    {
        return new GridPosition(vector.X, vector.Y);
    }

    /// <summary>
    /// Converts this GridPosition to a Vector2I.
    /// </summary>
    public Vector2I ToVector2I()
    {
        return new Vector2I(X, Y);
    }

    /// <summary>
    /// Returns the Manhattan distance between two grid positions.
    /// </summary>
    public int ManhattanDistance(GridPosition other)
    {
        return Mathf.Abs(X - other.X) + Mathf.Abs(Y - other.Y);
    }

    /// <summary>
    /// Adds a direction vector to this position.
    /// </summary>
    public GridPosition Add(Vector2I direction)
    {
        return new GridPosition(X + direction.X, Y + direction.Y);
    }

    public override bool Equals(object obj)
    {
        return obj is GridPosition position &&
               X == position.X &&
               Y == position.Y;
    }

    public override int GetHashCode()
    {
        return (X * 397) ^ Y;
    }

    public static bool operator ==(GridPosition left, GridPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GridPosition left, GridPosition right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
