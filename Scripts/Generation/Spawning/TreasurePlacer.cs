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
/// Higher intro_floor items are placed with tougher guardians.
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
    /// Places a guarded treasure in a region using intro floor-based selection.
    /// Higher threat guardians guard higher intro_floor items.
    /// </summary>
    /// <param name="region">Region to place treasure in</param>
    /// <param name="config">Floor spawn config</param>
    /// <param name="guardianThreat">Threat level of guardians in this region</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>Tuple of (items placed, intro floor)</returns>
    public (int count, int value) PlaceGuardedTreasure(
        Region region,
        FloorSpawnConfig config,
        int guardianThreat,
        HashSet<Vector2I> occupiedPositions)
    {
        // Select item based on guardian strength (stronger guardian = higher intro_floor item)
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

        GD.Print($"[TreasurePlacer] Placed guarded treasure '{itemId}' (intro floor {itemData.IntroFloor}) with guardian threat {guardianThreat}");
        return (1, itemData.IntroFloor);
    }

    /// <summary>
    /// Places an unguarded treasure in a low-danger region.
    /// Selects low intro_floor items appropriate for the current floor.
    /// </summary>
    public int PlaceUnguardedTreasure(
        Region region,
        FloorSpawnConfig config,
        HashSet<Vector2I> occupiedPositions)
    {
        // Select a lower intro_floor item for unguarded placement
        // Only items at or below current floor (no out-of-depth for unguarded)
        int currentFloor = config.Floor;

        var (itemId, itemData) = SelectItemByIntroFloor(currentFloor, minFloor: 1, maxFloor: currentFloor);
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
    /// Higher threat guardians guard higher intro_floor items.
    /// </summary>
    private (string id, ItemData data) SelectItemForGuardian(FloorSpawnConfig config, int guardianThreat)
    {
        int currentFloor = config.Floor;

        // Map guardian threat to intro_floor range
        // Higher threat = bias towards higher intro_floor items
        int minFloor = 1;
        int maxFloor = currentFloor;

        if (guardianThreat >= 16)
        {
            // Very dangerous - prefer high intro_floor items
            minFloor = Mathf.Max(1, currentFloor - 1);
            maxFloor = currentFloor + 2; // Allow some out-of-depth
        }
        else if (guardianThreat >= 8)
        {
            // Moderately dangerous - mid-to-high intro_floor items
            minFloor = Mathf.Max(1, currentFloor / 2);
            maxFloor = currentFloor + 1;
        }
        // else: lower danger regions get full intro_floor range

        return SelectItemByIntroFloor(currentFloor, minFloor, maxFloor);
    }

    /// <summary>
    /// Selects an item within the given intro_floor range.
    /// Uses decay-based weighting for selection.
    /// </summary>
    private (string id, ItemData data) SelectItemByIntroFloor(int currentFloor, int minFloor, int maxFloor)
    {
        var candidates = new List<(string id, ItemData data, float weight)>();

        foreach (var itemData in _dataLoader.Items.GetAll())
        {
            // Skip items marked as not auto-spawning
            if (itemData.NoAutoSpawn)
                continue;

            // Check intro_floor range
            if (itemData.IntroFloor >= minFloor && itemData.IntroFloor <= maxFloor)
            {
                // Calculate decay-based weight
                int floorsAboveIntro = currentFloor - itemData.IntroFloor;
                float decay = itemData.GetRelevanceDecay();
                float decayFactor = 1.0f / (1.0f + floorsAboveIntro * decay);
                float weight = Mathf.Max(0.01f, decayFactor * itemData.GetSpawnRarity());

                candidates.Add((itemData.DataFileId, itemData, weight));
            }
        }

        if (candidates.Count == 0)
            return (null, null);

        // Weighted random selection
        float totalWeight = candidates.Sum(c => c.weight);
        if (totalWeight <= 0)
        {
            var random = candidates[_rng.RandiRange(0, candidates.Count - 1)];
            return (random.id, random.data);
        }

        float roll = _rng.Randf() * totalWeight;
        float cumulative = 0;

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
