using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Spawns decorations throughout dungeon regions.
/// Respects theme assignments and placement hints.
/// IMPORTANT: Must run AFTER AI configuration (patrol routes) to avoid
/// blocking decorations interfering with patrol waypoint selection.
/// </summary>
public class DecorationSpawner
{
    private readonly DataLoader _dataLoader;
    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly MapSystem _mapSystem;
    private readonly RandomNumberGenerator _rng;

    // Configuration
    private const float BaseDecorationDensity = 0.03f;  // 3% of tiles
    private const float ThemeBonus = 0.01f;  // +1% in themed regions
    private const int MinDecorationsPerRegion = 0;
    private const int MaxDecorationsPerRegion = 5;

    public DecorationSpawner(
        DataLoader dataLoader,
        EntityFactory entityFactory,
        EntityManager entityManager,
        MapSystem mapSystem)
    {
        _dataLoader = dataLoader;
        _entityFactory = entityFactory;
        _entityManager = entityManager;
        _mapSystem = mapSystem;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Spawns decorations throughout all regions.
    /// </summary>
    /// <param name="regions">All dungeon regions.</param>
    /// <param name="regionSpawnData">Spawn data per region (includes theme assignment).</param>
    /// <param name="occupiedPositions">Positions already occupied by other entities.</param>
    /// <returns>Total number of decorations spawned.</returns>
    public int SpawnDecorations(
        List<Region> regions,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        HashSet<Vector2I> occupiedPositions)
    {
        int totalPlaced = 0;

        foreach (var region in regions)
        {
            // Get theme for this region (null if generic-only)
            FactionTheme theme = null;
            if (regionSpawnData.TryGetValue(region.Id, out var spawnData))
            {
                theme = spawnData.Theme;
            }

            int placed = PlaceDecorationsInRegion(
                region, theme, occupiedPositions);
            totalPlaced += placed;
        }

        return totalPlaced;
    }

    private int PlaceDecorationsInRegion(
        Region region,
        FactionTheme theme,
        HashSet<Vector2I> occupiedPositions)
    {
        // Calculate target count based on region size
        float density = BaseDecorationDensity;
        if (theme != null) density += ThemeBonus;

        int targetCount = Mathf.RoundToInt(region.Area * density);
        targetCount = Mathf.Clamp(targetCount, MinDecorationsPerRegion, MaxDecorationsPerRegion);

        // Get decoration sets to use
        var genericSet = _dataLoader.GetDecorationSet(null);
        var themedSet = theme != null ? _dataLoader.GetDecorationSet(theme.Id) : null;

        // If no generic set available, skip decoration placement
        if (genericSet == null)
        {
            return 0;
        }

        // Weight: 70% generic, 30% themed (if available)
        float themedChance = themedSet != null ? 0.3f : 0f;

        int placed = 0;
        int attempts = 0;
        int maxAttempts = targetCount * 3;

        while (placed < targetCount && attempts < maxAttempts)
        {
            attempts++;

            // Select decoration set
            bool useThemed = _rng.Randf() < themedChance;
            var set = useThemed && themedSet != null ? themedSet : genericSet;

            // Select decoration from set (weighted)
            var decorationId = SelectWeightedDecoration(set);
            if (decorationId == null) continue;

            var data = _dataLoader.GetDecoration(decorationId);
            if (data == null) continue;

            // Find valid position based on placement hints
            var position = FindDecorationPosition(region, data, occupiedPositions);
            if (position == null) continue;

            // Check path blocking for non-walkable decorations
            if (!data.IsWalkable)
            {
                if (!CanPlaceBlockingDecoration(position.Value, occupiedPositions))
                    continue;
            }

            // Create and place decoration
            var entity = _entityFactory.CreateDecoration(decorationId, position.Value);
            if (entity == null) continue;

            _entityManager.AddEntity(entity);
            occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
            placed++;
        }

        return placed;
    }

    private string SelectWeightedDecoration(DecorationSet set)
    {
        if (set.Decorations == null || set.Decorations.Count == 0)
            return null;

        int totalWeight = set.Decorations.Sum(d => d.Weight);
        if (totalWeight <= 0) return null;

        int roll = _rng.RandiRange(0, totalWeight - 1);
        int cumulative = 0;

        foreach (var entry in set.Decorations)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
                return entry.Id;
        }

        return set.Decorations[0].Id;
    }

