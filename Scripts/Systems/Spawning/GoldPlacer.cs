using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Places gold piles throughout the dungeon floor.
/// Distributes gold scaled by floor depth with some near guardians and some scattered.
/// </summary>
public class GoldPlacer
{
    private readonly EntityManager _entityManager;
    private readonly RandomNumberGenerator _rng;

    // Gold pile scene for instantiation
    private readonly PackedScene _goldScene;

    public GoldPlacer(EntityManager entityManager)
    {
        _entityManager = entityManager;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();

        // Load the Gold scene
        _goldScene = GD.Load<PackedScene>("res://Scenes/Entities/Gold.tscn");
    }

    /// <summary>
    /// Distributes gold across the dungeon floor.
    /// </summary>
    /// <param name="regions">All regions on the floor</param>
    /// <param name="regionSpawnData">Spawn data per region</param>
    /// <param name="goldBudget">Total gold to place</param>
    /// <param name="floorDepth">Current floor depth (affects pile sizes)</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>Total gold placed</returns>
    public int DistributeGold(
        List<Region> regions,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        int goldBudget,
        int floorDepth,
        HashSet<Vector2I> occupiedPositions)
    {
        if (regions == null || regions.Count == 0 || goldBudget <= 0)
            return 0;

        int goldPlaced = 0;

        // Calculate number of piles based on budget
        int minPileSize = 1 + floorDepth;
        int maxPileSize = 5 + (floorDepth * 2);
        int avgPileSize = (minPileSize + maxPileSize) / 2;
        int targetPiles = Mathf.Max(1, goldBudget / avgPileSize);

        // Place some gold in dangerous regions (guarded treasure)
        int guardedPiles = targetPiles / 3;
        goldPlaced += PlaceGuardedGold(regions, regionSpawnData, guardedPiles,
            minPileSize, maxPileSize, occupiedPositions);

        // Place remaining gold scattered throughout
        int scatteredPiles = targetPiles - guardedPiles;
        int remainingBudget = goldBudget - goldPlaced;
        goldPlaced += PlaceScatteredGold(regions, scatteredPiles, remainingBudget,
            minPileSize, maxPileSize, occupiedPositions);

        return goldPlaced;
    }

    /// <summary>
    /// Places gold piles in dangerous regions (near guardians).
    /// </summary>
    private int PlaceGuardedGold(
        List<Region> regions,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        int pileCount,
        int minPile,
        int maxPile,
        HashSet<Vector2I> occupiedPositions)
    {
        // Sort regions by danger (most dangerous first)
        var dangerousRegions = regions
            .Where(r => regionSpawnData.ContainsKey(r.Id) && regionSpawnData[r.Id].TotalThreatSpawned > 0)
            .OrderByDescending(r => regionSpawnData[r.Id].TotalThreatSpawned)
            .ToList();

        int goldPlaced = 0;
        int regionCount = dangerousRegions.Count;

        for (int i = 0; i < pileCount && i < regionCount; i++)
        {
            var region = dangerousRegions[i];

            var position = FindGoldPosition(region, occupiedPositions);
            if (position == null)
                continue;

            // Larger piles in more dangerous areas
            int dangerBonus = regionSpawnData[region.Id].TotalThreatSpawned / 5;
            int amount = _rng.RandiRange(minPile + dangerBonus, maxPile + dangerBonus);

            var goldPile = CreateGoldPile(amount, position.Value);
            if (goldPile != null)
            {
                _entityManager.AddEntity(goldPile);
                occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
                goldPlaced += amount;
            }
        }

        return goldPlaced;
    }

    /// <summary>
    /// Places gold piles scattered throughout all regions.
    /// </summary>
    private int PlaceScatteredGold(
        List<Region> regions,
        int pileCount,
        int maxGold,
        int minPile,
        int maxPile,
        HashSet<Vector2I> occupiedPositions)
    {
        int goldPlaced = 0;

        for (int i = 0; i < pileCount && goldPlaced < maxGold && regions.Count > 0; i++)
        {
            var region = regions[_rng.RandiRange(0, regions.Count - 1)];

            var position = FindGoldPosition(region, occupiedPositions);
            if (position == null)
                continue;

            int amount = _rng.RandiRange(minPile, maxPile);
            amount = Mathf.Min(amount, maxGold - goldPlaced);

            if (amount <= 0)
                break;

            var goldPile = CreateGoldPile(amount, position.Value);
            if (goldPile != null)
            {
                _entityManager.AddEntity(goldPile);
                occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
                goldPlaced += amount;
            }
        }

        return goldPlaced;
    }

    /// <summary>
    /// Places a single gold pile at a specific position.
    /// </summary>
    public Gold PlaceGoldPile(int amount, GridPosition position, HashSet<Vector2I> occupiedPositions)
    {
        if (occupiedPositions.Contains(new Vector2I(position.X, position.Y)))
            return null;

        var goldPile = CreateGoldPile(amount, position);
        if (goldPile != null)
        {
            _entityManager.AddEntity(goldPile);
            occupiedPositions.Add(new Vector2I(position.X, position.Y));
        }

        return goldPile;
    }

    /// <summary>
    /// Creates a gold pile entity.
    /// </summary>
    private Gold CreateGoldPile(int amount, GridPosition position)
    {
        if (_goldScene == null)
        {
            GD.PushWarning("GoldPlacer: Gold scene not loaded");
            return null;
        }

        var goldPile = _goldScene.Instantiate<Gold>();
        goldPile.Initialize(amount, position);
        return goldPile;
    }

    /// <summary>
    /// Finds a position for gold in a region.
    /// </summary>
    private GridPosition? FindGoldPosition(Region region, HashSet<Vector2I> occupiedPositions)
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
