using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Provides standardized distance calculation methods for grid-based gameplay.
/// Different distance metrics are appropriate for different game systems.
/// </summary>
public static class DistanceHelper
{
    /// <summary>
    /// Calculates Chebyshev distance (L∞ norm) between two grid positions.
    /// Also known as "chessboard" or "king's move" distance.
    ///
    /// Chebyshev distance allows 8-directional movement where diagonal moves
    /// cost the same as orthogonal moves.
    /// Distance = max(|x1 - x2|, |y1 - y2|)
    ///
    /// Creates square-shaped ranges.
    ///
    /// Use when:
    /// - Movement allows 8 directions with equal cost
    /// - You want square-shaped areas of effect
    /// - Modeling chess king movement or roguelike grid movement
    /// - Attack ranges in grid-based tactical games
    /// </summary>
    public static int ChebyshevDistance(GridPosition a, GridPosition b)
    {
        return Mathf.Max(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Y - b.Y));
    }

    /// <summary>
    /// Calculates Euclidean distance (L2 norm squared) between two grid positions.
    /// Returns squared distance to avoid expensive square root calculation.
    ///
    /// Distance² = (x1 - x2)² + (y1 - y2)²
    ///
    /// Creates circular-shaped ranges.
    ///
    /// Use when:
    /// - You need circular areas of effect or ranges
    /// - Vision systems and frequent range checks
    /// - Modeling realistic sight/sound propagation
    ///
    /// Note: Returns squared distance for efficiency. To check if within range,
    /// use IsInEuclideanRange() or IsInRange() instead of manual comparison.
    /// </summary>
    public static int EuclideanDistance(GridPosition a, GridPosition b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Checks if two positions are within range using the specified distance metric.
    /// Use this for range checks to ensure correct distance comparison.
    /// </summary>
    /// <param name="from">Starting position</param>
    /// <param name="to">Target position</param>
    /// <param name="range">Maximum range in tiles</param>
    /// <param name="metric">Distance metric to use</param>
    /// <returns>True if within range</returns>
    public static bool IsInRange(GridPosition from, GridPosition to, int range, DistanceMetric metric)
    {
        return metric == DistanceMetric.Euclidean
            ? EuclideanDistance(from, to) <= range * range
            : ChebyshevDistance(from, to) <= range;
    }
}
