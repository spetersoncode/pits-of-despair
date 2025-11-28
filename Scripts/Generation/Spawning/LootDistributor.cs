using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Spawning.Data;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Distributes items across the dungeon floor using density-based spawning.
/// Items are filtered by value range and weighted inversely by value (common items more frequent).
/// </summary>
public class LootDistributor
{
    private readonly DataLoader _dataLoader;
    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly RandomNumberGenerator _rng;

    public LootDistributor(
        DataLoader dataLoader,
        EntityFactory entityFactory,
        EntityManager entityManager)
    {
        _dataLoader = dataLoader;
        _entityFactory = entityFactory;
        _entityManager = entityManager;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Distributes items across regions using density-based spawning.
    /// Items are selected based on value range and weighted inversely by value.
    /// </summary>
    /// <param name="regions">Regions to distribute items across</param>
    /// <param name="regionSpawnData">Spawn data per region (for danger levels)</param>
    /// <param name="config">Floor spawn configuration with density and value parameters</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>Tuple of (items placed, total value)</returns>
    public (int count, int value) DistributeItemsByDensity(
        List<Region> regions,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        FloorSpawnConfig config,
        HashSet<Vector2I> occupiedPositions)
    {
        if (regions == null || regions.Count == 0)
            return (0, 0);

        // Calculate target item count from density
        int totalTiles = regions.Sum(r => r.Tiles?.Count ?? 0);
        int targetItems = Mathf.Max(1, Mathf.RoundToInt(totalTiles * config.ItemDensity));

        // Get valid items within value range
        int minValue = config.GetMinItemValue();
        int maxValue = config.GetMaxItemValue();
        var validItems = GetSpawnableItems(minValue, maxValue);

        // Check for out-of-depth items
        if (config.CreatureOutOfDepthChance > 0 && _rng.Randf() < config.CreatureOutOfDepthChance)
        {
            int oodMinValue = maxValue + 1;
            int oodMaxValue = maxValue + config.OutOfDepthFloors * 2;
            var oodItems = GetSpawnableItems(oodMinValue, oodMaxValue);
            if (oodItems.Count > 0)
            {
                validItems.AddRange(oodItems);
                GD.Print($"[LootDistributor] Out-of-depth triggered! Added {oodItems.Count} items from value range {oodMinValue}-{oodMaxValue}");
            }
        }

        if (validItems.Count == 0)
        {
            GD.PushWarning($"[LootDistributor] No spawnable items found in value range {minValue}-{maxValue}");
            return (0, 0);
        }

        int itemsPlaced = 0;
        int totalValue = 0;

        // Sort regions by danger (more dangerous = better loot placement preference)
        var sortedRegions = regions
            .Where(r => regionSpawnData.ContainsKey(r.Id))
            .OrderByDescending(r => regionSpawnData[r.Id].DangerLevel)
            .ToList();

        if (sortedRegions.Count == 0)
            sortedRegions = regions.ToList();

        // Distribute items across regions
        for (int i = 0; i < targetItems; i++)
        {
            // Cycle through regions, with bias towards dangerous regions for higher value items
            int regionIndex = i % sortedRegions.Count;
            var region = sortedRegions[regionIndex];

            // Select item with inverse value weighting and type multiplier
            var (itemId, itemData) = SelectItemWeighted(validItems, minValue, maxValue);
            if (itemData == null)
                continue;

            var position = FindLootPosition(region, occupiedPositions);
            if (position == null)
                continue;

            var itemEntity = _entityFactory.CreateItem(itemId, position.Value);
            if (itemEntity == null)
                continue;

            _entityManager.AddEntity(itemEntity);
            occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
            itemsPlaced++;
            totalValue += itemData.Value;
        }

        GD.Print($"[LootDistributor] Placed {itemsPlaced}/{targetItems} items (density {config.ItemDensity:P0} of {totalTiles} tiles, value range {minValue}-{maxValue})");
        return (itemsPlaced, totalValue);
    }

    /// <summary>
    /// Gets all items that can spawn automatically within the given value range.
    /// Items with Value <= 0 bypass floor filtering (always spawnable).
    /// </summary>
    private List<(string id, ItemData data)> GetSpawnableItems(int minValue, int maxValue)
    {
        var result = new List<(string id, ItemData data)>();

        foreach (var itemData in _dataLoader.Items.GetAll())
        {
            // Skip items marked as not auto-spawning
            if (itemData.NoAutoSpawn)
                continue;

            // Items with no value (0 or less) bypass floor filtering - always spawnable
            if (itemData.Value <= 0)
            {
                result.Add((itemData.DataFileId, itemData));
                continue;
            }

            // Check value range for items with positive value
            if (itemData.Value >= minValue && itemData.Value <= maxValue)
            {
                result.Add((itemData.DataFileId, itemData));
            }
        }

        return result;
    }

    /// <summary>
    /// Selects an item using combined weighting:
    /// 1. Inverse value weighting (lower value = more common)
    /// 2. Type-based spawn weight multiplier (consumables more common than equipment)
    /// Items with Value <= 0 are treated as having maxValue for weighting (rare).
    /// </summary>
    private (string id, ItemData data) SelectItemWeighted(List<(string id, ItemData data)> items, int minValue, int maxValue)
    {
        if (items == null || items.Count == 0)
            return (null, null);

        // Calculate weights with type multiplier
        var weighted = items.Select(i => {
            // Valueless items use maxValue (rarest tier - they bypass floor filtering so should be rare)
            int effectiveValue = i.data.Value > 0 ? i.data.Value : maxValue;

            // Base weight from inverse value (lower value = higher weight)
            int baseWeight = Mathf.Max(1, maxValue - effectiveValue + 1);

            // Apply type-based spawn weight multiplier
            float finalWeight = baseWeight * i.data.GetSpawnWeightMultiplier();

            return (
                id: i.id,
                data: i.data,
                weight: Mathf.Max(1, Mathf.RoundToInt(finalWeight))
            );
        }).ToList();

        int totalWeight = weighted.Sum(w => w.weight);
        if (totalWeight == 0)
        {
            var random = items[_rng.RandiRange(0, items.Count - 1)];
            return (random.id, random.data);
        }

        int roll = _rng.RandiRange(0, totalWeight - 1);
        int cumulative = 0;

        foreach (var (id, data, weight) in weighted)
        {
            cumulative += weight;
            if (roll < cumulative)
                return (id, data);
        }

        return (items[0].id, items[0].data);
    }

    /// <summary>
    /// Finds a position for loot in a region. Prefers interior tiles.
    /// </summary>
    private GridPosition? FindLootPosition(Region region, HashSet<Vector2I> occupiedPositions)
    {
        if (region?.Tiles == null || region.Tiles.Count == 0)
            return null;

        var availableTiles = region.Tiles
            .Select(t => new Vector2I(t.X, t.Y))
            .Where(t => !occupiedPositions.Contains(t))
            .ToList();

        if (availableTiles.Count == 0)
            return null;

        var tile = availableTiles[_rng.RandiRange(0, availableTiles.Count - 1)];
        return new GridPosition(tile.X, tile.Y);
    }
}
