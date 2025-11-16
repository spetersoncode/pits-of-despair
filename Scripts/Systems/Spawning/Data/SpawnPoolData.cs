using System.Collections.Generic;
using System.Linq;
using Godot;
using YamlDotNet.Serialization;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Represents a weighted pool of spawn entries (e.g., common, uncommon, rare).
/// </summary>
public class SpawnPoolData
{
    /// <summary>
    /// Identifier for this pool (e.g., "common", "uncommon", "rare").
    /// </summary>
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Weight for selecting this pool when multiple pools exist.
    /// </summary>
    [YamlMember(Alias = "weight")]
    public int Weight { get; set; } = 1;

    /// <summary>
    /// List of spawn entries in this pool.
    /// </summary>
    [YamlMember(Alias = "entries")]
    public List<SpawnEntryData> Entries { get; set; } = new();

    /// <summary>
    /// Selects a random entry from this pool using weighted random selection.
    /// </summary>
    public SpawnEntryData SelectRandomEntry()
    {
        if (Entries == null || Entries.Count == 0)
        {
            return null;
        }

        // Calculate total weight
        int totalWeight = Entries.Sum(e => e.Weight);
        if (totalWeight <= 0)
        {
            return null;
        }

        // Weighted random selection
        int roll = GD.RandRange(1, totalWeight);
        int currentWeight = 0;

        foreach (var entry in Entries)
        {
            currentWeight += entry.Weight;
            if (roll <= currentWeight)
            {
                return entry;
            }
        }

        // Fallback to last entry (should never reach here)
        return Entries.Last();
    }

    /// <summary>
    /// Gets all valid entries in this pool.
    /// </summary>
    public IEnumerable<SpawnEntryData> GetValidEntries()
    {
        return Entries?.Where(e => e.IsValid()) ?? Enumerable.Empty<SpawnEntryData>();
    }

    public override string ToString()
    {
        return $"Pool '{Id}' (Weight: {Weight}, Entries: {Entries?.Count ?? 0})";
    }
}
