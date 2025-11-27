using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Passes.Config;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;
using PitsOfDespair.Generation.Prefabs;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Generation pass that inserts prefabs into suitable regions.
/// Prefabs are stamped over existing regions, replacing their tiles.
/// </summary>
public class PrefabInsertionPass : IGenerationPass
{
    private readonly PassConfig _passConfig;
    private readonly int _budget;
    private readonly List<PrefabPoolEntry> _pool;

    public string Name => "Prefabs";
    public int Priority { get; }
    public PassRole Role => PassRole.Modifier;

    public PrefabInsertionPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? new PassConfig { Pass = "prefabs", Priority = 200 };
        Priority = _passConfig.Priority;

        _budget = _passConfig.GetConfigValue("budget", 2);

        // Parse pool from config
        _pool = ParsePoolConfig(_passConfig);
    }

    public bool CanExecute(GenerationContext context)
    {
        // Need metadata regions to place prefabs
        return context.Metadata?.Regions != null && context.Metadata.Regions.Count > 0;
    }

    public void Execute(GenerationContext context)
    {
        GD.Print($"[PrefabInsertionPass] Inserting prefabs (budget: {_budget})...");

        // Get prefab library from context or create one
        var library = GetPrefabLibrary(context);
        if (library == null || library.Count == 0)
        {
            GD.Print("[PrefabInsertionPass] No prefabs available, skipping.");
            return;
        }

        // Get floor depth from pass data (set by MapSystem) or default to 1
        int floorDepth = context.GetPassData("FloorDepth", 1);

        // Select prefabs to insert
        List<PrefabData> selectedPrefabs;
        if (_pool != null && _pool.Count > 0)
        {
            selectedPrefabs = library.SelectFromPool(_pool, _budget, floorDepth);
        }
        else
        {
            selectedPrefabs = library.SelectRandom(_budget, floorDepth);
        }

        if (selectedPrefabs.Count == 0)
        {
            GD.Print("[PrefabInsertionPass] No prefabs selected for this floor.");
            return;
        }

        GD.Print($"[PrefabInsertionPass] Selected {selectedPrefabs.Count} prefab(s): {string.Join(", ", selectedPrefabs.Select(p => p.Name))}");

        // Track which regions have been used
        var usedRegions = new HashSet<int>();
        int insertedCount = 0;

        foreach (var prefab in selectedPrefabs)
        {
            var targetRegion = FindSuitableRegion(context, prefab, usedRegions);
            if (targetRegion == null)
            {
                GD.Print($"[PrefabInsertionPass] No suitable region for prefab '{prefab.Name}'");
                continue;
            }

            if (InsertPrefab(context, prefab, targetRegion))
            {
                usedRegions.Add(targetRegion.Id);
                insertedCount++;
                GD.Print($"[PrefabInsertionPass] Inserted '{prefab.Name}' into region {targetRegion.Id}");
            }
        }

        GD.Print($"[PrefabInsertionPass] Inserted {insertedCount} prefab(s).");
    }

    private PrefabLibrary GetPrefabLibrary(GenerationContext context)
    {
        // Check if library is cached in pass data
        if (context.HasPassData("PrefabLibrary"))
        {
            return context.GetPassData<PrefabLibrary>("PrefabLibrary");
        }

        // Try to get from DataLoader (requires autoload access)
        // For now, we'll use a static accessor pattern
        var prefabs = PrefabLoader.GetLoadedPrefabs();
        if (prefabs == null || prefabs.Count == 0)
        {
            return null;
        }

        var library = new PrefabLibrary(prefabs, context.Random);
        context.SetPassData("PrefabLibrary", library);
        return library;
    }

    private Region FindSuitableRegion(GenerationContext context, PrefabData prefab, HashSet<int> usedRegions)
    {
        int prefabWidth = prefab.GetWidth();
        int prefabHeight = prefab.GetHeight();
        int prefabArea = prefabWidth * prefabHeight;

        // Filter regions by placement rules
        var candidates = context.Metadata.Regions
            .Where(r => !usedRegions.Contains(r.Id))
            .Where(r => r.Area >= prefabArea)
            .Where(r => r.BoundingBox.Size.X >= prefabWidth && r.BoundingBox.Size.Y >= prefabHeight)
            .Where(r => r.Area >= prefab.Placement.MinRegionSize)
            .Where(r => MatchesTags(r, prefab.Placement))
            .ToList();

        if (candidates.Count == 0) return null;

        // Prefer regions that fit well (not too much larger than the prefab)
        candidates = candidates
            .OrderBy(r => r.Area - prefabArea) // Closest fit first
            .ThenBy(_ => context.Random.Next()) // Random tiebreaker
            .ToList();

        return candidates.FirstOrDefault();
    }

    private bool MatchesTags(Region region, PrefabPlacementRules rules)
    {
        // Check required tags
        if (rules.RequiredTags != null && rules.RequiredTags.Count > 0)
        {
            if (string.IsNullOrEmpty(region.Tag)) return false;
            if (!rules.RequiredTags.Contains(region.Tag)) return false;
        }

        // Check excluded tags
        if (rules.ExcludedTags != null && rules.ExcludedTags.Count > 0)
        {
            if (!string.IsNullOrEmpty(region.Tag) && rules.ExcludedTags.Contains(region.Tag))
                return false;
        }

        return true;
    }

    private bool InsertPrefab(GenerationContext context, PrefabData prefab, Region targetRegion)
    {
        int prefabWidth = prefab.GetWidth();
        int prefabHeight = prefab.GetHeight();

        // Calculate placement position (centered in region bounding box)
        int startX = targetRegion.BoundingBox.Position.X +
                     (targetRegion.BoundingBox.Size.X - prefabWidth) / 2;
        int startY = targetRegion.BoundingBox.Position.Y +
                     (targetRegion.BoundingBox.Size.Y - prefabHeight) / 2;

        // Ensure within bounds
        startX = Math.Max(1, Math.Min(startX, context.Width - prefabWidth - 1));
        startY = Math.Max(1, Math.Min(startY, context.Height - prefabHeight - 1));

        // Stamp prefab tiles
        for (int py = 0; py < prefabHeight; py++)
        {
            for (int px = 0; px < prefabWidth; px++)
            {
                int worldX = startX + px;
                int worldY = startY + py;

                if (context.IsInBounds(worldX, worldY))
                {
                    var tileType = prefab.GetTileAt(px, py);
                    context.SetTile(worldX, worldY, tileType);
                }
            }
        }

        // Update region metadata
        targetRegion.Source = RegionSource.Prefab;
        targetRegion.Tag = prefab.Name;

        // Collect and attach spawn hints with world positions
        var hints = prefab.CollectSpawnHints();
        foreach (var hint in hints)
        {
            // Convert prefab-relative position to world position
            if (hint.Position.HasValue)
            {
                hint.Position = new GridPosition(
                    startX + hint.Position.Value.X,
                    startY + hint.Position.Value.Y
                );
            }
        }
        targetRegion.SpawnHints.AddRange(hints);

        return true;
    }

    private List<PrefabPoolEntry> ParsePoolConfig(PassConfig config)
    {
        var pool = new List<PrefabPoolEntry>();

        if (!config.Config.TryGetValue("pool", out var poolObj) || poolObj == null)
            return pool;

        // Pool is expected to be a list of dictionaries
        if (poolObj is List<object> poolList)
        {
            foreach (var item in poolList)
            {
                if (item is Dictionary<object, object> dict)
                {
                    var entry = new PrefabPoolEntry();

                    if (dict.TryGetValue("prefabId", out var idObj))
                        entry.PrefabId = idObj?.ToString();

                    if (dict.TryGetValue("weight", out var weightObj) && weightObj != null)
                    {
                        if (int.TryParse(weightObj.ToString(), out int weight))
                            entry.Weight = weight;
                    }

                    if (!string.IsNullOrEmpty(entry.PrefabId))
                        pool.Add(entry);
                }
            }
        }

        return pool;
    }
}

/// <summary>
/// Static accessor for prefabs loaded by DataLoader.
/// This bridges the gap between the generation system and the autoload DataLoader.
/// </summary>
public static class PrefabLoader
{
    private static Dictionary<string, PrefabData> _prefabs = new();

    /// <summary>
    /// Set the loaded prefabs (called by DataLoader).
    /// </summary>
    public static void SetPrefabs(Dictionary<string, PrefabData> prefabs)
    {
        _prefabs = prefabs ?? new Dictionary<string, PrefabData>();
    }

    /// <summary>
    /// Get all loaded prefabs.
    /// </summary>
    public static Dictionary<string, PrefabData> GetLoadedPrefabs()
    {
        return _prefabs;
    }

    /// <summary>
    /// Get a specific prefab by ID.
    /// </summary>
    public static PrefabData GetPrefab(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _prefabs.TryGetValue(id.ToLowerInvariant(), out var prefab);
        return prefab;
    }
}
