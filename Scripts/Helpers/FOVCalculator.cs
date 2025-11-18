using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Distance metric for range calculations.
/// </summary>
public enum DistanceMetric
{
    /// <summary>
    /// Euclidean distance (circular area) - realistic vision/area effects.
    /// </summary>
    Euclidean,

    /// <summary>
    /// Chebyshev distance (square area) - grid-based targeting/movement.
    /// Diagonals count as distance 1 (king's move).
    /// </summary>
    Chebyshev
}

/// <summary>
/// Calculates field-of-view for grid-based vision systems.
/// Uses the recursive shadowcasting algorithm for symmetric, efficient line-of-sight.
/// </summary>
public static class FOVCalculator
{
    /// <summary>
    /// Calculates all tiles visible from the origin point within the specified range.
    /// Uses recursive shadowcasting algorithm for symmetric, efficient FOV.
    /// </summary>
    /// <param name="origin">The grid position to calculate vision from</param>
    /// <param name="range">Maximum vision distance in tiles</param>
    /// <param name="mapSystem">Map system to query for blocking tiles</param>
    /// <param name="distanceMetric">Distance metric to use (Euclidean for circular, Chebyshev for square)</param>
    /// <returns>HashSet of all visible grid positions</returns>
    public static HashSet<GridPosition> CalculateVisibleTiles(
        GridPosition origin,
        int range,
        MapSystem mapSystem,
        DistanceMetric distanceMetric = DistanceMetric.Euclidean)
    {
        var visibleTiles = new HashSet<GridPosition>();

        // Origin is always visible
        visibleTiles.Add(origin);

        // Cast shadows in all 8 octants
        for (int octant = 0; octant < 8; octant++)
        {
            CastLight(visibleTiles, mapSystem, origin, range, 1, 1.0f, 0.0f, octant, distanceMetric);
        }

        return visibleTiles;
    }

    /// <summary>
    /// Recursively casts light (calculates visibility) in a single octant.
    /// </summary>
    private static void CastLight(
        HashSet<GridPosition> visibleTiles,
        MapSystem mapSystem,
        GridPosition origin,
        int range,
        int row,
        float startSlope,
        float endSlope,
        int octant,
        DistanceMetric distanceMetric)
    {
        if (startSlope < endSlope)
        {
            return;
        }

        float nextStartSlope = startSlope;

        for (int i = row; i <= range; i++)
        {
            bool blocked = false;

            for (int dx = -i, dy = -i; dx <= 0; dx++)
            {
                float leftSlope = (dx - 0.5f) / (dy + 0.5f);
                float rightSlope = (dx + 0.5f) / (dy - 0.5f);

                if (startSlope < rightSlope)
                {
                    continue;
                }
                else if (endSlope > leftSlope)
                {
                    break;
                }

                // Transform octant coordinates to actual map coordinates
                GridPosition current = TransformOctant(origin, dx, dy, octant);

                // Check if tile is in range using the specified distance metric
                bool inRange;
                if (distanceMetric == DistanceMetric.Euclidean)
                {
                    // Euclidean: circular range (returns squared distance)
                    int distanceSquared = DistanceHelper.EuclideanDistance(current, origin);
                    inRange = distanceSquared <= range * range;
                }
                else // Chebyshev
                {
                    // Chebyshev: square range (grid-based, diagonals = 1)
                    int distance = DistanceHelper.ChebyshevDistance(current, origin);
                    inRange = distance <= range;
                }

                if (inRange)
                {
                    visibleTiles.Add(current);
                }

                // Check if this tile blocks vision
                bool isBlocking = IsBlocking(mapSystem, current);

                if (blocked)
                {
                    // We're scanning a row that's in shadow
                    if (isBlocking)
                    {
                        // This blocking tile extends the shadow
                        nextStartSlope = rightSlope;
                        continue;
                    }
                    else
                    {
                        // We've found a non-blocking tile, exit shadow
                        blocked = false;
                        startSlope = nextStartSlope;
                    }
                }
                else
                {
                    // We're not in shadow
                    if (isBlocking && i < range)
                    {
                        // This tile starts casting a shadow
                        blocked = true;

                        // Recursively scan the shadowed area
                        CastLight(visibleTiles, mapSystem, origin, range, i + 1, startSlope, leftSlope, octant, distanceMetric);

                        nextStartSlope = rightSlope;
                    }
                }
            }

            if (blocked)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Transforms octant-relative coordinates to actual map coordinates.
    /// Each octant has a different transformation to cover all 8 directions.
    /// </summary>
    private static GridPosition TransformOctant(GridPosition origin, int dx, int dy, int octant)
    {
        return octant switch
        {
            0 => new GridPosition(origin.X + dx, origin.Y - dy),  // North-Northeast
            1 => new GridPosition(origin.X + dy, origin.Y - dx),  // North-Northwest
            2 => new GridPosition(origin.X - dy, origin.Y - dx),  // West-Northwest
            3 => new GridPosition(origin.X - dx, origin.Y - dy),  // West-Southwest
            4 => new GridPosition(origin.X - dx, origin.Y + dy),  // South-Southwest
            5 => new GridPosition(origin.X - dy, origin.Y + dx),  // South-Southeast
            6 => new GridPosition(origin.X + dy, origin.Y + dx),  // East-Southeast
            7 => new GridPosition(origin.X + dx, origin.Y + dy),  // East-Northeast
            _ => origin
        };
    }

    /// <summary>
    /// Determines if a tile blocks line-of-sight.
    /// </summary>
    private static bool IsBlocking(MapSystem mapSystem, GridPosition position)
    {
        // Out of bounds is considered blocking
        if (!mapSystem.IsInBounds(position))
        {
            return true;
        }

        // Walls block vision
        TileType tile = mapSystem.GetTileAt(position);
        return tile == TileType.Wall;
    }
}
