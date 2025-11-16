using System;
using Godot;
using YamlDotNet.Serialization;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Represents a range with minimum and maximum values for spawn counts.
/// </summary>
public class CountRange
{
    [YamlMember(Alias = "min")]
    public int Min { get; set; } = 1;

    [YamlMember(Alias = "max")]
    public int Max { get; set; } = 1;

    public int GetRandom()
    {
        return GD.RandRange(Min, Max);
    }
}

/// <summary>
/// Types of spawn entries that can appear in a spawn pool.
/// </summary>
public enum SpawnEntryType
{
    Single,  // Single creature type
    Band,    // Monster band/pack
    Unique   // Unique/boss creature
}

/// <summary>
/// Represents a single entry in a spawn pool that can be selected for spawning.
/// </summary>
public class SpawnEntryData
{
    /// <summary>
    /// Type of spawn entry (single, band, or unique).
    /// </summary>
    [YamlMember(Alias = "type")]
    public string TypeString { get; set; } = "single";

    [YamlIgnore]
    public SpawnEntryType Type => TypeString.ToLower() switch
    {
        "single" => SpawnEntryType.Single,
        "band" => SpawnEntryType.Band,
        "unique" => SpawnEntryType.Unique,
        _ => SpawnEntryType.Single
    };

    /// <summary>
    /// ID of the creature to spawn (for single/unique types).
    /// </summary>
    [YamlMember(Alias = "creatureId")]
    public string CreatureId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the item to spawn (for item entries).
    /// </summary>
    [YamlMember(Alias = "itemId")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the band to spawn (for band type).
    /// </summary>
    [YamlMember(Alias = "bandId")]
    public string BandId { get; set; } = string.Empty;

    /// <summary>
    /// Weight for weighted random selection within the pool.
    /// </summary>
    [YamlMember(Alias = "weight")]
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Number of entities to spawn (for single/unique types).
    /// </summary>
    [YamlMember(Alias = "count")]
    public CountRange Count { get; set; } = new CountRange();

    /// <summary>
    /// Placement strategy to use (random, center, surrounding, formation).
    /// </summary>
    [YamlMember(Alias = "placement")]
    public string Placement { get; set; } = "random";

    /// <summary>
    /// Minimum NxN area size required for spawning (0 = auto-calculate based on type).
    /// </summary>
    [YamlMember(Alias = "minSpace")]
    public int MinimumSpace { get; set; } = 0;

    /// <summary>
    /// Minimum distance (Manhattan) from other spawns (0 = no spacing requirement).
    /// </summary>
    [YamlMember(Alias = "minIsolation")]
    public int MinimumIsolation { get; set; } = 0;

    /// <summary>
    /// Validates that the entry has required fields based on type.
    /// </summary>
    public bool IsValid()
    {
        return Type switch
        {
            SpawnEntryType.Single => !string.IsNullOrEmpty(CreatureId) || !string.IsNullOrEmpty(ItemId),
            SpawnEntryType.Band => !string.IsNullOrEmpty(BandId),
            SpawnEntryType.Unique => !string.IsNullOrEmpty(CreatureId),
            _ => false
        };
    }

    public override string ToString()
    {
        return Type switch
        {
            SpawnEntryType.Single when !string.IsNullOrEmpty(CreatureId) => $"Single: {CreatureId} x{Count.Min}-{Count.Max}",
            SpawnEntryType.Single when !string.IsNullOrEmpty(ItemId) => $"Item: {ItemId} x{Count.Min}-{Count.Max}",
            SpawnEntryType.Band => $"Band: {BandId}",
            SpawnEntryType.Unique => $"Unique: {CreatureId}",
            _ => "Invalid Entry"
        };
    }
}
