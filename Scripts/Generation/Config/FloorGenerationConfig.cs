using System.Collections.Generic;

namespace PitsOfDespair.Generation.Config;

/// <summary>
/// Top-level floor generation configuration loaded from YAML.
/// Defines the generation pipeline and metadata settings for a dungeon floor.
/// </summary>
public class FloorGenerationConfig
{
    /// <summary>
    /// Unique identifier for this floor config.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Human-readable description of this floor type.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Random seed for generation. -1 for time-based random seed.
    /// </summary>
    public int Seed { get; set; } = -1;

    /// <summary>
    /// Map width in tiles.
    /// </summary>
    public int Width { get; set; } = 100;

    /// <summary>
    /// Map height in tiles.
    /// </summary>
    public int Height { get; set; } = 100;

    /// <summary>
    /// Ordered list of generation passes to execute.
    /// </summary>
    public List<PassConfig> Pipeline { get; set; } = new();

    /// <summary>
    /// Metadata analysis configuration.
    /// </summary>
    public MetadataConfig Metadata { get; set; } = new();

    /// <summary>
    /// Spawn table ID to use for this floor.
    /// </summary>
    public string SpawnTable { get; set; }

    /// <summary>
    /// Floor depth range where this config applies.
    /// </summary>
    public int MinFloor { get; set; } = 1;
    public int MaxFloor { get; set; } = 99;

    /// <summary>
    /// Get the actual seed value (time-based if -1).
    /// </summary>
    public int GetActualSeed()
    {
        return Seed == -1 ? (int)System.DateTime.Now.Ticks : Seed;
    }
}
