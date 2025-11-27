using System;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Metadata;

namespace PitsOfDespair.Generation.Pipeline;

/// <summary>
/// Shared context passed through all generation passes.
/// Contains the grid, metadata, and inter-pass communication data.
/// </summary>
public class GenerationContext
{
    /// <summary>
    /// Map width in tiles.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Map height in tiles.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// The tile grid being generated. Modified by generation passes.
    /// </summary>
    public TileType[,] Grid { get; set; }

    /// <summary>
    /// Seeded random number generator for reproducible generation.
    /// </summary>
    public Random Random { get; init; }

    /// <summary>
    /// The seed used for this generation (for logging/debugging).
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// Pipeline configuration from YAML.
    /// </summary>
    public PipelineConfig PipelineConfig { get; init; }

    /// <summary>
    /// Accumulated metadata. Populated by metadata analysis pass.
    /// </summary>
    public DungeonMetadata Metadata { get; }

    /// <summary>
    /// Inter-pass communication. Passes can store arbitrary data here
    /// for use by later passes (e.g., BSP tree structure for modifiers).
    /// </summary>
    public Dictionary<string, object> PassData { get; } = new();

    /// <summary>
    /// Name of the base generator that was executed.
    /// </summary>
    public string BaseGeneratorName { get; set; }

    /// <summary>
    /// Create a new generation context from pipeline configuration.
    /// </summary>
    public GenerationContext(PipelineConfig config)
    {
        PipelineConfig = config ?? throw new ArgumentNullException(nameof(config));
        var dimensions = config.Dimensions ?? new DimensionsConfig();
        Width = dimensions.Width;
        Height = dimensions.Height;
        Seed = config.GetActualSeed();
        Random = new Random(Seed);
        Grid = new TileType[Width, Height];
        Metadata = new DungeonMetadata();

        // Initialize grid with walls
        InitializeGrid();
    }

    /// <summary>
    /// Initialize the grid with all walls.
    /// </summary>
    private void InitializeGrid()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Grid[x, y] = TileType.Wall;
            }
        }
    }

    /// <summary>
    /// Check if a position is within the grid bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// Check if a position is within the grid bounds.
    /// </summary>
    public bool IsInBounds(GridPosition pos)
    {
        return IsInBounds(pos.X, pos.Y);
    }

    /// <summary>
    /// Get the tile type at a position (Wall if out of bounds).
    /// </summary>
    public TileType GetTile(int x, int y)
    {
        if (!IsInBounds(x, y)) return TileType.Wall;
        return Grid[x, y];
    }

    /// <summary>
    /// Get the tile type at a position (Wall if out of bounds).
    /// </summary>
    public TileType GetTile(GridPosition pos)
    {
        return GetTile(pos.X, pos.Y);
    }

    /// <summary>
    /// Set the tile type at a position (no-op if out of bounds).
    /// </summary>
    public void SetTile(int x, int y, TileType type)
    {
        if (IsInBounds(x, y))
            Grid[x, y] = type;
    }

    /// <summary>
    /// Set the tile type at a position (no-op if out of bounds).
    /// </summary>
    public void SetTile(GridPosition pos, TileType type)
    {
        SetTile(pos.X, pos.Y, type);
    }

    /// <summary>
    /// Check if a tile is walkable (floor).
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        return GetTile(x, y) == TileType.Floor;
    }

    /// <summary>
    /// Check if a tile is walkable (floor).
    /// </summary>
    public bool IsWalkable(GridPosition pos)
    {
        return IsWalkable(pos.X, pos.Y);
    }

    /// <summary>
    /// Store data for inter-pass communication.
    /// </summary>
    public void SetPassData<T>(string key, T value)
    {
        PassData[key] = value;
    }

    /// <summary>
    /// Retrieve data stored by a previous pass.
    /// </summary>
    public T GetPassData<T>(string key, T defaultValue = default)
    {
        if (PassData.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }

    /// <summary>
    /// Check if pass data exists for a key.
    /// </summary>
    public bool HasPassData(string key)
    {
        return PassData.ContainsKey(key);
    }
}
