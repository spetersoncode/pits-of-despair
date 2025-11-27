using System.Collections.Generic;
using PitsOfDespair.Generation.Spawning.Data;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Spawn-related data assigned to a region during dungeon population.
/// Tracks theme, danger level, and spawned encounters for a single region.
/// </summary>
public class RegionSpawnData
{
    /// <summary>
    /// The faction theme assigned to this region.
    /// </summary>
    public FactionTheme Theme { get; set; }

    /// <summary>
    /// Danger level modifier for this region (1.0 = normal).
    /// Higher values increase encounter difficulty selection.
    /// Affected by: distance from entrance, isolation, prefab tags.
    /// </summary>
    public float DangerLevel { get; set; } = 1.0f;

    /// <summary>
    /// Encounters spawned in this region.
    /// </summary>
    public List<SpawnedEncounter> SpawnedEncounters { get; set; } = new();

    /// <summary>
    /// Whether this region's theme was overridden by a prefab SpawnHint.
    /// </summary>
    public bool ThemeOverridden { get; set; } = false;

    /// <summary>
    /// IDs of adjacent regions (for territory clustering).
    /// </summary>
    public List<int> AdjacentRegionIds { get; set; } = new();

    /// <summary>
    /// Whether this region has been fully processed for spawning.
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Total threat spawned in this region.
    /// </summary>
    public int TotalThreatSpawned { get; set; } = 0;
}
