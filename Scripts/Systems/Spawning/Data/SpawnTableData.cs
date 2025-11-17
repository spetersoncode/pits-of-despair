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
    /// Total creature spawn budget for the entire floor.
    /// </summary>
    [YamlMember(Alias = "creatureBudget")]
    public CountRange CreatureBudget { get; set; } = new CountRange { DiceNotation = "1d11+9" };

    /// <summary>
    /// Total item spawn budget for the entire floor.
    /// </summary>
    [YamlMember(Alias = "itemBudget")]
    public CountRange ItemBudget { get; set; } = new CountRange { DiceNotation = "1d6+4" };

    /// <summary>
    /// Total gold spawn budget for the entire floor.
    /// </summary>
    [YamlMember(Alias = "goldBudget")]
    public CountRange GoldBudget { get; set; } = new CountRange { DiceNotation = "10d20+100" };

    /// <summary>
    /// Creature spawn pools (common, uncommon, rare, etc.).
    /// Powerful creatures should be placed in rare pools with low weights.
    /// </summary>
    [YamlMember(Alias = "creaturePools")]
    public List<SpawnPoolData> CreaturePools { get; set; } = new();

    /// <summary>
    /// Item spawn pools.
    /// </summary>
    [YamlMember(Alias = "itemPools")]
    public List<SpawnPoolData> ItemPools { get; set; } = new();

    /// <summary>
    /// Selects a random creature pool using weighted selection.
    /// </summary>
    public SpawnPoolData SelectRandomCreaturePool()
    {
        return SelectRandomPool(CreaturePools);
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
    /// Gets a random creature spawn budget value within the configured range.
    /// </summary>
    public int GetRandomCreatureBudget()
    {
        return CreatureBudget.GetRandom();
    }

    /// <summary>
    /// Gets a random item spawn budget value within the configured range.
    /// </summary>
    public int GetRandomItemBudget()
    {
        return ItemBudget.GetRandom();
    }

    /// <summary>
    /// Gets a random gold spawn budget value within the configured range.
    /// </summary>
    public int GetRandomGoldBudget()
    {
        return GoldBudget.GetRandom();
    }

    public override string ToString()
    {
        return $"SpawnTable '{Name}' (Creatures: {CreatureBudget.DiceNotation}, " +
               $"Items: {ItemBudget.DiceNotation}, " +
               $"Gold: {GoldBudget.DiceNotation}, " +
               $"Creature Pools: {CreaturePools.Count}, Item Pools: {ItemPools.Count})";
    }
}
