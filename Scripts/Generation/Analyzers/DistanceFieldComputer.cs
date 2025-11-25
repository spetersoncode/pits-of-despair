using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Analyzers;

/// <summary>
/// Computes distance fields using BFS (Breadth-First Search).
/// Distance fields track the distance from each tile to source positions.
/// </summary>
public static class DistanceFieldComputer
{
    private static readonly (int dx, int dy)[] CardinalDirections = { (0, -1), (1, 0), (0, 1), (-1, 0) };
    private static readonly (int dx, int dy)[] AllDirections = {
        (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)
    };

    /// <summary>
    /// Compute distance from all floor tiles to the nearest wall.
    /// </summary>
    public static DistanceField ComputeWallDistance(GenerationContext context)
    {
        var field = new DistanceField("walls", context.Width, context.Height);
        var queue = new Queue<(int x, int y, int dist)>();

        // Seed queue with all wall tiles adjacent to floor
        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                if (context.GetTile(x, y) == TileType.Wall)
                {
                    // Check if adjacent to any floor tile
                    bool adjacentToFloor = false;
                    foreach (var (dx, dy) in CardinalDirections)
                    {
                        if (context.IsWalkable(x + dx, y + dy))
                        {
                            adjacentToFloor = true;
                            break;
                        }
                    }

                    if (adjacentToFloor)
                    {
                        // Set wall tiles adjacent to floor as distance 0 sources
                        field.SetDistance(new GridPosition(x, y), 0);
                    }
                }
            }
        }

        // Now BFS from floor tiles adjacent to walls (distance 1)
        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                if (context.IsWalkable(x, y))
                {
                    // Check if adjacent to wall
                    foreach (var (dx, dy) in CardinalDirections)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (context.GetTile(nx, ny) == TileType.Wall)
                        {
                            field.SetDistance(new GridPosition(x, y), 1);
                            queue.Enqueue((x, y, 1));
                            break;
                        }
                    }
                }
            }
        }

        // BFS expansion
        while (queue.Count > 0)
        {
            var (cx, cy, dist) = queue.Dequeue();

            foreach (var (dx, dy) in CardinalDirections)
            {
                int nx = cx + dx, ny = cy + dy;
                var pos = new GridPosition(nx, ny);

                if (context.IsWalkable(nx, ny) && field.GetDistance(pos) == DistanceField.Unreachable)
                {
                    field.SetDistance(pos, dist + 1);
                    queue.Enqueue((nx, ny, dist + 1));
                }
            }
        }

        return field;
    }

    /// <summary>
    /// Compute distance from all floor tiles to a single source position.
    /// </summary>
    public static DistanceField ComputeFromPosition(GenerationContext context, GridPosition source, string name)
    {
        var field = new DistanceField(name, context.Width, context.Height);

        if (!context.IsWalkable(source))
            return field;

        var queue = new Queue<(int x, int y, int dist)>();
        field.SetDistance(source, 0);
        queue.Enqueue((source.X, source.Y, 0));

        while (queue.Count > 0)
        {
            var (cx, cy, dist) = queue.Dequeue();

            foreach (var (dx, dy) in CardinalDirections)
            {
                int nx = cx + dx, ny = cy + dy;
                var pos = new GridPosition(nx, ny);

                if (context.IsWalkable(nx, ny) && field.GetDistance(pos) == DistanceField.Unreachable)
                {
                    field.SetDistance(pos, dist + 1);
                    queue.Enqueue((nx, ny, dist + 1));
                }
            }
        }

        return field;
    }

    /// <summary>
    /// Compute distance from all floor tiles to multiple source positions.
    /// </summary>
    public static DistanceField ComputeFromPositions(GenerationContext context, IEnumerable<GridPosition> sources, string name)
    {
        var field = new DistanceField(name, context.Width, context.Height);
        var queue = new Queue<(int x, int y, int dist)>();

        // Seed all sources
        foreach (var source in sources)
        {
            if (context.IsWalkable(source))
            {
                field.SetDistance(source, 0);
                queue.Enqueue((source.X, source.Y, 0));
            }
        }

        // BFS expansion
        while (queue.Count > 0)
        {
            var (cx, cy, dist) = queue.Dequeue();

            foreach (var (dx, dy) in CardinalDirections)
            {
                int nx = cx + dx, ny = cy + dy;
                var pos = new GridPosition(nx, ny);

                if (context.IsWalkable(nx, ny) && field.GetDistance(pos) == DistanceField.Unreachable)
                {
                    field.SetDistance(pos, dist + 1);
                    queue.Enqueue((nx, ny, dist + 1));
                }
            }
        }

        return field;
    }
}
