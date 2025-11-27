using System.Collections.Generic;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Generation.Config;

/// <summary>
/// Configuration for map dimensions.
/// </summary>
public class DimensionsConfig
{
    /// <summary>
    /// Map width in tiles. Default: 80
    /// </summary>
    public int Width { get; set; } = 80;

    /// <summary>
    /// Map height in tiles. Default: 70
    /// </summary>
    public int Height { get; set; } = 70;
}

/// <summary>
/// Layout-dependent spawn settings that vary by map generation strategy.
/// These settings are threat-neutral and depend on map structure.
/// </summary>
public class PipelineSpawnSettings
{
    /// <summary>
    /// Target item density as percentage of walkable tiles (0.0-1.0).
    /// Default: 0.03 (3% of tiles get items)
    /// </summary>
    public float ItemDensity { get; set; } = 0.03f;

    /// <summary>
    /// Target gold pile density as percentage of walkable tiles (0.0-1.0).
    /// Default: 0.04 (4% of tiles get gold piles)
    /// </summary>
    public float GoldDensity { get; set; } = 0.04f;

    /// <summary>
    /// Chance (0.0-1.0) for each region to receive an encounter.
    /// Default: 0.7 (70%)
    /// </summary>
    public float EncounterChance { get; set; } = 0.7f;

    /// <summary>
    /// Maximum encounters as ratio of total regions (0.0-1.0).
    /// Default: 0.6 (60% of regions max)
    /// </summary>
    public float MaxEncounterRatio { get; set; } = 0.6f;

    /// <summary>
    /// Minimum distance between encounter centers (in tiles).
    /// Default: 6
    /// </summary>
    public int MinEncounterSpacing { get; set; } = 6;

    /// <summary>
    /// Maximum encounters per region. Default: 1
    /// </summary>
    public int MaxEncountersPerRegion { get; set; } = 1;

    /// <summary>
    /// Radius around player start where no encounters spawn (in tiles).
    /// Default: 13
    /// </summary>
    public int PlayerExclusionRadius { get; set; } = 13;

    /// <summary>
    /// Multiplier for template weight when region type matches preferred regions.
    /// Default: 1.5 (50% bonus)
    /// </summary>
    public float RegionMatchMultiplier { get; set; } = 1.5f;

    /// <summary>
    /// Multiplier for ambush template weight in dangerous regions.
    /// Default: 1.3 (30% bonus)
    /// </summary>
    public float DangerBonusMultiplier { get; set; } = 1.3f;

    /// <summary>
    /// Minimum creature count for floor validation. Default: 3
    /// </summary>
    public int MinCreatureCount { get; set; } = 3;

    /// <summary>
    /// Encounter types suited to this layout with weights.
    /// </summary>
    public List<WeightedEntry> EncounterWeights { get; set; } = new();
}

/// <summary>
/// Map generation pipeline configuration.
/// Defines the generation strategy and layout-dependent spawn settings.
/// Loaded from Data/Pipelines/*.yaml
/// </summary>
public class PipelineConfig
{
    /// <summary>
    /// Unique identifier for this pipeline.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Human-readable description of this pipeline.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Random seed for generation. -1 for time-based random seed.
    /// </summary>
    public int Seed { get; set; } = -1;

    /// <summary>
    /// Map dimensions for this pipeline.
    /// </summary>
    public DimensionsConfig Dimensions { get; set; } = new();

    /// <summary>
    /// Ordered list of generation passes to execute.
    /// </summary>
    public List<PassConfig> Passes { get; set; } = new();

    /// <summary>
    /// Metadata analysis configuration.
    /// </summary>
    public MetadataConfig Metadata { get; set; } = new();

    /// <summary>
    /// Layout-dependent spawn settings for this pipeline.
    /// </summary>
    public PipelineSpawnSettings SpawnSettings { get; set; } = new();

    /// <summary>
    /// Get the actual seed value (time-based if -1).
    /// </summary>
    public int GetActualSeed()
    {
        return Seed == -1 ? (int)System.DateTime.Now.Ticks : Seed;
    }
}
