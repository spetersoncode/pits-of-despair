using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Allocates power budget across regions based on size and characteristics.
/// Larger regions get proportionally more budget.
/// Isolated/dangerous regions get slight increases.
/// </summary>
public class RegionBudgetAllocator
{
    private readonly RandomNumberGenerator _rng;

    public RegionBudgetAllocator()
    {
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Distributes the floor's power budget across all regions.
    /// </summary>
    /// <param name="metadata">Dungeon metadata with regions</param>
    /// <param name="config">Floor spawn configuration</param>
    /// <param name="regionSpawnData">Region spawn data to populate with budgets</param>
    /// <param name="entrancePosition">Player spawn position for distance calculations</param>
    public void AllocateBudgets(
        DungeonMetadata metadata,
        FloorSpawnConfig config,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        GridPosition entrancePosition)
    {
        if (metadata.Regions == null || metadata.Regions.Count == 0)
        {
            GD.PushWarning("RegionBudgetAllocator: No regions to allocate budget to");
            return;
        }

        // Roll total floor budget
        int totalBudget = config.RollPowerBudget();

        // Calculate total weighted area across all regions
        float totalWeightedArea = 0f;
        var regionWeights = new Dictionary<int, float>();

        foreach (var region in metadata.Regions)
        {
            float weight = CalculateRegionWeight(region, metadata, regionSpawnData, entrancePosition);
            regionWeights[region.Id] = weight;
            totalWeightedArea += region.Area * weight;
        }

        // Allocate budget proportionally
        int allocatedTotal = 0;

        foreach (var region in metadata.Regions)
        {
            if (!regionSpawnData.TryGetValue(region.Id, out var spawnData))
                continue;

            float weight = regionWeights[region.Id];
            float proportion = (region.Area * weight) / totalWeightedArea;

            int regionBudget = Mathf.RoundToInt(totalBudget * proportion);

            // Ensure minimum budget for non-trivial regions
            if (region.Area >= 9 && regionBudget < 1)
            {
                regionBudget = 1;
            }

            spawnData.AllocatedBudget = regionBudget;
            spawnData.RemainingBudget = regionBudget;
            allocatedTotal += regionBudget;
        }

        // Distribute any rounding remainder to largest regions
        int remainder = totalBudget - allocatedTotal;
        if (remainder > 0)
        {
            var sortedBySize = metadata.Regions
                .OrderByDescending(r => r.Area)
                .ToList();

            for (int i = 0; i < remainder && i < sortedBySize.Count; i++)
            {
                var region = sortedBySize[i];
                if (regionSpawnData.TryGetValue(region.Id, out var spawnData))
                {
                    spawnData.AllocatedBudget++;
                    spawnData.RemainingBudget++;
                }
            }
        }
    }

    /// <summary>
    /// Calculates the weight multiplier for a region.
    /// Higher weight = more budget allocated.
    /// </summary>
    private float CalculateRegionWeight(
        Region region,
        DungeonMetadata metadata,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        GridPosition entrancePosition)
    {
        float weight = 1.0f;

        // Distance from entrance increases weight (further = more dangerous)
        float distanceFromEntrance = Mathf.Sqrt(
            Mathf.Pow(region.Centroid.X - entrancePosition.X, 2) +
            Mathf.Pow(region.Centroid.Y - entrancePosition.Y, 2)
        );

        // Normalize distance (assume max ~50 tiles typical dungeon)
        float distanceFactor = Mathf.Clamp(distanceFromEntrance / 50f, 0f, 1f);
        weight += distanceFactor * 0.3f; // Up to 30% bonus for distant regions

        // Isolation factor (fewer connections = more dangerous)
        if (regionSpawnData.TryGetValue(region.Id, out var spawnData))
        {
            int connections = spawnData.AdjacentRegionIds.Count;
            if (connections <= 1)
            {
                weight += 0.2f; // Dead ends are more dangerous
                spawnData.DangerLevel = 1.2f;
            }
            else if (connections >= 4)
            {
                weight -= 0.1f; // Well-connected areas are safer
                spawnData.DangerLevel = 0.9f;
            }
        }

        // Special region tags affect weight
        if (!string.IsNullOrEmpty(region.Tag))
        {
            weight += GetTagWeightModifier(region.Tag);
        }

        // Very small regions get reduced weight
        if (region.Area < 16)
        {
            weight *= 0.5f;
        }

        return Mathf.Max(0.1f, weight);
    }

    /// <summary>
    /// Gets weight modifier based on region tag.
    /// </summary>
    private float GetTagWeightModifier(string tag)
    {
        return tag.ToLowerInvariant() switch
        {
            "treasure_room" => 0.5f,   // Treasure rooms need guardians
            "boss_room" => 1.0f,       // Boss rooms get high budget
            "entrance" => -0.5f,       // Entrance area is safer
            "shrine" => 0.3f,          // Shrines have some protection
            "armory" => 0.4f,          // Armories are guarded
            "dead_end" => 0.2f,        // Dead ends slightly more dangerous
            _ => 0f
        };
    }

    /// <summary>
    /// Calculates danger level for a region based on characteristics.
    /// Used for encounter difficulty selection.
    /// </summary>
    public void CalculateDangerLevels(
        DungeonMetadata metadata,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        GridPosition entrancePosition)
    {
        if (metadata.Regions == null)
            return;

        // Find maximum distance for normalization
        float maxDistance = 0f;
        foreach (var region in metadata.Regions)
        {
            float dist = Mathf.Sqrt(
                Mathf.Pow(region.Centroid.X - entrancePosition.X, 2) +
                Mathf.Pow(region.Centroid.Y - entrancePosition.Y, 2)
            );
            maxDistance = Mathf.Max(maxDistance, dist);
        }

        if (maxDistance < 1f) maxDistance = 1f;

        foreach (var region in metadata.Regions)
        {
            if (!regionSpawnData.TryGetValue(region.Id, out var spawnData))
                continue;

            float distance = Mathf.Sqrt(
                Mathf.Pow(region.Centroid.X - entrancePosition.X, 2) +
                Mathf.Pow(region.Centroid.Y - entrancePosition.Y, 2)
            );

            // Base danger level from distance (0.8 to 1.4)
            float normalizedDistance = distance / maxDistance;
            spawnData.DangerLevel = 0.8f + (normalizedDistance * 0.6f);

            // Adjust for isolation
            int connections = spawnData.AdjacentRegionIds.Count;
            if (connections <= 1)
            {
                spawnData.DangerLevel += 0.15f;
            }

            // Adjust for special tags
            if (!string.IsNullOrEmpty(region.Tag))
            {
                spawnData.DangerLevel += GetTagDangerModifier(region.Tag);
            }

            // Clamp to reasonable range
            spawnData.DangerLevel = Mathf.Clamp(spawnData.DangerLevel, 0.5f, 2.0f);
        }
    }

    /// <summary>
    /// Gets danger modifier based on region tag.
    /// </summary>
    private float GetTagDangerModifier(string tag)
    {
        return tag.ToLowerInvariant() switch
        {
            "treasure_room" => 0.3f,
            "boss_room" => 0.5f,
            "entrance" => -0.3f,
            "safe_zone" => -0.5f,
            _ => 0f
        };
    }
}
