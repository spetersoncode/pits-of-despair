using System;
using Godot;
using YamlDotNet.Serialization;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Represents a dice-based count for spawn amounts.
/// </summary>
public class CountRange
{
    [YamlMember(Alias = "dice")]
    public string DiceNotation { get; set; } = "1";

    public int GetRandom()
    {
        return DiceRoller.Roll(DiceNotation);
    }

    /// <summary>
    /// Gets the maximum possible value from the dice notation.
    /// </summary>
    public int GetMax()
    {
        if (DiceRoller.TryParse(DiceNotation, out int count, out int sides, out int modifier))
        {
            return (count * sides) + modifier;
        }
        return 1; // Default fallback
    }
}

/// <summary>
/// Types of spawn entries that can appear in a spawn pool.
/// </summary>
public enum SpawnEntryType
{
    Single,    // One creature only
    Multiple,  // Multiple of same creature type
    Band,      // Monster band/pack with leader
    Unique     // Unique/boss creature
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
    public SpawnEntryType Type
    {
        get
        {
            // Items are intrinsically single spawn - ignore type field
            if (!string.IsNullOrEmpty(ItemId))
            {
                return SpawnEntryType.Single;
            }

            // For creatures/bands, use specified type
            return TypeString.ToLower() switch
            {
                "single" => SpawnEntryType.Single,
                "multiple" => SpawnEntryType.Multiple,
                "band" => SpawnEntryType.Band,
                "unique" => SpawnEntryType.Unique,
                _ => SpawnEntryType.Single
            };
        }
    }

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
    /// ID of the band to spawn (for band type, external file reference).
    /// </summary>
    [YamlMember(Alias = "bandId")]
    public string BandId { get; set; } = string.Empty;

    /// <summary>
    /// Inline band definition (for band type, alternative to bandId).
    /// </summary>
    [YamlMember(Alias = "band")]
    public BandData Band { get; set; } = null;

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
    /// Quantity for stackable items (ammo, consumables).
    /// If specified, the item will spawn with this quantity.
    /// </summary>
    [YamlMember(Alias = "quantity")]
    public CountRange Quantity { get; set; } = null;

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
        // Items are always valid if they have an itemId (they're intrinsically single)
        if (!string.IsNullOrEmpty(ItemId))
        {
            return true;
        }

        // Creatures follow type-based validation
        return Type switch
        {
            SpawnEntryType.Single => !string.IsNullOrEmpty(CreatureId),
            SpawnEntryType.Multiple => !string.IsNullOrEmpty(CreatureId),
            SpawnEntryType.Band => !string.IsNullOrEmpty(BandId) || (Band != null && Band.IsValid()),
            SpawnEntryType.Unique => !string.IsNullOrEmpty(CreatureId),
            _ => false
        };
    }

    public override string ToString()
    {
        return Type switch
        {
            SpawnEntryType.Single when !string.IsNullOrEmpty(CreatureId) => $"Single: {CreatureId}",
            SpawnEntryType.Single when !string.IsNullOrEmpty(ItemId) => $"Item: {ItemId}",
            SpawnEntryType.Multiple when !string.IsNullOrEmpty(CreatureId) => $"Multiple: {CreatureId} ({Count.DiceNotation})",
            SpawnEntryType.Multiple when !string.IsNullOrEmpty(ItemId) => $"Items: {ItemId} ({Count.DiceNotation})",
            SpawnEntryType.Band when !string.IsNullOrEmpty(BandId) => $"Band: {BandId}",
            SpawnEntryType.Band when Band != null => $"Band: {Band.Name ?? "Inline"}",
            SpawnEntryType.Unique => $"Unique: {CreatureId}",
            _ => "Invalid Entry"
        };
    }
}
