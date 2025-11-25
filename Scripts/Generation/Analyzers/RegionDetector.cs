using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Analyzers;

/// <summary>
/// Detects contiguous regions using flood fill.
/// Regions are walkable areas >= MinRegionSize. Smaller areas become Alcoves.
/// </summary>
public static class RegionDetector
{
    private static readonly (int dx, int dy)[] CardinalDirections = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    /// <summary>
    /// Detect all regions and alcoves in the grid.
    /// Updates context.Metadata with detected regions.
    /// </summary>
    /// <param name="context">Generation context with grid.</param>
    /// <param name="classifications">Pre-computed tile classifications.</param>
    /// <param name="minRegionSize">Minimum tiles for a Region (smaller = Alcove).</param>
    /// <param name="preserveExisting">If true, preserve regions already in metadata (from BSP).</param>
    public static void DetectRegions(
        GenerationContext context,
        TileClassification[,] classifications,
        int minRegionSize = 16,
        bool preserveExisting = true)
    {
        var visited = new bool[context.Width, context.Height];
        var regionIds = new int[context.Width, context.Height];

        // Initialize region IDs to -1
        for (int x = 0; x < context.Width; x++)
            for (int y = 0; y < context.Height; y++)
                regionIds[x, y] = -1;

        // If preserving existing regions, mark their tiles as visited
        if (preserveExisting && context.Metadata.Regions.Count > 0)
        {
            foreach (var region in context.Metadata.Regions)
            {
                foreach (var tile in region.Tiles)
                {
                    if (context.IsInBounds(tile))
                    {
                        visited[tile.X, tile.Y] = true;
                        regionIds[tile.X, tile.Y] = region.Id;
                    }
                }
            }
        }
        else
        {
            // Clear existing regions if not preserving
            context.Metadata.Regions.Clear();
            context.Metadata.Alcoves.Clear();
        }

        int nextRegionId = context.Metadata.Regions.Count;

        // Flood fill from unvisited floor tiles
        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                if (visited[x, y] || !context.IsWalkable(x, y))
                    continue;

                // Skip passage/chokepoint tiles for region seeding
                var classification = classifications[x, y];
                if (classification == TileClassification.Passage ||
                    classification == TileClassification.Chokepoint)
                    continue;

                // Flood fill this area
                var area = FloodFill(context, classifications, visited, x, y);

                if (area.Count == 0)
                    continue;

                // Create region or alcove
                var region = CreateRegion(area, nextRegionId);

                if (area.Count >= minRegionSize)
                {
                    region.Source = RegionSource.Detected;
                    context.Metadata.Regions.Add(region);

                    // Update region IDs
                    foreach (var tile in area)
                        regionIds[tile.X, tile.Y] = nextRegionId;

                    nextRegionId++;
                }
                else
                {
                    region.Source = RegionSource.Detected;
                    context.Metadata.Alcoves.Add(region);
                    // Alcoves keep regionId = -1
                }
            }
        }

        context.Metadata.RegionIds = regionIds;
    }

    /// <summary>
    /// Flood fill from a starting position, stopping at passages/chokepoints.
    /// </summary>
    private static List<GridPosition> FloodFill(
        GenerationContext context,
        TileClassification[,] classifications,
        bool[,] visited,
        int startX, int startY)
    {
        var area = new List<GridPosition>();
        var queue = new Queue<(int x, int y)>();

        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            area.Add(new GridPosition(x, y));

            foreach (var (dx, dy) in CardinalDirections)
            {
                int nx = x + dx, ny = y + dy;

                if (!context.IsInBounds(nx, ny) || visited[nx, ny] || !context.IsWalkable(nx, ny))
                    continue;

                // Stop at passages/chokepoints (they separate regions)
                var classification = classifications[nx, ny];
                if (classification == TileClassification.Passage ||
                    classification == TileClassification.Chokepoint)
                {
                    visited[nx, ny] = true; // Mark as visited but don't add to area
                    continue;
                }

                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return area;
    }

    /// <summary>
    /// Create a Region from a list of tiles.
    /// </summary>
    private static Region CreateRegion(List<GridPosition> tiles, int id)
    {
        // Calculate bounding box and centroid
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        int sumX = 0, sumY = 0;

        foreach (var tile in tiles)
        {
            if (tile.X < minX) minX = tile.X;
            if (tile.Y < minY) minY = tile.Y;
            if (tile.X > maxX) maxX = tile.X;
            if (tile.Y > maxY) maxY = tile.Y;
            sumX += tile.X;
            sumY += tile.Y;
        }

        var boundingBox = new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
        var centroid = new GridPosition(sumX / tiles.Count, sumY / tiles.Count);

        // Identify edge tiles (simplified - tiles on bounding box edges)
        var edgeTiles = new List<GridPosition>();
        foreach (var tile in tiles)
        {
            if (tile.X == minX || tile.X == maxX || tile.Y == minY || tile.Y == maxY)
                edgeTiles.Add(tile);
        }

        return new Region
        {
            Id = id,
            Tiles = tiles,
            EdgeTiles = edgeTiles,
            BoundingBox = boundingBox,
            Centroid = centroid
        };
    }
}
