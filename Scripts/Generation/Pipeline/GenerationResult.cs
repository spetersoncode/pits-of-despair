using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;

namespace PitsOfDespair.Generation.Pipeline;

/// <summary>
/// Result of a complete dungeon generation pipeline execution.
/// Contains the final grid, metadata, and generation diagnostics.
/// </summary>
public class GenerationResult
{
    /// <summary>
    /// The generated tile grid.
    /// </summary>
    public TileType[,] Grid { get; init; }

    /// <summary>
    /// Computed dungeon metadata for downstream systems.
    /// </summary>
    public DungeonMetadata Metadata { get; init; }

    /// <summary>
    /// The random seed used for generation.
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// Name of the base generator pass that was executed.
    /// </summary>
    public string BaseGenerator { get; init; }

    /// <summary>
    /// Ordered list of pass names that were executed.
    /// </summary>
    public List<string> PassesExecuted { get; init; } = new();

    /// <summary>
    /// Width of the generated map.
    /// </summary>
    public int Width => Grid?.GetLength(0) ?? 0;

    /// <summary>
    /// Height of the generated map.
    /// </summary>
    public int Height => Grid?.GetLength(1) ?? 0;

    /// <summary>
    /// Count the number of floor tiles in the grid.
    /// </summary>
    public int CountFloorTiles()
    {
        if (Grid == null) return 0;

        int count = 0;
        int width = Grid.GetLength(0);
        int height = Grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Grid[x, y] == TileType.Floor)
                    count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Get the percentage of tiles that are walkable.
    /// </summary>
    public float GetWalkablePercent()
    {
        if (Grid == null) return 0f;
        int total = Grid.GetLength(0) * Grid.GetLength(1);
        return (float)CountFloorTiles() / total * 100f;
    }
}
