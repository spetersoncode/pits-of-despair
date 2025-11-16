using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Serializable creature spawn table structure.
/// Loaded from Data/SpawnTables/*_creatures.yaml files.
/// </summary>
public class CreatureSpawnTable
{
    public string Name { get; set; } = string.Empty;

    public List<CreatureSpawnTableEntry> Entries { get; set; } = new();
}

/// <summary>
/// Individual creature spawn table entry with creature ID and spawn parameters.
/// </summary>
public class CreatureSpawnTableEntry
{
    public string CreatureId { get; set; } = string.Empty;

    public int Weight { get; set; } = 1;

    public int MinCount { get; set; } = 1;

    public int MaxCount { get; set; } = 1;
}
