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
    /// Calculates squared Euclidean distance (L2 norm squared) between two grid positions.
    /// This is an optimized version that avoids the expensive square root calculation.
    ///
    /// Distance² = (x1 - x2)² + (y1 - y2)²
    ///
    /// Creates circular-shaped ranges.
    ///
    /// Use when:
    /// - You need circular areas of effect or ranges
    /// - You only need to compare distances (not the actual value)
    /// - Performance matters (vision systems, frequent range checks)
    /// - You can compare squared values: distance² <= range²
    ///
    /// Note: Returns squared distance, not actual distance. To check if within range,
    /// compare against range² to avoid sqrt: EuclideanDistanceSquared(a, b) <= range * range
    /// </summary>
    public static int EuclideanDistanceSquared(GridPosition a, GridPosition b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Calculates true Euclidean distance (L2 norm) between two grid positions.
    /// This is the standard "straight-line" distance.
    ///
    /// Distance = √((x1 - x2)² + (y1 - y2)²)
    ///
    /// Creates circular-shaped ranges.
    ///
    /// Use when:
    /// - You need the actual distance value (not just comparison)
    /// - Circular areas of effect or ranges
    /// - Physics calculations requiring real distance
    ///
    /// Note: More expensive than EuclideanDistanceSquared due to sqrt.
    /// Prefer squared version when only comparing distances.
    /// </summary>
    public static float EuclideanDistance(GridPosition a, GridPosition b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
