using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Analyzers;

/// <summary>
/// Detects passages - narrow walkable areas connecting regions.
/// </summary>
public static class PassageDetector
{
    private static readonly (int dx, int dy)[] CardinalDirections = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    /// <summary>
    /// Detect all passages in the grid.
    /// Passages are narrow tile clusters that connect 2+ regions.
    /// </summary>
    public static void DetectPassages(
        GenerationContext context,
        TileClassification[,] classifications,
        DistanceField wallDistance)
    {
        var visited = new bool[context.Width, context.Height];
        var passages = new List<Passage>();
        int passageId = 0;

        // Find all passage/chokepoint tiles
        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                if (visited[x, y] || !context.IsWalkable(x, y))
                    continue;

                var classification = classifications[x, y];
                if (classification != TileClassification.Passage &&
                    classification != TileClassification.Chokepoint)
                    continue;

                // Flood fill this passage cluster
                var passageTiles = FloodFillPassage(context, classifications, visited, x, y);

                if (passageTiles.Count == 0)
                    continue;

                // Find which regions this passage connects
                var connectedRegions = FindConnectedRegions(context, passageTiles);

                if (connectedRegions.Count >= 2)
                {
                    // Calculate minimum width
                    int minWidth = CalculateMinWidth(wallDistance, passageTiles);

                    var passage = new Passage
                    {
                        Id = passageId++,
                        RegionAId = connectedRegions[0],
                        RegionBId = connectedRegions.Count > 1 ? connectedRegions[1] : connectedRegions[0],
                        Tiles = passageTiles,
                        MinWidth = minWidth
                    };

                    passages.Add(passage);
                }
            }
        }

        // Update metadata
        context.Metadata.Passages.Clear();
        context.Metadata.Passages.AddRange(passages);
    }

    /// <summary>
    /// Flood fill passage tiles (passage and chokepoint classifications).
    /// </summary>
    private static List<GridPosition> FloodFillPassage(
        GenerationContext context,
        TileClassification[,] classifications,
        bool[,] visited,
        int startX, int startY)
    {
        var tiles = new List<GridPosition>();
        var queue = new Queue<(int x, int y)>();

        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            tiles.Add(new GridPosition(x, y));

            foreach (var (dx, dy) in CardinalDirections)
            {
                int nx = x + dx, ny = y + dy;

                if (!context.IsInBounds(nx, ny) || visited[nx, ny] || !context.IsWalkable(nx, ny))
                    continue;

                var classification = classifications[nx, ny];
                if (classification != TileClassification.Passage &&
                    classification != TileClassification.Chokepoint)
                    continue;

                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return tiles;
    }

    /// <summary>
    /// Find region IDs that a passage connects to.
    /// </summary>
    private static List<int> FindConnectedRegions(GenerationContext context, List<GridPosition> passageTiles)
    {
        var regionIds = context.Metadata.RegionIds;
        if (regionIds == null)
            return new List<int>();

        var connected = new HashSet<int>();

        foreach (var tile in passageTiles)
        {
            foreach (var (dx, dy) in CardinalDirections)
            {
                int nx = tile.X + dx, ny = tile.Y + dy;

                if (!context.IsInBounds(nx, ny))
                    continue;

                int regionId = regionIds[nx, ny];
                if (regionId >= 0)
                    connected.Add(regionId);
            }
        }

        return new List<int>(connected);
    }

    /// <summary>
    /// Calculate the minimum width of a passage.
    /// </summary>
    private static int CalculateMinWidth(DistanceField wallDistance, List<GridPosition> tiles)
    {
        int minDist = int.MaxValue;

        foreach (var tile in tiles)
        {
            int dist = wallDistance.GetDistance(tile);
            if (dist != DistanceField.Unreachable && dist < minDist)
                minDist = dist;
        }

        // Width is approximately 2 * wallDistance (walls on both sides)
        return minDist == int.MaxValue ? 1 : minDist * 2 - 1;
    }
}
