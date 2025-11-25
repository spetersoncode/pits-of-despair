using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Static service for generating patrol routes based on scope and configuration.
/// Creates waypoint patterns for patrolling creatures.
/// </summary>
public static class PatrolRouteGenerator
{
    /// <summary>
    /// Generates a patrol route based on scope configuration.
    /// </summary>
    /// <param name="scope">How far the patrol extends (Local, Extended, Roaming).</param>
    /// <param name="startPos">Starting position for route.</param>
    /// <param name="region">Spawn region (used for Local/Extended scope).</param>
    /// <param name="mapSystem">Map system for walkability checks (used for Extended/Roaming).</param>
    /// <param name="waypointCount">Number of waypoints to generate.</param>
    /// <param name="minDistance">Minimum distance between waypoints.</param>
    /// <param name="maxDistance">Maximum distance from start (Roaming only).</param>
    /// <returns>A patrol route, or null if generation fails.</returns>
    public static PatrolRoute GeneratePatrol(
        PatrolScope scope,
        GridPosition startPos,
        Region region,
        MapSystem mapSystem,
        int waypointCount = 4,
        int minDistance = 8,
        int maxDistance = 40)
    {
        return scope switch
        {
            PatrolScope.Local => GenerateLocalPatrol(region, startPos, waypointCount),
            PatrolScope.Extended => GenerateExtendedPatrol(region, startPos, mapSystem, waypointCount, minDistance),
            PatrolScope.Roaming => GenerateRoamingPatrol(startPos, mapSystem, waypointCount, minDistance, maxDistance),
            _ => GenerateLocalPatrol(region, startPos, waypointCount)
        };
    }

    /// <summary>
    /// Generates waypoints distributed across a region (Local scope).
    /// Strategy: Divide region into quadrants, pick one walkable tile per quadrant,
    /// order by greedy shortest path starting from startPos.
    /// </summary>
    public static PatrolRoute GenerateLocalPatrol(
        Region region,
        GridPosition startPos,
        int waypointCount = 4)
    {
        if (region?.Tiles == null || region.Tiles.Count < waypointCount)
            return null;

        var waypoints = SelectDistributedWaypoints(region.Tiles, region.BoundingBox, waypointCount);
        if (waypoints.Count < 2)
            return null;

        var orderedWaypoints = OrderByNearestNeighbor(waypoints, startPos);
        return new PatrolRoute(orderedWaypoints, PatrolRouteType.Loop, waypointTolerance: 1);
    }

    /// <summary>
    /// Generates waypoints extending beyond spawn region into adjacent areas (Extended scope).
    /// Includes tiles within minDistance of the region boundary.
    /// </summary>
    public static PatrolRoute GenerateExtendedPatrol(
        Region region,
        GridPosition startPos,
        MapSystem mapSystem,
        int waypointCount = 4,
        int minDistance = 8)
    {
        if (region?.Tiles == null || mapSystem == null)
            return null;

        // Collect tiles: region tiles + nearby walkable tiles
        var extendedTiles = new HashSet<GridPosition>(region.Tiles);
        var bbox = region.BoundingBox;
        int expansion = minDistance;

        // Expand search area beyond region bounds
        for (int x = bbox.Position.X - expansion; x < bbox.Position.X + bbox.Size.X + expansion; x++)
        {
            for (int y = bbox.Position.Y - expansion; y < bbox.Position.Y + bbox.Size.Y + expansion; y++)
            {
                var pos = new GridPosition(x, y);
                if (mapSystem.IsWalkable(pos))
                {
                    extendedTiles.Add(pos);
                }
            }
        }

        if (extendedTiles.Count < waypointCount)
            return null;

        // Create bounding box for extended area
        var allTiles = extendedTiles.ToList();
        var extendedBbox = CalculateBoundingBox(allTiles);

        var waypoints = SelectDistributedWaypoints(allTiles, extendedBbox, waypointCount);
        if (waypoints.Count < 2)
            return null;

        var orderedWaypoints = OrderByNearestNeighbor(waypoints, startPos);
        return new PatrolRoute(orderedWaypoints, PatrolRouteType.Loop, waypointTolerance: 1);
    }

    /// <summary>
    /// Generates waypoints across the map (Roaming scope).
    /// Picks random distant walkable tiles within maxDistance.
    /// </summary>
    public static PatrolRoute GenerateRoamingPatrol(
        GridPosition startPos,
        MapSystem mapSystem,
        int waypointCount = 4,
        int minDistance = 8,
        int maxDistance = 40)
    {
        if (mapSystem == null)
            return null;

        var waypoints = new List<GridPosition>();
        int mapWidth = mapSystem.MapWidth;
        int mapHeight = mapSystem.MapHeight;

        const int maxAttempts = 100;
        int attempts = 0;

        while (waypoints.Count < waypointCount && attempts < maxAttempts)
        {
            attempts++;

            int x = GD.RandRange(1, mapWidth - 2);
            int y = GD.RandRange(1, mapHeight - 2);
            var pos = new GridPosition(x, y);

            if (!mapSystem.IsWalkable(pos))
                continue;

            int distance = DistanceHelper.ChebyshevDistance(startPos, pos);
            if (distance < minDistance || distance > maxDistance)
                continue;

            // Ensure waypoints are spread out from each other
            bool tooClose = waypoints.Any(wp =>
                DistanceHelper.ChebyshevDistance(wp, pos) < minDistance / 2);
            if (tooClose)
                continue;

            waypoints.Add(pos);
        }

        if (waypoints.Count < 2)
            return null;

        var orderedWaypoints = OrderByNearestNeighbor(waypoints, startPos);
        return new PatrolRoute(orderedWaypoints, PatrolRouteType.Loop, waypointTolerance: 1);
    }

    /// <summary>
    /// Legacy method - generates waypoints distributed across a region.
    /// Preserved for backwards compatibility.
    /// </summary>
    public static PatrolRoute GenerateRegionPatrol(
        Region region,
        GridPosition startPos,
        int waypointCount = 4)
    {
        return GenerateLocalPatrol(region, startPos, waypointCount);
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
    /// Selects waypoints distributed across tiles by dividing into quadrants.
    /// </summary>
    private static List<GridPosition> SelectDistributedWaypoints(
        IList<GridPosition> tiles,
        Rect2I bbox,
        int count)
    {
        var midX = bbox.Position.X + bbox.Size.X / 2;
        var midY = bbox.Position.Y + bbox.Size.Y / 2;

        // Divide tiles into quadrants
        var quadrants = new List<GridPosition>[4];
        for (int i = 0; i < 4; i++)
            quadrants[i] = new List<GridPosition>();

        foreach (var tile in tiles)
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

        // If we need more waypoints, add random tiles
        while (waypoints.Count < count && waypoints.Count < tiles.Count)
        {
            var tile = tiles[GD.RandRange(0, tiles.Count - 1)];
            if (!waypoints.Contains(tile))
            {
                waypoints.Add(tile);
            }
        }

        return waypoints;
    }

    /// <summary>
    /// Calculates bounding box for a set of tiles.
    /// </summary>
    private static Rect2I CalculateBoundingBox(IList<GridPosition> tiles)
    {
        if (tiles.Count == 0)
            return new Rect2I(0, 0, 0, 0);

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var tile in tiles)
        {
            if (tile.X < minX) minX = tile.X;
            if (tile.Y < minY) minY = tile.Y;
            if (tile.X > maxX) maxX = tile.X;
            if (tile.Y > maxY) maxY = tile.Y;
        }

        return new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
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
