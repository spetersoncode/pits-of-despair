using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Per-tile distance values computed via BFS from source positions.
/// Used for spatial queries like "tiles far from entrance" or "tiles near walls".
/// </summary>
public class DistanceField
{
    /// <summary>
    /// Identifier for this distance field (e.g., "walls", "entrance", "exit").
    /// </summary>
    public string Name { get; init; }

    private readonly int[,] _distances;
    private readonly int _width;
    private readonly int _height;

    /// <summary>
    /// Value indicating an unreachable tile.
    /// </summary>
    public const int Unreachable = int.MaxValue;

    public DistanceField(string name, int width, int height)
    {
        Name = name;
        _width = width;
        _height = height;
        _distances = new int[width, height];

        // Initialize all distances to unreachable
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _distances[x, y] = Unreachable;
    }

    /// <summary>
    /// Set the distance value at a position.
    /// </summary>
    public void SetDistance(GridPosition pos, int distance)
    {
        if (IsInBounds(pos))
            _distances[pos.X, pos.Y] = distance;
    }

    /// <summary>
    /// Get the distance value at a position.
    /// </summary>
    public int GetDistance(GridPosition pos)
    {
        if (!IsInBounds(pos)) return Unreachable;
        return _distances[pos.X, pos.Y];
    }

    /// <summary>
    /// Get all tiles at exactly the specified distance.
    /// </summary>
    public IEnumerable<GridPosition> GetTilesAtDistance(int distance)
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_distances[x, y] == distance)
                    yield return new GridPosition(x, y);
            }
        }
    }

    /// <summary>
    /// Get all tiles within the specified maximum distance (inclusive).
    /// </summary>
    public IEnumerable<GridPosition> GetTilesWithinDistance(int maxDistance)
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_distances[x, y] <= maxDistance)
                    yield return new GridPosition(x, y);
            }
        }
    }

    /// <summary>
    /// Get the maximum distance in the field (excluding unreachable).
    /// </summary>
    public int GetMaxDistance()
    {
        int max = 0;
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_distances[x, y] != Unreachable && _distances[x, y] > max)
                    max = _distances[x, y];
            }
        }
        return max;
    }

    private bool IsInBounds(GridPosition pos)
    {
        return pos.X >= 0 && pos.X < _width && pos.Y >= 0 && pos.Y < _height;
    }
}
