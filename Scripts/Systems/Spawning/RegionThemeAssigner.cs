using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Data;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Assigns faction themes to regions based on floor configuration.
/// Supports territory clustering where adjacent regions can share themes.
/// </summary>
public class RegionThemeAssigner
{
    private readonly DataLoader _dataLoader;
    private readonly RandomNumberGenerator _rng;

    public RegionThemeAssigner(DataLoader dataLoader)
    {
        _dataLoader = dataLoader;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Assigns themes to all regions in the dungeon metadata.
    /// </summary>
    /// <param name="metadata">Dungeon metadata with regions</param>
    /// <param name="config">Floor spawn configuration with theme weights</param>
    /// <param name="regionSpawnData">Dictionary to populate with spawn data per region</param>
    public void AssignThemes(
        DungeonMetadata metadata,
        FloorSpawnConfig config,
        Dictionary<int, RegionSpawnData> regionSpawnData)
    {
        if (metadata.Regions == null || metadata.Regions.Count == 0)
        {
            GD.PushWarning("RegionThemeAssigner: No regions to assign themes to");
            return;
        }

        // Build adjacency map from passages
        var adjacencyMap = BuildAdjacencyMap(metadata);

        // Get available themes for this floor
        var availableThemes = GetAvailableThemes(config);
        if (availableThemes.Count == 0)
        {
            GD.PushWarning("RegionThemeAssigner: No themes available for this floor");
            return;
        }

        // Initialize spawn data for each region
        foreach (var region in metadata.Regions)
        {
            var spawnData = new RegionSpawnData();

            if (adjacencyMap.TryGetValue(region.Id, out var adjacentIds))
            {
                spawnData.AdjacentRegionIds = adjacentIds;
            }

            regionSpawnData[region.Id] = spawnData;
        }

        // Process regions, checking for prefab overrides first
        foreach (var region in metadata.Regions)
        {
            var spawnData = regionSpawnData[region.Id];

            // Check for theme override from prefab SpawnHints
            var themeOverride = GetThemeOverrideFromHints(region, availableThemes);
            if (themeOverride != null)
            {
                spawnData.Theme = themeOverride;
                spawnData.ThemeOverridden = true;
                continue;
            }

            // Try to cluster with adjacent regions that already have themes
            var clusteredTheme = TryClusterWithAdjacent(region.Id, regionSpawnData, availableThemes);
            if (clusteredTheme != null)
            {
                spawnData.Theme = clusteredTheme;
                continue;
            }

            // Select theme based on weighted random from config
            spawnData.Theme = SelectWeightedTheme(config, availableThemes);
        }
    }

    /// <summary>
    /// Builds a map of region adjacencies from passages and direct tile adjacency.
    /// </summary>
    private Dictionary<int, List<int>> BuildAdjacencyMap(DungeonMetadata metadata)
    {
        var adjacencyMap = new Dictionary<int, List<int>>();

        // Initialize empty lists for all regions
        foreach (var region in metadata.Regions)
        {
            adjacencyMap[region.Id] = new List<int>();
        }

        // Build adjacency from passages (passages connect regions)
        if (metadata.Passages != null)
        {
            foreach (var passage in metadata.Passages)
            {
                // A passage's connected regions indicate adjacency
                // We need to find which regions the passage connects
                // For now, use spatial proximity - regions sharing passage tiles
                var connectedRegions = FindRegionsConnectedByPassage(passage, metadata);

                for (int i = 0; i < connectedRegions.Count; i++)
                {
                    for (int j = i + 1; j < connectedRegions.Count; j++)
                    {
                        int r1 = connectedRegions[i];
                        int r2 = connectedRegions[j];

                        if (!adjacencyMap[r1].Contains(r2))
                            adjacencyMap[r1].Add(r2);
                        if (!adjacencyMap[r2].Contains(r1))
                            adjacencyMap[r2].Add(r1);
                    }
                }
            }
        }

        // Also detect direct tile adjacency (for wide openings without passages)
        if (metadata.RegionIds != null)
        {
            var regionIds = metadata.RegionIds;
            int width = regionIds.GetLength(0);
            int height = regionIds.GetLength(1);

            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    int r1 = regionIds[x, y];
                    if (r1 < 0) continue;

                    // Check right neighbor
                    int r2 = regionIds[x + 1, y];
                    if (r2 >= 0 && r1 != r2)
                    {
                        if (!adjacencyMap[r1].Contains(r2))
                            adjacencyMap[r1].Add(r2);
                        if (!adjacencyMap[r2].Contains(r1))
                            adjacencyMap[r2].Add(r1);
                    }

                    // Check bottom neighbor
                    r2 = regionIds[x, y + 1];
                    if (r2 >= 0 && r1 != r2)
                    {
                        if (!adjacencyMap[r1].Contains(r2))
                            adjacencyMap[r1].Add(r2);
                        if (!adjacencyMap[r2].Contains(r1))
                            adjacencyMap[r2].Add(r1);
                    }
                }
            }
        }

