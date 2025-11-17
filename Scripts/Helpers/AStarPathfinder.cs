using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Provides A* pathfinding for grid-based movement.
/// Uses Chebyshev distance (8-directional movement with equal cost).
/// </summary>
public static class AStarPathfinder
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
    /// Finds a path from start to goal using A* pathfinding.
    /// Returns null if no path exists.
    /// Considers both terrain and entity occupancy.
    /// </summary>
    /// <param name="start">Starting grid position</param>
    /// <param name="goal">Goal grid position</param>
    /// <param name="mapSystem">Map system for walkability checks</param>
    /// <param name="entityManager">Entity manager for checking creature occupancy</param>
    /// <param name="player">Player reference for checking player occupancy</param>
    /// <returns>Queue of positions to follow (not including start), or null if no path found</returns>
    public static Queue<GridPosition>? FindPath(GridPosition start, GridPosition goal, MapSystem mapSystem, EntityManager entityManager, Player player)
    {
        // Early exit if goal is not walkable
        if (!mapSystem.IsWalkable(goal))
        {
            return null;
        }

        // Early exit if start equals goal
        if (start.Equals(goal))
        {
            return new Queue<GridPosition>();
        }

        var openSet = new PriorityQueue<GridPosition, int>();
        var cameFrom = new Dictionary<GridPosition, GridPosition>();
        var gScore = new Dictionary<GridPosition, int>();
        var fScore = new Dictionary<GridPosition, int>();

        // Initialize start node
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            GridPosition current = openSet.Dequeue();

            // Goal reached
            if (current.Equals(goal))
            {
                return ReconstructPath(cameFrom, current, start);
            }

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

                // Skip if occupied by creature or player (unless it's the goal)
                if (!neighbor.Equals(goal) && IsPositionOccupied(neighbor, entityManager, player))
                {
                    continue;
                }

                // Calculate tentative g score (all moves cost 1 for Chebyshev)
                int tentativeGScore = gScore[current] + 1;

                // If this path to neighbor is better than any previous one
                if (!gScore.TryGetValue(neighbor, out int currentGScore) || tentativeGScore < currentGScore)
                {
                    // Record this path
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Heuristic(neighbor, goal);

                    // Add to open set (PriorityQueue allows duplicates, but we'll always process the best one first)
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        // No path found
        return null;
    }

    /// <summary>
    /// Chebyshev distance heuristic for 8-directional movement.
    /// Returns the maximum of absolute differences in x and y.
    /// </summary>
    private static int Heuristic(GridPosition a, GridPosition b)
    {
        return DistanceHelper.ChebyshevDistance(a, b);
    }

    /// <summary>
    /// Reconstructs the path from goal back to start.
    /// Returns a queue of positions (not including start position).
    /// </summary>
    private static Queue<GridPosition> ReconstructPath(
        Dictionary<GridPosition, GridPosition> cameFrom,
        GridPosition current,
        GridPosition start)
    {
        var path = new Stack<GridPosition>();

        // Build path backwards from goal to start
        while (!current.Equals(start))
        {
            path.Push(current);
            current = cameFrom[current];
        }

        // Convert stack to queue (reverses order)
        var queue = new Queue<GridPosition>();
        while (path.Count > 0)
        {
            queue.Enqueue(path.Pop());
        }

        return queue;
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
