using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Analyzers;

/// <summary>
/// Detects chokepoints - tactical bottleneck positions in passages.
/// </summary>
public static class ChokepointDetector
{
    private static readonly (int dx, int dy)[] CardinalDirections = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    /// <summary>
    /// Detect all chokepoints in the grid.
    /// </summary>
    public static void DetectChokepoints(
        GenerationContext context,
        TileClassification[,] classifications,
        DistanceField wallDistance)
    {
        var chokepoints = new List<Chokepoint>();

        // Find tiles classified as chokepoints
        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                if (classifications[x, y] != TileClassification.Chokepoint)
                    continue;

                var pos = new GridPosition(x, y);
                int width = CalculateWidth(wallDistance, pos);
                var connectedRegions = FindConnectedRegions(context, pos);
                int? passageId = FindContainingPassage(context, pos);
                float strategicValue = CalculateStrategicValue(context, pos, connectedRegions, width);

                chokepoints.Add(new Chokepoint
                {
                    Position = pos,
                    Width = width,
                    ConnectedRegionIds = connectedRegions.ToArray(),
                    StrategicValue = strategicValue,
                    PassageId = passageId
                });
            }
        }

        // Also add narrow passage points (width 1) that aren't already chokepoints
        foreach (var passage in context.Metadata.Passages)
        {
            if (passage.MinWidth <= 1)
            {
                foreach (var tile in passage.Tiles)
                {
                    int dist = wallDistance.GetDistance(tile);
                    if (dist == 1)
                    {
                        // Check if already in chokepoints
                        bool exists = false;
                        foreach (var choke in chokepoints)
                        {
                            if (choke.Position == tile)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            var connectedRegions = new List<int> { passage.RegionAId, passage.RegionBId };
                            chokepoints.Add(new Chokepoint
                            {
                                Position = tile,
                                Width = 1,
                                ConnectedRegionIds = connectedRegions.ToArray(),
                                StrategicValue = 0.8f, // Narrow passage points are strategic
                                PassageId = passage.Id
                            });
                        }
                    }
                }
            }
        }

        // Update metadata
        context.Metadata.Chokepoints.Clear();
        context.Metadata.Chokepoints.AddRange(chokepoints);
    }

    /// <summary>
    /// Calculate the width at a chokepoint position.
    /// </summary>
    private static int CalculateWidth(DistanceField wallDistance, GridPosition pos)
    {
        int dist = wallDistance.GetDistance(pos);
        if (dist == DistanceField.Unreachable)
            return 1;

        // Width is approximately 2 * wallDistance - 1
        return dist * 2 - 1;
    }

    /// <summary>
    /// Find regions connected through this position.
    /// </summary>
    private static List<int> FindConnectedRegions(GenerationContext context, GridPosition pos)
    {
        var regionIds = context.Metadata.RegionIds;
        if (regionIds == null)
            return new List<int>();

        var connected = new HashSet<int>();

        // BFS to find reachable regions
        var visited = new HashSet<GridPosition>();
        var queue = new Queue<GridPosition>();
        queue.Enqueue(pos);
        visited.Add(pos);

        int maxSteps = 20; // Limit search distance
        int steps = 0;

        while (queue.Count > 0 && steps < maxSteps)
        {
            int levelSize = queue.Count;
            for (int i = 0; i < levelSize; i++)
            {
                var current = queue.Dequeue();

                foreach (var (dx, dy) in CardinalDirections)
                {
                    var next = new GridPosition(current.X + dx, current.Y + dy);

                    if (!context.IsInBounds(next) || visited.Contains(next) || !context.IsWalkable(next))
                        continue;

                    visited.Add(next);

                    int regionId = regionIds[next.X, next.Y];
                    if (regionId >= 0)
                    {
                        connected.Add(regionId);
                    }
                    else
                    {
                        queue.Enqueue(next);
                    }
                }
            }
            steps++;
        }

        return new List<int>(connected);
    }

    /// <summary>
    /// Find the passage containing this position (if any).
    /// </summary>
    private static int? FindContainingPassage(GenerationContext context, GridPosition pos)
    {
        foreach (var passage in context.Metadata.Passages)
        {
            foreach (var tile in passage.Tiles)
            {
                if (tile == pos)
                    return passage.Id;
            }
        }
        return null;
    }

    /// <summary>
    /// Calculate strategic value of a chokepoint (0-1).
    /// Higher values = more traffic funneled through.
    /// </summary>
    private static float CalculateStrategicValue(
        GenerationContext context,
        GridPosition pos,
        List<int> connectedRegions,
        int width)
    {
        float value = 0f;

        // Narrower = more strategic
        if (width == 1) value += 0.4f;
        else if (width == 2) value += 0.2f;
        else if (width == 3) value += 0.1f;

        // More regions connected = more strategic
        if (connectedRegions.Count >= 3) value += 0.3f;
        else if (connectedRegions.Count == 2) value += 0.2f;

        // Bonus for connecting large regions
        int totalArea = 0;
        foreach (var regionId in connectedRegions)
        {
            if (regionId >= 0 && regionId < context.Metadata.Regions.Count)
            {
                totalArea += context.Metadata.Regions[regionId].Area;
            }
        }

        if (totalArea > 200) value += 0.2f;
        else if (totalArea > 100) value += 0.1f;

        return System.Math.Min(1f, value);
    }
}
