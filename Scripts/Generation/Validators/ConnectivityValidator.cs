using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Validators;

/// <summary>
/// Validates that all walkable areas in the dungeon are connected.
/// Uses flood fill to identify disconnected islands.
/// </summary>
public static class ConnectivityValidator
{
    private static readonly (int dx, int dy)[] CardinalDirections = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    /// <summary>
    /// Result of connectivity validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsFullyConnected { get; init; }
        public List<List<GridPosition>> Islands { get; init; } = new();
        public int LargestIslandIndex { get; init; }
        public int TotalWalkableTiles { get; init; }
    }

    /// <summary>
    /// Validate connectivity of all walkable tiles.
    /// </summary>
    public static ValidationResult Validate(GenerationContext context)
    {
        var visited = new bool[context.Width, context.Height];
        var islands = new List<List<GridPosition>>();
        int largestIndex = 0;
        int largestSize = 0;
        int totalWalkable = 0;

        // Find all islands via flood fill
        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                if (visited[x, y] || !context.IsWalkable(x, y))
                    continue;

                var island = FloodFill(context, visited, x, y);
                totalWalkable += island.Count;

                if (island.Count > largestSize)
                {
                    largestSize = island.Count;
                    largestIndex = islands.Count;
                }

                islands.Add(island);
            }
        }

        return new ValidationResult
        {
            IsFullyConnected = islands.Count <= 1,
            Islands = islands,
            LargestIslandIndex = largestIndex,
            TotalWalkableTiles = totalWalkable
        };
    }

    /// <summary>
    /// Flood fill from a starting position.
    /// </summary>
    private static List<GridPosition> FloodFill(GenerationContext context, bool[,] visited, int startX, int startY)
    {
        var island = new List<GridPosition>();
        var queue = new Queue<(int x, int y)>();

        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            island.Add(new GridPosition(x, y));

            foreach (var (dx, dy) in CardinalDirections)
            {
                int nx = x + dx, ny = y + dy;

                if (!context.IsInBounds(nx, ny) || visited[nx, ny] || !context.IsWalkable(nx, ny))
                    continue;

                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return island;
    }

    /// <summary>
    /// Find the closest pair of tiles between two islands.
    /// </summary>
    public static (GridPosition from, GridPosition to, int distance) FindClosestPair(
        List<GridPosition> islandA,
        List<GridPosition> islandB)
    {
        GridPosition bestFrom = default;
        GridPosition bestTo = default;
        int bestDist = int.MaxValue;

        // Sample tiles for large islands to avoid O(n*m) complexity
        var sampleA = SampleIsland(islandA, 50);
        var sampleB = SampleIsland(islandB, 50);

        foreach (var a in sampleA)
        {
            foreach (var b in sampleB)
            {
                int dist = ManhattanDistance(a, b);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestFrom = a;
                    bestTo = b;
                }
            }
        }

        return (bestFrom, bestTo, bestDist);
    }

    private static List<GridPosition> SampleIsland(List<GridPosition> island, int maxSamples)
    {
        if (island.Count <= maxSamples)
            return island;

        var sampled = new List<GridPosition>();
        int step = island.Count / maxSamples;

        for (int i = 0; i < island.Count && sampled.Count < maxSamples; i += step)
        {
            sampled.Add(island[i]);
        }

        return sampled;
    }

    private static int ManhattanDistance(GridPosition a, GridPosition b)
    {
        return System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
    }
}