        return adjacencyMap;
    }

    /// <summary>
    /// Finds regions connected by a passage.
    /// </summary>
    private List<int> FindRegionsConnectedByPassage(Passage passage, DungeonMetadata metadata)
    {
        var connectedRegions = new HashSet<int>();

        // Check tiles adjacent to passage endpoints and along passage
        foreach (var tile in passage.Tiles)
        {
            // Check all 8 neighbors
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var neighborPos = new Core.GridPosition(tile.X + dx, tile.Y + dy);
                    var region = metadata.GetRegionAt(neighborPos);
                    if (region != null)
                    {
                        connectedRegions.Add(region.Id);
                    }
                }
            }
        }

        return connectedRegions.ToList();
    }

    /// <summary>
    /// Gets themes available for the current floor depth.
    /// </summary>
    private List<FactionTheme> GetAvailableThemes(FloorSpawnConfig config)
    {
        var themes = new List<FactionTheme>();

        foreach (var entry in config.ThemeWeights)
        {
            var theme = _dataLoader.GetFactionTheme(entry.Id);
            if (theme != null)
            {
                themes.Add(theme);
            }
            else
            {
                GD.PushWarning($"[RegionThemeAssigner] Theme '{entry.Id}' not found in data loader");
            }
        }

        // If no themes configured, try to get all themes valid for this floor range
        if (themes.Count == 0)
        {
            foreach (var theme in _dataLoader.GetAllFactionThemes())
            {
                if (config.MinFloor >= theme.MinFloor && config.MaxFloor <= theme.MaxFloor)
                {
                    themes.Add(theme);
                }
            }
        }

        return themes;
    }

    /// <summary>
    /// Checks if a region has a theme override from prefab SpawnHints.
    /// </summary>
    private FactionTheme GetThemeOverrideFromHints(Region region, List<FactionTheme> availableThemes)
    {
        if (region.SpawnHints == null || region.SpawnHints.Count == 0)
            return null;

        foreach (var hint in region.SpawnHints)
        {
            // Check if hint specifies a theme
            if (!string.IsNullOrEmpty(hint.ThemeId))
            {
                var theme = _dataLoader.GetFactionTheme(hint.ThemeId);
                if (theme != null)
                    return theme;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to cluster this region with an adjacent region's theme.
    /// </summary>
    private FactionTheme TryClusterWithAdjacent(
        int regionId,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        List<FactionTheme> availableThemes)
    {
        var spawnData = regionSpawnData[regionId];

        // 40% chance to cluster with an adjacent region
        if (_rng.Randf() > 0.4f)
            return null;

        // Find adjacent regions that already have themes assigned
        var themedAdjacent = spawnData.AdjacentRegionIds
            .Where(id => regionSpawnData.ContainsKey(id) && regionSpawnData[id].Theme != null)
            .ToList();

        if (themedAdjacent.Count == 0)
            return null;

        // Pick a random adjacent region's theme
        int adjacentId = themedAdjacent[_rng.RandiRange(0, themedAdjacent.Count - 1)];
        var adjacentTheme = regionSpawnData[adjacentId].Theme;

        // Only cluster if the theme is in our available list
        if (availableThemes.Contains(adjacentTheme))
            return adjacentTheme;

        return null;
    }

    /// <summary>
    /// Selects a theme based on weighted random from config.
    /// </summary>
    private FactionTheme SelectWeightedTheme(FloorSpawnConfig config, List<FactionTheme> availableThemes)
    {
        if (availableThemes.Count == 0)
            return null;

        // Build weighted selection
        int totalWeight = 0;
        var weightedThemes = new List<(FactionTheme theme, int weight)>();

        foreach (var entry in config.ThemeWeights)
        {
            var theme = availableThemes.FirstOrDefault(t => t.Id == entry.Id);
            if (theme != null)
            {
                weightedThemes.Add((theme, entry.Weight));
                totalWeight += entry.Weight;
            }
        }

        // If no weighted themes, pick randomly from available
        if (weightedThemes.Count == 0 || totalWeight == 0)
        {
            return availableThemes[_rng.RandiRange(0, availableThemes.Count - 1)];
        }

        // Weighted random selection
        int roll = _rng.RandiRange(0, totalWeight - 1);
        int cumulative = 0;

        foreach (var (theme, weight) in weightedThemes)
        {
            cumulative += weight;
            if (roll < cumulative)
                return theme;
        }

        // Fallback
        return weightedThemes[0].theme;
    }
}