    private GridPosition? FindDecorationPosition(
        Region region,
        DecorationData data,
        HashSet<Vector2I> occupiedPositions)
    {
        var candidates = new List<GridPosition>();

        foreach (var tile in region.Tiles)
        {
            var vec = new Vector2I(tile.X, tile.Y);
            if (occupiedPositions.Contains(vec))
                continue;

            // Check placement hints
            bool matches = MatchesPlacementHints(tile, region, data.PlacementHints);
            if (matches)
                candidates.Add(tile);
        }

        if (candidates.Count == 0)
        {
            // Fallback: any available tile
            foreach (var tile in region.Tiles)
            {
                var vec = new Vector2I(tile.X, tile.Y);
                if (!occupiedPositions.Contains(vec))
                    candidates.Add(tile);
            }
        }

        if (candidates.Count == 0)
            return null;

        return candidates[_rng.RandiRange(0, candidates.Count - 1)];
    }

    private bool MatchesPlacementHints(
        GridPosition tile,
        Region region,
        List<string> hints)
    {
        if (hints == null || hints.Count == 0)
            return true;  // No restrictions

        foreach (var hint in hints)
        {
            switch (hint.ToLower())
            {
                case "wall_adjacent":
                    if (IsAdjacentToWall(tile)) return true;
                    break;
                case "corner":
                    if (IsInCorner(tile, region)) return true;
                    break;
                case "center":
                    if (IsNearCenter(tile, region)) return true;
                    break;
                case "scattered":
                    return true;  // No restriction
                case "clustered":
                    return true;  // Handled by placement algorithm
            }
        }

        return false;
    }

    private bool IsAdjacentToWall(GridPosition tile)
    {
        // Check 4-directional neighbors for walls
        var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        foreach (var (dx, dy) in directions)
        {
            var neighbor = new GridPosition(tile.X + dx, tile.Y + dy);
            if (!_mapSystem.IsWalkable(neighbor))
                return true;
        }
        return false;
    }

    private bool IsInCorner(GridPosition tile, Region region)
    {
        // Corner = near region bounding box corners
        var bbox = region.BoundingBox;
        int margin = 2;

        bool nearLeft = tile.X <= bbox.Position.X + margin;
        bool nearRight = tile.X >= bbox.End.X - margin;
        bool nearTop = tile.Y <= bbox.Position.Y + margin;
        bool nearBottom = tile.Y >= bbox.End.Y - margin;

        return (nearLeft || nearRight) && (nearTop || nearBottom);
    }

    private bool IsNearCenter(GridPosition tile, Region region)
    {
        int distSq = (tile.X - region.Centroid.X) * (tile.X - region.Centroid.X) +
                     (tile.Y - region.Centroid.Y) * (tile.Y - region.Centroid.Y);
        int radiusSq = (region.Area / 10) + 4;  // Scale with region size
        return distSq <= radiusSq;
    }

    private bool CanPlaceBlockingDecoration(
        GridPosition position,
        HashSet<Vector2I> occupiedPositions)
    {
        // Simple check: ensure at least 2 adjacent walkable tiles remain
        // This prevents blocking narrow passages
        int walkableNeighbors = 0;
        var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        foreach (var (dx, dy) in directions)
        {
            var neighbor = new GridPosition(position.X + dx, position.Y + dy);
            var vec = new Vector2I(neighbor.X, neighbor.Y);

            if (_mapSystem.IsWalkable(neighbor) &&
                !occupiedPositions.Contains(vec))
            {
                walkableNeighbors++;
            }
        }

        return walkableNeighbors >= 2;
    }
}
