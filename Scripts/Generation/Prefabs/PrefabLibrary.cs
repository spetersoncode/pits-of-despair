using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PitsOfDespair.Generation.Prefabs;

/// <summary>
/// Service for querying and selecting prefabs.
/// Prefabs are loaded by DataLoader; this provides query functionality.
/// </summary>
public class PrefabLibrary
{
    private readonly Dictionary<string, PrefabData> _prefabs;
    private readonly Random _random;

    // Frequency weights for selection
    private static readonly Dictionary<string, int> FrequencyWeights = new()
    {
        { "common", 10 },
        { "uncommon", 5 },
        { "rare", 2 },
        { "unique", 1 }
    };

    public PrefabLibrary(Dictionary<string, PrefabData> prefabs, Random random = null)
    {
        _prefabs = prefabs ?? new Dictionary<string, PrefabData>();
        _random = random ?? new Random();
    }

    /// <summary>
    /// Get a prefab by ID.
    /// </summary>
    public PrefabData GetPrefab(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _prefabs.TryGetValue(id.ToLowerInvariant(), out var prefab);
        return prefab;
    }

    /// <summary>
    /// Get all prefabs valid for a given floor depth.
    /// </summary>
    public List<PrefabData> GetPrefabsForFloor(int floorDepth)
    {
        return _prefabs.Values
            .Where(p => floorDepth >= p.Placement.MinFloor && floorDepth <= p.Placement.MaxFloor)
            .ToList();
    }

    /// <summary>
    /// Get prefabs that can fit in a region of the given size.
    /// </summary>
    public List<PrefabData> GetPrefabsForRegionSize(int regionArea, int floorDepth)
    {
        return GetPrefabsForFloor(floorDepth)
            .Where(p => p.Placement.MinRegionSize <= regionArea)
            .Where(p => p.GetWidth() * p.GetHeight() <= regionArea)
            .ToList();
    }

    /// <summary>
    /// Select prefabs from a pool configuration.
    /// </summary>
    /// <param name="pool">Pool configuration with prefab IDs and weights.</param>
    /// <param name="budget">Maximum number of prefabs to select.</param>
    /// <param name="floorDepth">Current floor depth for filtering.</param>
    public List<PrefabData> SelectFromPool(List<PrefabPoolEntry> pool, int budget, int floorDepth)
    {
        if (pool == null || pool.Count == 0 || budget <= 0)
            return new List<PrefabData>();

        var result = new List<PrefabData>();
        var availablePool = new List<PrefabPoolEntry>(pool);

        while (result.Count < budget && availablePool.Count > 0)
        {
            // Calculate total weight
            int totalWeight = availablePool.Sum(e => e.Weight);
            if (totalWeight <= 0) break;

            // Weighted random selection
            int roll = _random.Next(totalWeight);
            int cumulative = 0;
            PrefabPoolEntry selected = null;

            foreach (var entry in availablePool)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                {
                    selected = entry;
                    break;
                }
            }

            if (selected == null) break;

            var prefab = GetPrefab(selected.PrefabId);
            if (prefab != null && floorDepth >= prefab.Placement.MinFloor && floorDepth <= prefab.Placement.MaxFloor)
            {
                result.Add(prefab);

                // Unique prefabs can only be selected once
                if (prefab.Placement.Frequency?.ToLowerInvariant() == "unique")
                {
                    availablePool.Remove(selected);
                }
            }
            else
            {
                // Invalid prefab, remove from pool
                availablePool.Remove(selected);
            }
        }

        return result;
    }

    /// <summary>
    /// Select random prefabs based on floor depth and frequency weights.
    /// </summary>
    public List<PrefabData> SelectRandom(int count, int floorDepth, int? maxRegionSize = null)
    {
        var candidates = GetPrefabsForFloor(floorDepth);

        if (maxRegionSize.HasValue)
        {
            candidates = candidates
                .Where(p => p.GetWidth() * p.GetHeight() <= maxRegionSize.Value)
                .ToList();
        }

        if (candidates.Count == 0) return new List<PrefabData>();

        var result = new List<PrefabData>();
        var available = new List<PrefabData>(candidates);

        while (result.Count < count && available.Count > 0)
        {
            // Calculate weighted selection
            int totalWeight = available.Sum(GetFrequencyWeight);
            if (totalWeight <= 0) break;

            int roll = _random.Next(totalWeight);
            int cumulative = 0;
            PrefabData selected = null;

            foreach (var prefab in available)
            {
                cumulative += GetFrequencyWeight(prefab);
                if (roll < cumulative)
                {
                    selected = prefab;
                    break;
                }
            }

            if (selected == null) break;

            result.Add(selected);

            // Remove unique prefabs from pool
            if (selected.Placement.Frequency?.ToLowerInvariant() == "unique")
            {
                available.Remove(selected);
            }
        }

        return result;
    }

    private int GetFrequencyWeight(PrefabData prefab)
    {
        var freq = prefab.Placement.Frequency?.ToLowerInvariant() ?? "common";
        return FrequencyWeights.TryGetValue(freq, out var weight) ? weight : 5;
    }

    /// <summary>
    /// Get all loaded prefab IDs.
    /// </summary>
    public IEnumerable<string> GetAllPrefabIds() => _prefabs.Keys;

    /// <summary>
    /// Get total count of loaded prefabs.
    /// </summary>
    public int Count => _prefabs.Count;
}

/// <summary>
/// Entry in a prefab pool for weighted selection.
/// </summary>
public class PrefabPoolEntry
{
    /// <summary>
    /// Prefab ID to select.
    /// </summary>
    public string PrefabId { get; set; }

    /// <summary>
    /// Selection weight (higher = more likely).
    /// </summary>
    public int Weight { get; set; } = 1;
}
