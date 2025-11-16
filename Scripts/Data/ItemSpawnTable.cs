using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Serializable item spawn table structure.
/// Loaded from Data/SpawnTables/*_items.yaml files.
/// </summary>
public class ItemSpawnTable
{
    public string Name { get; set; } = string.Empty;

    public List<ItemSpawnTableEntry> Entries { get; set; } = new();
}

/// <summary>
/// Individual item spawn table entry with item ID and spawn parameters.
/// </summary>
public class ItemSpawnTableEntry
{
    public string ItemId { get; set; } = string.Empty;

    public int Weight { get; set; } = 1;

    public int MinCount { get; set; } = 1;

    public int MaxCount { get; set; } = 1;
}
