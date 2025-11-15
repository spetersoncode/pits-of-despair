using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Collection of spawn entries with weighted random selection.
/// Used for data-driven spawning (creatures, items, furniture).
/// </summary>
[GlobalClass]
public partial class SpawnTable : Resource
{
    /// <summary>
    /// Array of possible spawns with their weights and counts.
    /// </summary>
    [Export]
    public SpawnTableEntry[] Entries { get; set; } = System.Array.Empty<SpawnTableEntry>();

    /// <summary>
    /// Select a random EntityData from the table based on weights.
    /// </summary>
    /// <param name="rng">Random number generator to use.</param>
    /// <returns>Selected EntityData, or null if table is empty.</returns>
    public EntityData? SelectRandom(RandomNumberGenerator rng)
    {
        if (Entries == null || Entries.Length == 0)
            return null;

        // Calculate total weight
        int totalWeight = 0;
        foreach (var entry in Entries)
        {
            if (entry?.EntityData != null)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight == 0)
            return null;

        // Select random entry based on weight
        int randomValue = rng.RandiRange(0, totalWeight - 1);
        int currentWeight = 0;

        foreach (var entry in Entries)
        {
            if (entry?.EntityData == null)
                continue;

            currentWeight += entry.Weight;
            if (randomValue < currentWeight)
            {
                return entry.EntityData;
            }
        }

        // Fallback (should never reach here)
        return Entries[0]?.EntityData;
    }

    /// <summary>
    /// Get a random spawn count based on the first entry's min/max.
    /// Note: Currently uses first valid entry. Future: Could vary by selected entry.
    /// </summary>
    /// <param name="rng">Random number generator to use.</param>
    /// <returns>Random count between min and max.</returns>
    public int GetSpawnCount(RandomNumberGenerator rng)
    {
        if (Entries == null || Entries.Length == 0)
            return 0;

        // Use first valid entry's count range
        foreach (var entry in Entries)
        {
            if (entry != null)
            {
                return rng.RandiRange(entry.MinCount, entry.MaxCount);
            }
        }

        return 0;
    }
}
