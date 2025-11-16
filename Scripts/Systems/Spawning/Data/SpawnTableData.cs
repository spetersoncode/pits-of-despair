using System.Collections.Generic;
using System.Linq;
using Godot;
using YamlDotNet.Serialization;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Main spawn table data for a dungeon floor, containing creature and item pools
/// with spawn density configuration.
/// </summary>
public class SpawnTableData
{
    /// <summary>
    /// Name of this spawn table for identification.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Total spawn budget (min-max) for the entire floor.
    /// </summary>
    [YamlMember(Alias = "spawnBudget")]
    public CountRange SpawnBudget { get; set; } = new CountRange { Min = 10, Max = 20 };

    /// <summary>
    /// Chance (0.0 to 1.0) for a room to remain empty.
    /// </summary>
    [YamlMember(Alias = "emptyRoomChance")]
    public float EmptyRoomChance { get; set; } = 0.2f;

    /// <summary>
    /// Chance (0.0 to 1.0) for out-of-depth spawns (harder creatures).
    /// </summary>
    [YamlMember(Alias = "outOfDepthChance")]
    public float OutOfDepthChance { get; set; } = 0.05f;

    /// <summary>
    /// Creature spawn pools (common, uncommon, rare, etc.).
    /// </summary>
    [YamlMember(Alias = "creaturePools")]
    public List<SpawnPoolData> CreaturePools { get; set; } = new();

    /// <summary>
    /// Item spawn pools.
    /// </summary>
    [YamlMember(Alias = "itemPools")]
    public List<SpawnPoolData> ItemPools { get; set; } = new();

    /// <summary>
    /// Out-of-depth creature pools (harder creatures from deeper floors).
    /// </summary>
    [YamlMember(Alias = "outOfDepthPools")]
    public List<SpawnPoolData> OutOfDepthPools { get; set; } = new();

    /// <summary>
    /// Selects a random creature pool using weighted selection.
    /// </summary>
    public SpawnPoolData SelectRandomCreaturePool(bool outOfDepth = false)
    {
        var pools = outOfDepth && OutOfDepthPools.Count > 0 ? OutOfDepthPools : CreaturePools;
        return SelectRandomPool(pools);
    }

    /// <summary>
    /// Selects a random item pool using weighted selection.
    /// </summary>
    public SpawnPoolData SelectRandomItemPool()
    {
        return SelectRandomPool(ItemPools);
    }

    /// <summary>
    /// Performs weighted random selection on a list of pools.
    /// </summary>
    private SpawnPoolData SelectRandomPool(List<SpawnPoolData> pools)
    {
        if (pools == null || pools.Count == 0)
        {
            return null;
        }

        int totalWeight = pools.Sum(p => p.Weight);
        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = GD.RandRange(1, totalWeight);
        int currentWeight = 0;

        foreach (var pool in pools)
        {
            currentWeight += pool.Weight;
            if (roll <= currentWeight)
            {
                return pool;
            }
        }

        return pools.Last();
    }

    /// <summary>
    /// Gets a random spawn budget value within the configured range.
    /// </summary>
    public int GetRandomSpawnBudget()
    {
        return SpawnBudget.GetRandom();
    }

    public override string ToString()
    {
        return $"SpawnTable '{Name}' (Budget: {SpawnBudget.Min}-{SpawnBudget.Max}, " +
               $"Creature Pools: {CreaturePools.Count}, Item Pools: {ItemPools.Count})";
    }
}
