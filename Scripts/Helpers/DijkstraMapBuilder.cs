using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Builds Dijkstra distance maps for multi-target pathfinding scenarios.
/// Useful for finding nearest of multiple goals, flee behavior, and influence maps.
/// </summary>
public static class DijkstraMapBuilder
{
    private static readonly GridPosition[] Directions =
    {
        new GridPosition(-1, 0),  // Left
        new GridPosition(1, 0),   // Right
        new GridPosition(0, -1),  // Up
        new GridPosition(0, 1),   // Down
        new GridPosition(-1, -1), // Up-Left
        new GridPosition(1, -1),  // Up-Right
        new GridPosition(-1, 1),  // Down-Left
        new GridPosition(1, 1)    // Down-Right
    };

    /// <summary>
    /// Builds a distance map from multiple goal positions.
    /// Each cell contains the distance to the nearest goal.
    /// Unreachable cells contain float.MaxValue.
    /// </summary>
    /// <param name="goals">List of goal positions (targets to find distance to)</param>
    /// <param name="mapSystem">Map system for walkability checks</param>
    /// <param name="entityManager">Optional entity manager for checking occupancy</param>
    /// <param name="player">Optional player reference for checking player occupancy</param>
    /// <returns>2D array where each cell contains distance to nearest goal</returns>
    public static float[,] BuildDistanceMap(
        List<GridPosition> goals,
        MapSystem mapSystem,
        EntityManager? entityManager = null,
        Player? player = null)
    {
        int width = mapSystem.MapWidth;
        int height = mapSystem.MapHeight;
        float[,] distanceMap = new float[width, height];

        // Initialize all cells to max value (unreachable)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                distanceMap[x, y] = float.MaxValue;
            }
        }

        // Early exit if no goals
        if (goals == null || goals.Count == 0)
        {
            return distanceMap;
        }

        // Use a queue for BFS (Dijkstra with uniform cost = BFS)
        Queue<GridPosition> queue = new Queue<GridPosition>();

        // Initialize all goal positions with distance 0
        foreach (GridPosition goal in goals)
        {
            if (mapSystem.IsInBounds(goal) && mapSystem.IsWalkable(goal))
            {
                distanceMap[goal.X, goal.Y] = 0;
                queue.Enqueue(goal);
            }
        }

        // Flood fill from all goals simultaneously
        while (queue.Count > 0)
        {
            GridPosition current = queue.Dequeue();
            float currentDistance = distanceMap[current.X, current.Y];

            // Check all neighbors
            foreach (GridPosition direction in Directions)
            {
                GridPosition neighbor = new GridPosition(
                    current.X + direction.X,
                    current.Y + direction.Y
                );

                // Skip if out of bounds or not walkable
                if (!mapSystem.IsInBounds(neighbor) || !mapSystem.IsWalkable(neighbor))
                {
                    continue;
                }

                // Skip if occupied by creature or player (optional)
                if (entityManager != null && player != null &&
                    IsPositionOccupied(neighbor, entityManager, player))
                {
                    continue;
                }

                // Calculate new distance (all moves cost 1)
                float newDistance = currentDistance + 1;

                // If we found a shorter path to this neighbor
                if (newDistance < distanceMap[neighbor.X, neighbor.Y])
                {
                    distanceMap[neighbor.X, neighbor.Y] = newDistance;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distanceMap;
    }

    /// <summary>
    /// Gets the direction to move toward the nearest goal using a distance map.
    /// Returns null if no valid direction exists.
    /// </summary>
    /// <param name="start">Current position</param>
    /// <param name="distanceMap">Pre-computed distance map</param>
    /// <returns>Next position to move toward, or null if stuck/at goal</returns>
    public static GridPosition? GetNearestGoalDirection(GridPosition start, float[,] distanceMap)
    {
        int width = distanceMap.GetLength(0);
        int height = distanceMap.GetLength(1);

        // Validate start position
        if (start.X < 0 || start.X >= width || start.Y < 0 || start.Y >= height)
        {
            return null;
        }

        float currentDistance = distanceMap[start.X, start.Y];

        // If at goal or unreachable, no direction to move
        if (currentDistance == 0 || currentDistance == float.MaxValue)
        {
            return null;
        }

        GridPosition? bestNeighbor = null;
        float bestDistance = currentDistance;

        // Check all neighbors for the lowest distance
        foreach (GridPosition direction in Directions)
        {
            GridPosition neighbor = new GridPosition(
                start.X + direction.X,
                start.Y + direction.Y
            );

            // Skip out of bounds
            if (neighbor.X < 0 || neighbor.X >= width || neighbor.Y < 0 || neighbor.Y >= height)
            {
                continue;
            }

            float neighborDistance = distanceMap[neighbor.X, neighbor.Y];

            // Follow the gradient downhill (lower distance = closer to goal)
            if (neighborDistance < bestDistance)
            {
                bestDistance = neighborDistance;
                bestNeighbor = neighbor;
            }
        }

        return bestNeighbor;
    }

    /// <summary>
    /// Gets a complete path to the nearest goal using a distance map.
    /// Returns empty queue if at goal, null if unreachable.
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="distanceMap">Pre-computed distance map</param>
    /// <returns>Queue of positions to follow, or null if unreachable</returns>
    public static Queue<GridPosition>? GetPathToNearestGoal(GridPosition start, float[,] distanceMap)
    {
        int width = distanceMap.GetLength(0);
        int height = distanceMap.GetLength(1);

        // Validate start position
        if (start.X < 0 || start.X >= width || start.Y < 0 || start.Y >= height)
        {
            return null;
        }

        float currentDistance = distanceMap[start.X, start.Y];

        // If unreachable, return null
        if (currentDistance == float.MaxValue)
        {
            return null;
        }

        // If already at goal, return empty path
        if (currentDistance == 0)
        {
            return new Queue<GridPosition>();
        }

        Queue<GridPosition> path = new Queue<GridPosition>();
        GridPosition current = start;

        // Follow gradient until we reach a goal (distance 0)
        while (distanceMap[current.X, current.Y] > 0)
        {
            GridPosition? next = GetNearestGoalDirection(current, distanceMap);

            // No valid next step (shouldn't happen with valid map)
            if (next == null)
            {
                return null;
            }

            path.Enqueue(next.Value);
            current = next.Value;

            // Safety check to prevent infinite loops
            if (path.Count > width * height)
            {
                return null;
            }
        }

        return path;
    }

    /// <summary>
    /// Checks if a position is occupied by a creature or the player.
    /// Uses EntityManager's O(1) position cache.
    /// </summary>
    private static bool IsPositionOccupied(GridPosition position, EntityManager entityManager, Player player)
    {
        // Check if player is at this position
        if (player.GridPosition.Equals(position))
        {
            return true;
        }

        // Check if any creature is at this position (O(1) lookup)
        return entityManager.IsPositionOccupied(position);
    }
}
