using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;

namespace PitsOfDespair.Generation.Prefabs;

/// <summary>
/// Data class representing a dungeon prefab loaded from YAML.
/// Prefabs are pre-designed room layouts that can be stamped into the dungeon.
/// </summary>
public class PrefabData
{
    /// <summary>
    /// Unique identifier (filename without extension).
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Prefab dimensions.
    /// </summary>
    public PrefabDimensions Dimensions { get; set; }

    /// <summary>
    /// ASCII tile layout (# = wall, . = floor, other chars defined in legend).
    /// </summary>
    public List<string> Tiles { get; set; } = new();

    /// <summary>
    /// Legend mapping characters to tile types and spawn points.
    /// </summary>
    public Dictionary<string, TileLegendEntry> Legend { get; set; } = new();

    /// <summary>
    /// Rules for when and where this prefab can be placed.
    /// </summary>
    public PrefabPlacementRules Placement { get; set; } = new();

    /// <summary>
    /// Spawn hints attached to this prefab, keyed by tag.
    /// </summary>
    public Dictionary<string, SpawnHintConfig> SpawnHints { get; set; } = new();

    /// <summary>
    /// Get the tile type at a position in the prefab.
    /// </summary>
    public TileType GetTileAt(int x, int y)
    {
        if (y < 0 || y >= Tiles.Count) return TileType.Wall;
        var row = Tiles[y];
        if (x < 0 || x >= row.Length) return TileType.Wall;

        char c = row[x];
        return CharToTileType(c);
    }

    /// <summary>
    /// Get spawn point tag at position, or null if not a spawn point.
    /// </summary>
    public string GetSpawnTagAt(int x, int y)
    {
        if (y < 0 || y >= Tiles.Count) return null;
        var row = Tiles[y];
        if (x < 0 || x >= row.Length) return null;

        char c = row[x];
        string key = c.ToString();

        if (Legend.TryGetValue(key, out var entry) && entry.Type == "spawn_point")
        {
            return entry.Tag;
        }
        return null;
    }

    /// <summary>
    /// Convert ASCII character to TileType.
    /// </summary>
    private TileType CharToTileType(char c)
    {
        // Check legend first
        string key = c.ToString();
        if (Legend.TryGetValue(key, out var entry))
        {
            // Spawn points are floor tiles
            if (entry.Type == "spawn_point") return TileType.Floor;
            if (entry.Type == "wall") return TileType.Wall;
            if (entry.Type == "floor") return TileType.Floor;
        }

        // Default mappings
        return c switch
        {
            '#' => TileType.Wall,
            '.' => TileType.Floor,
            ' ' => TileType.Wall,
            _ => TileType.Floor // Unknown chars default to floor
        };
    }

    /// <summary>
    /// Get the actual width from tile data.
    /// </summary>
    public int GetWidth()
    {
        if (Dimensions?.Width > 0) return Dimensions.Width;
        if (Tiles.Count == 0) return 0;
        int maxWidth = 0;
        foreach (var row in Tiles)
        {
            if (row.Length > maxWidth) maxWidth = row.Length;
        }
        return maxWidth;
    }

    /// <summary>
    /// Get the actual height from tile data.
    /// </summary>
    public int GetHeight()
    {
        if (Dimensions?.Height > 0) return Dimensions.Height;
        return Tiles.Count;
    }

    /// <summary>
    /// Collect all spawn hints with their positions from the prefab layout.
    /// </summary>
    public List<SpawnHint> CollectSpawnHints()
    {
        var hints = new List<SpawnHint>();
        int height = GetHeight();
        int width = GetWidth();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                string tag = GetSpawnTagAt(x, y);
                if (tag != null && SpawnHints.TryGetValue(tag, out var config))
                {
                    hints.Add(new SpawnHint
                    {
                        Tag = tag,
                        SpawnType = config.SpawnType ?? "single",
                        CreaturePool = config.CreaturePool,
                        ItemId = config.ItemId,
                        ItemPool = config.ItemPool,
                        Placement = config.Placement ?? "random",
                        Position = new GridPosition(x, y), // Relative to prefab origin
                        Count = config.Count ?? "1"
                    });
                }
            }
        }

        return hints;
    }
}

/// <summary>
/// Prefab dimensions.
/// </summary>
public class PrefabDimensions
{
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Entry in the prefab legend mapping characters to meanings.
/// </summary>
public class TileLegendEntry
{
    /// <summary>
    /// Type of this tile (wall, floor, spawn_point).
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Tag for spawn points (e.g., "guardian", "treasure").
    /// </summary>
    public string Tag { get; set; }
}

/// <summary>
/// Rules controlling when and where a prefab can be placed.
/// </summary>
public class PrefabPlacementRules
{
    /// <summary>
    /// Minimum floor depth where this prefab can appear.
    /// </summary>
    public int MinFloor { get; set; } = 1;

    /// <summary>
    /// Maximum floor depth where this prefab can appear.
    /// </summary>
    public int MaxFloor { get; set; } = 99;

    /// <summary>
    /// Spawn frequency (common, uncommon, rare, unique).
    /// </summary>
    public string Frequency { get; set; } = "common";

    /// <summary>
    /// Minimum region size required to place this prefab.
    /// </summary>
    public int MinRegionSize { get; set; } = 0;

    /// <summary>
    /// Tags required on target region.
    /// </summary>
    public List<string> RequiredTags { get; set; } = new();

    /// <summary>
    /// Tags that prevent placement on a region.
    /// </summary>
    public List<string> ExcludedTags { get; set; } = new();
}

/// <summary>
/// Configuration for a spawn hint in a prefab.
/// </summary>
public class SpawnHintConfig
{
    /// <summary>
    /// Spawn type (single, band, unique).
    /// </summary>
    public string SpawnType { get; set; }

    /// <summary>
    /// Pool of creature IDs to select from.
    /// </summary>
    public List<string> CreaturePool { get; set; }

    /// <summary>
    /// Specific item ID to spawn.
    /// </summary>
    public string ItemId { get; set; }

    /// <summary>
    /// Item pool ID to select from.
    /// </summary>
    public string ItemPool { get; set; }

    /// <summary>
    /// Placement strategy (center, random, surrounding).
    /// </summary>
    public string Placement { get; set; }

    /// <summary>
    /// Count expression (dice notation or fixed number).
    /// </summary>
    public string Count { get; set; }
}
