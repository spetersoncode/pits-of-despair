using System.Collections.Generic;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Weighted entry for theme or encounter selection.
/// </summary>
public class WeightedEntry
{
    /// <summary>
    /// ID of the theme or encounter template.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Selection weight (higher = more likely).
    /// </summary>
    public int Weight { get; set; } = 1;
}

/// <summary>
/// Configuration for a unique creature spawn on a floor.
/// </summary>
public class UniqueSpawnEntry
{
    /// <summary>
    /// Creature ID to spawn.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Chance to spawn on this floor (0.0-1.0).
    /// </summary>
    public float SpawnChance { get; set; } = 1.0f;
}

/// <summary>
/// Configuration for spawning on a specific floor or floor range.
/// Uses density-driven spawning with threat bands and encounter templates.
/// </summary>
public class FloorSpawnConfig
{
    /// <summary>
    /// Unique identifier for this config.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this config.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Minimum floor depth this config applies to.
    /// </summary>
    public int MinFloor { get; set; } = 1;

    /// <summary>
    /// Maximum floor depth this config applies to.
    /// </summary>
    public int MaxFloor { get; set; } = 99;

    /// <summary>
    /// Weighted list of faction themes available on this floor.
    /// </summary>
    public List<WeightedEntry> ThemeWeights { get; set; } = new();

    /// <summary>
    /// Weighted list of encounter templates available on this floor.
    /// </summary>
    public List<WeightedEntry> EncounterWeights { get; set; } = new();

    /// <summary>
    /// Chance (0.0-1.0) to spawn an out-of-depth creature or item.
    /// </summary>
    public float CreatureOutOfDepthChance { get; set; } = 0.0f;

    /// <summary>
    /// How many floors ahead to pull out-of-depth creatures/items from.
    /// </summary>
    public int OutOfDepthFloors { get; set; } = 2;

    /// <summary>
    /// Minimum threat rating for creatures on this floor.
    /// Creatures below this are filtered out.
    /// </summary>
    public int MinThreat { get; set; } = 0;

    /// <summary>
    /// Maximum threat rating for regular spawns on this floor.
    /// Higher threat creatures only appear via out-of-depth mechanic.
    /// </summary>
    public int MaxThreat { get; set; } = 999;

    /// <summary>
    /// Chance (0.0-1.0) for each region to receive an encounter.
    /// Default 0.7 (70%).
    /// </summary>
    public float EncounterChance { get; set; } = 0.7f;

    /// <summary>
    /// Maximum encounters as a ratio of total regions (0.0-1.0).
    /// Caps total encounters to prevent over-spawning. Default 0.6 (60% of regions).
    /// </summary>
    public float MaxEncounterRatio { get; set; } = 0.6f;

    #region Item Density Spawning

    /// <summary>
    /// Target item density as percentage of walkable tiles (0.0-1.0).
    /// Default 0.03 (3% of tiles get items).
    /// </summary>
    public float ItemDensity { get; set; } = 0.03f;

    /// <summary>
    /// Minimum item value for this floor. If null, defaults to MinThreat.
    /// </summary>
    public int? MinItemValue { get; set; } = null;

    /// <summary>
    /// Maximum item value for this floor. If null, defaults to MaxThreat.
    /// </summary>
    public int? MaxItemValue { get; set; } = null;

    /// <summary>
    /// Gets the effective minimum item value (defaults to MinThreat if not specified).
    /// </summary>
    public int GetMinItemValue() => MinItemValue ?? MinThreat;

    /// <summary>
    /// Gets the effective maximum item value (defaults to MaxThreat if not specified).
    /// </summary>
    public int GetMaxItemValue() => MaxItemValue ?? MaxThreat;

    #endregion

    #region Gold Density Spawning

    /// <summary>
    /// Target gold pile density as percentage of walkable tiles (0.0-1.0).
    /// Default 0.04 (4% of tiles get gold piles).
    /// </summary>
    public float GoldDensity { get; set; } = 0.04f;

    /// <summary>
    /// Base gold amount per pile before floor scaling.
    /// </summary>
    public int BaseGoldPerPile { get; set; } = 5;

    /// <summary>
    /// Additional gold per pile per floor depth (multiplied by floor number).
    /// Default 1.0 means +1 gold per pile per floor.
    /// </summary>
    public float GoldFloorScale { get; set; } = 1.0f;

    #endregion

    /// <summary>
    /// Unique creatures with spawn chances for this floor.
    /// Each unique only spawns once per run.
    /// </summary>
    public List<UniqueSpawnEntry> UniqueCreatures { get; set; } = new();
}
