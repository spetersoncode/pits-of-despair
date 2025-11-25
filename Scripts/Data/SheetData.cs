using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Wrapper for creature sheet YAML files.
/// Format:
///   type: goblinoid
///   defaults:
///     glyph: g
///     color: Default
///   entries:
///     goblin:
///       name: goblin
///       ...
/// </summary>
public class CreatureSheetData
{
    /// <summary>
    /// Creature type for this sheet (e.g., "goblinoid", "rodents", "undead").
    /// All entries inherit this type unless overridden.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Default values applied to all entries in this sheet.
    /// Individual entries can override any default.
    /// </summary>
    public CreatureDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Creature entries keyed by ID (e.g., "goblin", "goblin_archer").
    /// The key becomes the creature's ID for lookup.
    /// </summary>
    public Dictionary<string, CreatureData> Entries { get; set; } = new();
}

/// <summary>
/// Default values for creatures in a sheet.
/// These values are applied to entries that don't specify them.
/// </summary>
public class CreatureDefaults
{
    public string? Glyph { get; set; }
    public string? Color { get; set; }
    public int? Threat { get; set; }
    public int? Strength { get; set; }
    public int? Agility { get; set; }
    public int? Endurance { get; set; }
    public int? Will { get; set; }
    public int? VisionRange { get; set; }
    public bool? HasMovement { get; set; }
    public bool? HasAI { get; set; }
    public string? Faction { get; set; }
}

/// <summary>
/// Wrapper for item sheet YAML files.
/// Format:
///   type: potion
///   defaults:
///     glyph: "!"
///     isConsumable: true
///   entries:
///     strength_1:
///       name: minor strength
///       ...
/// </summary>
public class ItemSheetData
{
    /// <summary>
    /// Item type for this sheet (e.g., "potion", "weapon", "armor").
    /// All entries inherit this type. Used for ID generation: {type}_{key}.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Default values applied to all entries in this sheet.
    /// Individual entries can override any default.
    /// </summary>
    public ItemDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Item entries keyed by ID suffix (e.g., "strength_1", "club").
    /// Final ID is {type}_{key} (e.g., "potion_strength_1", "weapon_club").
    /// </summary>
    public Dictionary<string, ItemData> Entries { get; set; } = new();
}

/// <summary>
/// Default values for items in a sheet.
/// These values are applied to entries that don't specify them.
/// </summary>
public class ItemDefaults
{
    public string? Glyph { get; set; }
    public string? Color { get; set; }
    public bool? IsConsumable { get; set; }
    public bool? IsEquippable { get; set; }
    public string? EquipSlot { get; set; }
    public bool? AutoPickup { get; set; }
}

/// <summary>
/// Wrapper for skill sheet YAML files.
/// Format:
///   type: strength
///   defaults:
///     tier: 1
///   entries:
///     power_attack:
///       name: Power Attack
///       ...
/// </summary>
public class SkillSheetData
{
    /// <summary>
    /// Skill category/type for this sheet (e.g., "strength", "agility").
    /// Informational only - not used for ID generation.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Default values applied to all entries in this sheet.
    /// Individual entries can override any default.
    /// </summary>
    public SkillDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Skill entries keyed by ID (e.g., "power_attack", "cleave").
    /// The key becomes the skill's ID for lookup.
    /// </summary>
    public Dictionary<string, SkillDefinition> Entries { get; set; } = new();
}

/// <summary>
/// Default values for skills in a sheet.
/// These values are applied to entries that don't specify them.
/// </summary>
public class SkillDefaults
{
    public string? Category { get; set; }
    public string? Targeting { get; set; }
    public int? Tier { get; set; }
    public int? WillpowerCost { get; set; }
    public int? Range { get; set; }
}
