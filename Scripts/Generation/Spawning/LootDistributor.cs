using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Spawning.Data;
using PitsOfDespair.ItemProperties;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Distributes items across the dungeon floor using density-based spawning.
/// Items are filtered by intro floor and weighted by decay formula + spawn rarity.
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
    /// Items are selected based on intro floor eligibility and weighted by decay formula.
    /// </summary>
    /// <param name="regions">Regions to distribute items across</param>
    /// <param name="regionSpawnData">Spawn data per region (for danger levels)</param>
    /// <param name="config">Floor spawn configuration with density parameters</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>Tuple of (items placed, total intro floor sum)</returns>
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

        int currentFloor = config.Floor;

        // Determine max intro floor (with out-of-depth chance)
        int maxIntroFloor = currentFloor;
        bool outOfDepthTriggered = false;
        if (config.CreatureOutOfDepthChance > 0 && _rng.Randf() < config.CreatureOutOfDepthChance)
        {
            maxIntroFloor = currentFloor + config.OutOfDepthFloors;
            outOfDepthTriggered = true;
        }

        // Get eligible items based on intro floor
        var eligibleItems = GetEligibleItems(maxIntroFloor);

        if (eligibleItems.Count == 0)
        {
            GD.PushWarning($"[LootDistributor] No spawnable items found for floor {currentFloor} (maxIntroFloor {maxIntroFloor})");
            return (0, 0);
        }

        if (outOfDepthTriggered)
        {
            int oodCount = eligibleItems.Count(i => i.data.IntroFloor > currentFloor);
            GD.Print($"[LootDistributor] Out-of-depth triggered! {oodCount} items from floors {currentFloor + 1}-{maxIntroFloor} eligible");
        }

        int itemsPlaced = 0;
        int totalIntroFloor = 0;

        // Sort regions by danger (more dangerous = better loot placement preference)
        var sortedRegions = regions
            .Where(r => regionSpawnData.ContainsKey(r.Id))
            .OrderByDescending(r => regionSpawnData[r.Id].DangerLevel)
            .ToList();

        if (sortedRegions.Count == 0)
            sortedRegions = regions.ToList();

        // Distribute items across regions
        int ringsPlaced = 0;
        for (int i = 0; i < targetItems; i++)
        {
            // Cycle through regions, with bias towards dangerous regions
            int regionIndex = i % sortedRegions.Count;
            var region = sortedRegions[regionIndex];

            var position = FindLootPosition(region, occupiedPositions);
            if (position == null)
                continue;

            BaseEntity? itemEntity;

            // Check if this should be a ring instead of a regular item
            if (_rng.Randf() < RingSpawnChance)
            {
                // Generate a ring with a random property
                itemEntity = _entityFactory.CreateRandomRing(currentFloor, position.Value, _rng);
                if (itemEntity != null)
                {
                    ringsPlaced++;
                    _entityManager.AddEntity(itemEntity);
                    occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
                    itemsPlaced++;
                    // Rings use floor 3 as average intro for stats
                    totalIntroFloor += 3;
                    continue;
                }
                // Fall through to regular item if ring creation failed
            }

            // Select item using decay-based weighting
            var (itemId, itemData) = SelectItemWithDecay(eligibleItems, currentFloor);
            if (itemData == null)
                continue;

            itemEntity = _entityFactory.CreateItem(itemId, position.Value);
            if (itemEntity == null)
                continue;

            // Apply random properties based on item type and floor
            if (itemEntity.ItemData != null)
            {
                ApplySpawnProperties(itemEntity.ItemData, currentFloor);
            }

            _entityManager.AddEntity(itemEntity);
            occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
            itemsPlaced++;
            totalIntroFloor += itemData.IntroFloor;
        }

        if (ringsPlaced > 0)
        {
            GD.Print($"[LootDistributor] Generated {ringsPlaced} rings with properties");
        }

        GD.Print($"[LootDistributor] Placed {itemsPlaced}/{targetItems} items (density {config.ItemDensity:P0} of {totalTiles} tiles, floor {currentFloor}, maxIntro {maxIntroFloor})");
        return (itemsPlaced, totalIntroFloor);
    }

    /// <summary>
    /// Gets all items eligible to spawn based on intro floor.
    /// Items are eligible if IntroFloor <= maxIntroFloor and not marked NoAutoSpawn.
    /// </summary>
    private List<(string id, ItemData data)> GetEligibleItems(int maxIntroFloor)
    {
        var result = new List<(string id, ItemData data)>();

        foreach (var itemData in _dataLoader.Items.GetAll())
        {
            // Skip items marked as not auto-spawning
            if (itemData.NoAutoSpawn)
                continue;

            // Check intro floor eligibility
            if (itemData.IntroFloor <= maxIntroFloor)
            {
                result.Add((itemData.DataFileId, itemData));
            }
        }

        return result;
    }

    /// <summary>
    /// Selects an item using decay-based weighting.
    /// Weight = decayFactor * spawnRarity
    /// Where decayFactor = 1.0 / (1.0 + floorsAboveIntro * relevanceDecay)
    /// Out-of-depth items (floorsAboveIntro < 0) get decayFactor > 1.0, making them more likely.
    /// </summary>
    private (string id, ItemData data) SelectItemWithDecay(List<(string id, ItemData data)> items, int currentFloor)
    {
        if (items == null || items.Count == 0)
            return (null, null);

        // Calculate weights using decay formula
        var weighted = items.Select(i => {
            int floorsAboveIntro = currentFloor - i.data.IntroFloor;
            float decay = i.data.GetRelevanceDecay();

            // decayFactor = 1.0 / (1.0 + floorsAboveIntro * decay)
            // When floorsAboveIntro < 0 (out-of-depth), decayFactor > 1.0
            float decayFactor = 1.0f / (1.0f + floorsAboveIntro * decay);

            float finalWeight = decayFactor * i.data.GetSpawnRarity();

            return (
                id: i.id,
                data: i.data,
                weight: Mathf.Max(0.01f, finalWeight)
            );
        }).ToList();

        float totalWeight = weighted.Sum(w => w.weight);
        if (totalWeight <= 0)
        {
            var random = items[_rng.RandiRange(0, items.Count - 1)];
            return (random.id, random.data);
        }

        float roll = _rng.Randf() * totalWeight;
        float cumulative = 0;

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

    #region Property Distribution

    // Property spawn chances by item type
    private const float WeaponPropertyChance = 0.08f;  // 8% for weapons
    private const float ArmorPropertyChance = 0.08f;   // 8% for armor
    private const float AmmoPropertyChance = 0.03f;    // 3% for ammo
    private const float WandPropertyChance = 0.10f;    // 10% for wands
    private const float StaffPropertyChance = 0.15f;   // 15% for staves
    private const float RingPropertyChance = 1.0f;     // 100% for rings (always get property)

    // Chance to spawn a ring instead of a regular item (per item spawn)
    private const float RingSpawnChance = 0.08f;       // 8% of items could be rings

    /// <summary>
    /// Applies random properties to an item based on its type and floor.
    /// </summary>
    private void ApplySpawnProperties(ItemInstance item, int currentFloor)
    {
        if (item == null) return;

        var itemType = ItemPropertyFactory.ParseItemType(item.Template.Type);
        if (itemType == ItemType.None) return;

        float propertyChance = GetPropertyChance(itemType);
        if (propertyChance <= 0f) return;

        // Roll for property
        if (_rng.Randf() > propertyChance) return;

        // Get eligible properties for this item type and floor
        var eligible = ItemPropertyFactory.GetEligibleProperties(currentFloor, itemType);
        if (eligible.Count == 0) return;

        // Select and apply property
        var selected = ItemPropertyFactory.SelectPropertyWithDecay(eligible, currentFloor, _rng);
        if (selected == null) return;

        var property = ItemPropertyFactory.CreateFromMetadata(selected, _rng);
        if (property != null)
        {
            item.AddProperty(property);
        }
    }

    /// <summary>
    /// Gets the property spawn chance for an item type.
    /// </summary>
    private static float GetPropertyChance(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Weapon => WeaponPropertyChance,
            ItemType.Armor => ArmorPropertyChance,
            ItemType.Ammo => AmmoPropertyChance,
            ItemType.Wand => WandPropertyChance,
            ItemType.Staff => StaffPropertyChance,
            ItemType.Ring => RingPropertyChance,
            _ => 0f
        };
    }

    #endregion
}
