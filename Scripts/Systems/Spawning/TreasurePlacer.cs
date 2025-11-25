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
/// Places valuable items with guardians. Implements risk = reward principle.
/// Higher value items are placed with tougher guardians.
/// </summary>
public class TreasurePlacer
{
    private readonly DataLoader _dataLoader;
    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly RandomNumberGenerator _rng;

    public TreasurePlacer(
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
    /// Places a guarded treasure in a region. Returns the item value consumed from budget.
    /// </summary>
    public int PlaceGuardedTreasure(
        Region region,
        ItemSpawnConfig itemConfig,
        int itemBudget,
        int guardianThreat,
        HashSet<Vector2I> occupiedPositions)
    {
        // Select item based on guardian strength (stronger guardian = rarer item)
        var (itemId, itemData) = SelectItemForGuardian(itemConfig, guardianThreat);
        if (itemData == null)
            return 0;

        // Find position for treasure (prefer center/alcove areas)
        var position = FindTreasurePosition(region, occupiedPositions);
        if (position == null)
            return 0;

        // Create and place the item
        var itemEntity = _entityFactory.CreateItem(itemId, position.Value);
        if (itemEntity == null)
            return 0;

        _entityManager.AddEntity(itemEntity);
        occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));

        return itemData.Value > 0 ? itemData.Value : 1;
    }

    /// <summary>
    /// Places an unguarded treasure in a low-danger region.
    /// </summary>
    public int PlaceUnguardedTreasure(
        Region region,
        ItemSpawnConfig itemConfig,
        int itemBudget,
        HashSet<Vector2I> occupiedPositions)
    {
        // Select a common/uncommon item for unguarded placement
        var (itemId, itemData) = SelectItemByRarity(itemConfig, ItemRarity.Common, ItemRarity.Uncommon);
        if (itemData == null)
            return 0;

        var position = FindTreasurePosition(region, occupiedPositions);
        if (position == null)
            return 0;

        var itemEntity = _entityFactory.CreateItem(itemId, position.Value);
        if (itemEntity == null)
            return 0;

        _entityManager.AddEntity(itemEntity);
        occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));

        return itemData.Value > 0 ? itemData.Value : 1;
    }

    /// <summary>
    /// Selects an item appropriate for the guardian's threat level.
    /// Higher threat guardians guard rarer items.
    /// </summary>
    private (string id, ItemData data) SelectItemForGuardian(ItemSpawnConfig config, int guardianThreat)
    {
        // Map guardian threat to item rarity
        ItemRarity minRarity;
        ItemRarity maxRarity;

        if (guardianThreat >= 16)
        {
            minRarity = ItemRarity.Rare;
            maxRarity = ItemRarity.Epic;
        }
        else if (guardianThreat >= 8)
        {
            minRarity = ItemRarity.Uncommon;
            maxRarity = ItemRarity.Rare;
        }
        else
        {
            minRarity = ItemRarity.Common;
            maxRarity = ItemRarity.Uncommon;
        }

        return SelectItemByRarity(config, minRarity, maxRarity);
    }

    /// <summary>
    /// Selects an item within the given rarity range using weighted random.
    /// </summary>
    private (string id, ItemData data) SelectItemByRarity(
        ItemSpawnConfig config,
        ItemRarity minRarity,
        ItemRarity maxRarity)
    {
        var candidates = new List<(string id, ItemData data, int weight)>();

        // Add items from each rarity tier within range
        if (minRarity <= ItemRarity.Common && maxRarity >= ItemRarity.Common)
        {
            AddItemsFromPool(candidates, config.CommonItems, config.CommonWeight);
        }
        if (minRarity <= ItemRarity.Uncommon && maxRarity >= ItemRarity.Uncommon)
        {
            AddItemsFromPool(candidates, config.UncommonItems, config.UncommonWeight);
        }
        if (minRarity <= ItemRarity.Rare && maxRarity >= ItemRarity.Rare)
        {
            AddItemsFromPool(candidates, config.RareItems, config.RareWeight);
        }
        if (minRarity <= ItemRarity.Epic && maxRarity >= ItemRarity.Epic)
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
        }
    }

    /// <summary>
    /// Finds a suitable position for treasure in a region.
    /// Prefers corners and edges (alcove-like positions).
    /// </summary>
    private GridPosition? FindTreasurePosition(Region region, HashSet<Vector2I> occupiedPositions)
    {
        if (region?.Tiles == null || region.Tiles.Count == 0)
            return null;

        // Prefer edge tiles (more likely to be alcoves/corners)
        var edgeTiles = region.EdgeTiles?
            .Select(t => new Vector2I(t.X, t.Y))
            .Where(t => !occupiedPositions.Contains(t))
            .ToList();

        if (edgeTiles != null && edgeTiles.Count > 0)
        {
            var selected = edgeTiles[_rng.RandiRange(0, edgeTiles.Count - 1)];
            return new GridPosition(selected.X, selected.Y);
        }

        // Fallback to any available tile
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
