using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PitsOfDespair.Data;

/// <summary>
/// JSON-serializable spawn table structure.
/// Loaded from Data/SpawnTables/*.json files.
/// </summary>
public class JsonSpawnTable
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<JsonSpawnTableEntry> Entries { get; set; } = new();
}

/// <summary>
/// Individual spawn table entry with creature ID and spawn parameters.
/// </summary>
public class JsonSpawnTableEntry
{
    [JsonPropertyName("creatureId")]
    public string CreatureId { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 1;

    [JsonPropertyName("minCount")]
    public int MinCount { get; set; } = 1;

    [JsonPropertyName("maxCount")]
    public int MaxCount { get; set; } = 1;
}
