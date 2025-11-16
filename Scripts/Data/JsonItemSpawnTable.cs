using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PitsOfDespair.Data;

/// <summary>
/// JSON-serializable item spawn table structure.
/// Loaded from Data/SpawnTables/*_items.json files.
/// </summary>
public class JsonItemSpawnTable
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<JsonItemSpawnTableEntry> Entries { get; set; } = new();
}

/// <summary>
/// Individual item spawn table entry with item ID and spawn parameters.
/// </summary>
public class JsonItemSpawnTableEntry
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 1;

    [JsonPropertyName("minCount")]
    public int MinCount { get; set; } = 1;

    [JsonPropertyName("maxCount")]
    public int MaxCount { get; set; } = 1;
}
