using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Analyzers;

/// <summary>
/// Classifies each tile based on its spatial context.
/// Uses wall distance and neighborhood analysis for classification.
/// </summary>
public static class TileClassifier
{
    private static readonly (int dx, int dy)[] CardinalDirections = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    /// <summary>
    /// Threshold for Open vs Edge classification (tiles with wall distance > this are Open).
    /// </summary>
    public const int OpenThreshold = 2;

    /// <summary>
    /// Threshold for Passage classification (tiles with wall distance <= this in narrow areas).
    /// </summary>
    public const int PassageThreshold = 1;

    /// <summary>
    /// Classify all tiles in the grid.
    /// </summary>
    public static TileClassification[,] ClassifyAll(GenerationContext context, DistanceField wallDistance)
    {
        var classifications = new TileClassification[context.Width, context.Height];

        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                classifications[x, y] = ClassifyTile(context, wallDistance, x, y);
            }
        }

        return classifications;
    }

    /// <summary>
    /// Classify a single tile based on its context.
    /// </summary>
    public static TileClassification ClassifyTile(GenerationContext context, DistanceField wallDistance, int x, int y)
    {
        // Walls are always walls
        if (context.GetTile(x, y) == TileType.Wall)
            return TileClassification.Wall;

        var pos = new GridPosition(x, y);
        int dist = wallDistance.GetDistance(pos);

        // Unreachable floor tiles (shouldn't happen, but handle gracefully)
        if (dist == DistanceField.Unreachable)
            return TileClassification.Open;

        // Count adjacent walls (cardinal directions)
        int adjacentWalls = 0;
        foreach (var (dx, dy) in CardinalDirections)
        {
            if (context.GetTile(x + dx, y + dy) == TileType.Wall)
                adjacentWalls++;
        }

        // Dead end: 3 adjacent walls
        if (adjacentWalls >= 3)
            return TileClassification.DeadEnd;

        // Check if this is a narrow passage (low wall distance on opposite sides)
        bool isNarrow = IsNarrowPassage(context, wallDistance, x, y);

        if (isNarrow)
        {
            // Single-tile width chokepoint
            if (dist == 1 && CountWalkableNeighbors(context, x, y) == 2)
                return TileClassification.Chokepoint;

            return TileClassification.Passage;
        }

        // Corner: edge tile with walls on multiple non-opposite sides
        if (dist == 1 && adjacentWalls >= 2 && !HasOppositeWalls(context, x, y))
            return TileClassification.Corner;

        // Edge: adjacent to wall
        if (dist == 1)
            return TileClassification.Edge;

        // Open: interior tile far from walls
        return TileClassification.Open;
    }

    /// <summary>
    /// Check if a tile is in a narrow passage (walls close on opposite sides).
    /// </summary>
    private static bool IsNarrowPassage(GenerationContext context, DistanceField wallDistance, int x, int y)
    {
        var pos = new GridPosition(x, y);
        int dist = wallDistance.GetDistance(pos);

        // Only check tiles close to walls
        if (dist > PassageThreshold + 1)
            return false;

        // Check horizontal narrowness (walls to left and right)
        bool wallLeft = false, wallRight = false;
        for (int dx = 1; dx <= 2; dx++)
        {
            if (context.GetTile(x - dx, y) == TileType.Wall) wallLeft = true;
            if (context.GetTile(x + dx, y) == TileType.Wall) wallRight = true;
        }
        bool horizontallyNarrow = wallLeft && wallRight;

        // Check vertical narrowness (walls above and below)
        bool wallUp = false, wallDown = false;
        for (int dy = 1; dy <= 2; dy++)
        {
            if (context.GetTile(x, y - dy) == TileType.Wall) wallUp = true;
            if (context.GetTile(x, y + dy) == TileType.Wall) wallDown = true;
        }
        bool verticallyNarrow = wallUp && wallDown;

        return horizontallyNarrow || verticallyNarrow;
    }

    /// <summary>
    /// Check if walls are on opposite sides (not a corner).
    /// </summary>
    private static bool HasOppositeWalls(GenerationContext context, int x, int y)
    {
        bool wallN = context.GetTile(x, y - 1) == TileType.Wall;
        bool wallS = context.GetTile(x, y + 1) == TileType.Wall;
        bool wallE = context.GetTile(x + 1, y) == TileType.Wall;
        bool wallW = context.GetTile(x - 1, y) == TileType.Wall;

        return (wallN && wallS) || (wallE && wallW);
    }

    /// <summary>
    /// Count walkable neighbors in cardinal directions.
    /// </summary>
    private static int CountWalkableNeighbors(GenerationContext context, int x, int y)
    {
        int count = 0;
        foreach (var (dx, dy) in CardinalDirections)
        {
            if (context.IsWalkable(x + dx, y + dy))
                count++;
        }
        return count;
    }
}
