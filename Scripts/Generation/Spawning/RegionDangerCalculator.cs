using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Spawning.Data;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Calculates danger levels for regions based on distance and characteristics.
/// Used for encounter difficulty selection and theme clustering.
/// </summary>
public class RegionDangerCalculator
{
    /// <summary>
    /// Calculates danger level for each region based on characteristics.
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
