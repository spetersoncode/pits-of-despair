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
    /// Places a guarded treasure in a region using value-based selection.
    /// Higher threat guardians guard higher value items.
    /// </summary>
    /// <param name="region">Region to place treasure in</param>
    /// <param name="config">Floor spawn config with value ranges</param>
    /// <param name="guardianThreat">Threat level of guardians in this region</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>Tuple of (items placed, total value)</returns>
    public (int count, int value) PlaceGuardedTreasure(
        Region region,
        FloorSpawnConfig config,
        int guardianThreat,
        HashSet<Vector2I> occupiedPositions)
    {
        // Select item based on guardian strength (stronger guardian = higher value item)
        var (itemId, itemData) = SelectItemForGuardian(config, guardianThreat);
        if (itemData == null)
            return (0, 0);

        // Find position for treasure (prefer center/alcove areas)
        var position = FindTreasurePosition(region, occupiedPositions);
        if (position == null)
            return (0, 0);

        // Create and place the item
        var itemEntity = _entityFactory.CreateItem(itemId, position.Value);
        if (itemEntity == null)
            return (0, 0);

        _entityManager.AddEntity(itemEntity);
        occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));

        GD.Print($"[TreasurePlacer] Placed guarded treasure '{itemId}' (value {itemData.Value}) with guardian threat {guardianThreat}");
        return (1, itemData.Value);
    }

    /// <summary>
    /// Places an unguarded treasure in a low-danger region.
    /// </summary>
    public int PlaceUnguardedTreasure(
        Region region,
        FloorSpawnConfig config,
        HashSet<Vector2I> occupiedPositions)
    {
        // Select a lower-value item for unguarded placement
        int minValue = config.GetMinItemValue();
        int maxValue = minValue + 1; // Only lowest tier items unguarded

        var (itemId, itemData) = SelectItemByValueRange(minValue, maxValue);
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

        return 1;
    }

    /// <summary>
    /// Selects an item appropriate for the guardian's threat level.
    /// Higher threat guardians guard higher value items.
    /// </summary>
    private (string id, ItemData data) SelectItemForGuardian(FloorSpawnConfig config, int guardianThreat)
    {
        int minValue = config.GetMinItemValue();
        int maxValue = config.GetMaxItemValue();

        // Map guardian threat to item value range
        // Higher threat = bias towards higher value items
        int valueRange = maxValue - minValue;

        if (guardianThreat >= 16)
        {
            // Very dangerous - high value items only
            minValue = maxValue - (valueRange / 3);
        }
        else if (guardianThreat >= 8)
        {
            // Moderately dangerous - mid-to-high value items
            minValue = minValue + (valueRange / 3);
        }
        // else: lower danger regions get full value range

        return SelectItemByValueRange(minValue, maxValue);
    }

    /// <summary>
    /// Selects an item within the given value range.
    /// Uses combined weighting: inverse value (lower = more common) + type multiplier.
    /// Items with Value <= 0 bypass floor filtering and are treated as maxValue for weighting (rare).
    /// </summary>
    private (string id, ItemData data) SelectItemByValueRange(int minValue, int maxValue)
    {
        var candidates = new List<(string id, ItemData data, int weight)>();

        foreach (var itemData in _dataLoader.Items.GetAll())
        {
            // Skip items marked as not auto-spawning
            if (itemData.NoAutoSpawn)
                continue;

            // Items with no value (0 or less) bypass floor filtering
            bool isValueless = itemData.Value <= 0;
            bool inValueRange = itemData.Value >= minValue && itemData.Value <= maxValue;

            if (isValueless || inValueRange)
            {
                // Valueless items use maxValue for weighting (rarest tier)
                int effectiveValue = isValueless ? maxValue : itemData.Value;

                // Inverse value weighting: lower value = higher base weight
                int baseWeight = Mathf.Max(1, maxValue - effectiveValue + 1);

                // Apply type-based spawn weight multiplier
                float finalWeight = baseWeight * itemData.GetSpawnWeightMultiplier();
                int weight = Mathf.Max(1, Mathf.RoundToInt(finalWeight));

                candidates.Add((itemData.DataFileId, itemData, weight));
            }
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
