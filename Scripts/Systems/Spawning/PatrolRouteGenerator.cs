using System.Collections.Generic;
using Godot;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Static service for generating patrol routes within regions.
/// Creates waypoint patterns for patrolling creatures.
/// </summary>
public static class PatrolRouteGenerator
{
    /// <summary>
    /// Generates waypoints distributed across a region.
    /// Strategy: Divide region into quadrants, pick one walkable tile per quadrant,
    /// order by greedy shortest path starting from startPos.
    /// </summary>
    /// <param name="region">The region to patrol.</param>
    /// <param name="startPos">Starting position for route ordering.</param>
    /// <param name="waypointCount">Target number of waypoints (3-5 recommended).</param>
    /// <returns>A Loop patrol route, or null if insufficient waypoints found.</returns>
    public static PatrolRoute GenerateRegionPatrol(
        Region region,
        GridPosition startPos,
        int waypointCount = 4)
    {
        if (region?.Tiles == null || region.Tiles.Count < waypointCount)
            return null;

        var waypoints = SelectDistributedWaypoints(region, waypointCount);
        if (waypoints.Count < 2)
            return null;

        // Order waypoints by greedy nearest neighbor starting from startPos
        var orderedWaypoints = OrderByNearestNeighbor(waypoints, startPos);

        return new PatrolRoute(orderedWaypoints, PatrolRouteType.Loop, waypointTolerance: 1);
    }

    /// <summary>
    /// Generates waypoints around a central point (e.g., for guard posts).
    /// Strategy: Cardinal/diagonal points around center, filtered to valid tiles.
    /// </summary>
    /// <param name="center">Center point to patrol around.</param>
    /// <param name="radius">Distance from center for waypoints.</param>
    /// <param name="validTiles">Set of valid walkable tile positions.</param>
    /// <returns>A PingPong patrol route, or null if insufficient waypoints found.</returns>
    public static PatrolRoute GenerateGuardPostPatrol(
        GridPosition center,
        int radius,
        HashSet<GridPosition> validTiles)
    {
        if (validTiles == null || validTiles.Count == 0)
            return null;

        var waypoints = new List<GridPosition>();

        // Try cardinal directions first
        var cardinalOffsets = new Vector2I[]
        {
            new(0, -radius),  // North
            new(radius, 0),   // East
            new(0, radius),   // South
            new(-radius, 0)   // West
        };

        foreach (var offset in cardinalOffsets)
        {
            var pos = new GridPosition(center.X + offset.X, center.Y + offset.Y);
            if (validTiles.Contains(pos))
            {
                waypoints.Add(pos);
            }
        }

        // If we don't have enough, try diagonal directions
        if (waypoints.Count < 2)
        {
            var diagonalOffsets = new Vector2I[]
            {
                new(radius, -radius),   // NE
                new(radius, radius),    // SE
                new(-radius, radius),   // SW
                new(-radius, -radius)   // NW
            };

            foreach (var offset in diagonalOffsets)
            {
                var pos = new GridPosition(center.X + offset.X, center.Y + offset.Y);
                if (validTiles.Contains(pos))
                {
                    waypoints.Add(pos);
                }
            }
        }

        if (waypoints.Count < 2)
            return null;

        // Order by nearest neighbor starting from center
        var orderedWaypoints = OrderByNearestNeighbor(waypoints, center);

        return new PatrolRoute(orderedWaypoints, PatrolRouteType.PingPong, waypointTolerance: 1);
    }

    /// <summary>
    /// Selects waypoints distributed across the region by dividing into quadrants.
    /// </summary>
    private static List<GridPosition> SelectDistributedWaypoints(Region region, int count)
    {
        var bbox = region.BoundingBox;
        var midX = bbox.Position.X + bbox.Size.X / 2;
        var midY = bbox.Position.Y + bbox.Size.Y / 2;

        // Divide tiles into quadrants
        var quadrants = new List<GridPosition>[4];
        for (int i = 0; i < 4; i++)
            quadrants[i] = new List<GridPosition>();

        foreach (var tile in region.Tiles)
        {
            int quadrant = (tile.X >= midX ? 1 : 0) + (tile.Y >= midY ? 2 : 0);
            quadrants[quadrant].Add(tile);
        }

        var waypoints = new List<GridPosition>();

        // Pick one random tile from each non-empty quadrant
        foreach (var quadrant in quadrants)
        {
            if (quadrant.Count > 0)
            {
                var tile = quadrant[GD.RandRange(0, quadrant.Count - 1)];
                waypoints.Add(tile);
            }
        }

        // If we need more waypoints, add random tiles from the region
        while (waypoints.Count < count && waypoints.Count < region.Tiles.Count)
        {
            var tile = region.Tiles[GD.RandRange(0, region.Tiles.Count - 1)];
            if (!waypoints.Contains(tile))
            {
                waypoints.Add(tile);
            }
        }

        return waypoints;
    }

    /// <summary>
    /// Orders waypoints using greedy nearest neighbor algorithm.
    /// Creates a relatively short path through all waypoints.
    /// </summary>
    private static List<GridPosition> OrderByNearestNeighbor(List<GridPosition> waypoints, GridPosition start)
    {
        if (waypoints.Count <= 1)
            return new List<GridPosition>(waypoints);

        var remaining = new List<GridPosition>(waypoints);
        var ordered = new List<GridPosition>();
        var current = start;

        while (remaining.Count > 0)
        {
            // Find nearest remaining waypoint
            int nearestIndex = 0;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < remaining.Count; i++)
            {
                int distance = DistanceHelper.ChebyshevDistance(current, remaining[i]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            current = remaining[nearestIndex];
            ordered.Add(current);
            remaining.RemoveAt(nearestIndex);
        }

        return ordered;
    }
}
