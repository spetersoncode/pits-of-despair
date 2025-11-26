using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Distributes consumables and lesser items across the dungeon floor.
/// Places items based on region danger levels and remaining budget.
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
    /// Distributes items across regions based on budget and danger levels.
    /// </summary>
    /// <param name="regions">Regions to distribute items across</param>
    /// <param name="regionSpawnData">Spawn data per region (for danger levels)</param>
    /// <param name="itemConfig">Item pool configuration</param>
    /// <param name="itemBudget">Total item budget to spend</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>Total budget consumed</returns>
    public int DistributeItems(
        List<Region> regions,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        ItemSpawnConfig itemConfig,
        int itemBudget,
        HashSet<Vector2I> occupiedPositions)
    {
        if (regions == null || regions.Count == 0 || itemBudget <= 0)
            return 0;

        int budgetSpent = 0;

        // Sort regions by danger level (safer regions get more loot)
        var sortedRegions = regions
            .Where(r => regionSpawnData.ContainsKey(r.Id))
            .OrderBy(r => regionSpawnData[r.Id].TotalThreatSpawned)
            .ToList();

        // Distribute budget proportionally, with safer regions getting more
        int regionsToUse = Mathf.Min(sortedRegions.Count, itemBudget);

        for (int i = 0; i < regionsToUse && budgetSpent < itemBudget; i++)
        {
            int regionIndex = i % sortedRegions.Count;
            var region = sortedRegions[regionIndex];
            var spawnData = regionSpawnData[region.Id];

            // Determine rarity based on region danger
            ItemRarity maxRarity = spawnData.TotalThreatSpawned switch
            {
                > 15 => ItemRarity.Rare,      // Dangerous regions can have rare items
                > 5 => ItemRarity.Uncommon,   // Medium regions get uncommon
                _ => ItemRarity.Common         // Safe regions get common
            };

            // Select and place an item
            var (itemId, itemData) = SelectItemWithinRarity(itemConfig, maxRarity);
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

            budgetSpent += itemData.Value > 0 ? itemData.Value : 1;
        }

        return budgetSpent;
    }

    /// <summary>
    /// Places consumable items (potions, scrolls) in the dungeon.
    /// </summary>
    public int PlaceConsumables(
        List<Region> regions,
        ItemSpawnConfig itemConfig,
        int count,
        HashSet<Vector2I> occupiedPositions)
    {
        int placed = 0;

        for (int i = 0; i < count && regions.Count > 0; i++)
        {
            var region = regions[_rng.RandiRange(0, regions.Count - 1)];

            // Select a consumable item (common rarity)
            var (itemId, itemData) = SelectItemWithinRarity(itemConfig, ItemRarity.Common);
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
            placed++;
        }

        return placed;
    }

    /// <summary>
    /// Selects an item up to the given maximum rarity.
    /// </summary>
    private (string id, ItemData data) SelectItemWithinRarity(ItemSpawnConfig config, ItemRarity maxRarity)
    {
        var candidates = new List<(string id, ItemData data, int weight)>();

        // Always include common items
        AddItemsFromPool(candidates, config.CommonItems, config.CommonWeight);

        if (maxRarity >= ItemRarity.Uncommon)
        {
            AddItemsFromPool(candidates, config.UncommonItems, config.UncommonWeight);
        }
        if (maxRarity >= ItemRarity.Rare)
        {
            AddItemsFromPool(candidates, config.RareItems, config.RareWeight);
        }
        if (maxRarity >= ItemRarity.Epic)
        {
            AddItemsFromPool(candidates, config.EpicItems, config.EpicWeight);
        }

        if (candidates.Count == 0)
            return (null, null);

        // Weighted random selection
        int totalWeight = candidates.Sum(c => c.weight);
        if (totalWeight == 0)
        {
            var random = candidates[_rng.RandiRange(0, candidates.Count - 1)];
            return (random.id, random.data);
        }

        int roll = _rng.RandiRange(0, totalWeight - 1);
        int cumulative = 0;

        foreach (var (id, data, weight) in candidates)
        {
            cumulative += weight;
            if (roll < cumulative)
                return (id, data);
        }

        return (candidates[0].id, candidates[0].data);
    }

    private void AddItemsFromPool(
        List<(string id, ItemData data, int weight)> candidates,
        List<string> itemIds,
        int weight)
    {
        foreach (var itemId in itemIds)
        {
            var data = _dataLoader.GetItem(itemId);
            if (data != null)
            {
                candidates.Add((itemId, data, weight));
            }
            else
            {
                GD.PushWarning($"[LootDistributor] Item '{itemId}' not found in data loader");
            }
        }
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
