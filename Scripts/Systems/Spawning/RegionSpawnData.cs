using System.Collections.Generic;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Spawn-related data assigned to a region during dungeon population.
/// Tracks theme, budget, and spawned encounters for a single region.
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
    /// Total power budget allocated to this region.
    /// </summary>
    public int AllocatedBudget { get; set; } = 0;

    /// <summary>
    /// Remaining power budget after spawning.
    /// </summary>
    public int RemainingBudget { get; set; } = 0;

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

    /// <summary>
    /// Consumes budget when spawning creatures.
    /// </summary>
    /// <param name="threat">Threat cost to consume</param>
    /// <returns>True if budget was available and consumed</returns>
    public bool ConsumeBudget(int threat)
    {
        if (threat > RemainingBudget)
            return false;

        RemainingBudget -= threat;
        TotalThreatSpawned += threat;
        return true;
    }

    /// <summary>
    /// Gets the budget utilization percentage.
    /// </summary>
    public float BudgetUtilization => AllocatedBudget > 0
        ? (float)TotalThreatSpawned / AllocatedBudget
        : 0f;
}
