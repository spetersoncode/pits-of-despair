using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Data;

/// <summary>
/// Type information for creature categories.
/// Defines default glyph and color for creature types.
/// </summary>
public class CreatureTypeInfo
{
    public string DefaultGlyph { get; set; } = DataDefaults.UnknownGlyph;
    public string DefaultColor { get; set; } = DataDefaults.DefaultColor;
}

/// <summary>
/// Maps short AI component names used in YAML to full class names.
/// </summary>
public static class AIComponentTypes
{
    private static readonly Dictionary<string, string> TypeMap = new()
    {
        ["Cowardly"] = "CowardlyComponent",
        ["YellForHelp"] = "YellForHelpComponent",
        ["ShootAndScoot"] = "ShootAndScootComponent",
        ["ItemUsage"] = "ItemUsageComponent"
    };

    /// <summary>
    /// Resolves a component type name from YAML to the full class name.
    /// Accepts both short names (Cowardly) and full names (CowardlyComponent).
    /// </summary>
    public static string Resolve(string typeName)
    {
        if (TypeMap.TryGetValue(typeName, out var fullName))
            return fullName;
        return typeName; // Already full name or unknown
    }
}

/// <summary>
/// Serializable creature data structure.
/// Loaded from Data/Creatures/*.yaml files.
/// </summary>
public class CreatureData
{
    /// <summary>
    /// Type metadata for creature categories.
    /// Maps type string (e.g., "goblinoid") to default properties.
    /// </summary>
    private static readonly Dictionary<string, CreatureTypeInfo> TypeInfo = new()
    {
        ["goblinoid"] = new CreatureTypeInfo
        {
            DefaultGlyph = "g",
            DefaultColor = Palette.ToHex(Palette.Default)
        },
        ["rodents"] = new CreatureTypeInfo
        {
            DefaultGlyph = "r",
            DefaultColor = Palette.ToHex(Palette.Minion)
        },
        ["undead"] = new CreatureTypeInfo
        {
            DefaultGlyph = "z",
            DefaultColor = Palette.ToHex(Palette.Default)
        },
        ["allies"] = new CreatureTypeInfo
        {
            DefaultGlyph = "a",
            DefaultColor = Palette.ToHex(Palette.Player)
        }
    };

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Atmospheric description of the creature.
    /// Used for examine command and creature details.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Creature type for category-based defaults (e.g., "goblinoid", "rodents").
    /// Optional - blank type means no inherited defaults.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string Glyph { get; set; } = DataDefaults.UnknownGlyph;

    public string Color { get; set; } = DataDefaults.DefaultColor;

    // Stats
    public int Strength { get; set; } = 0;
    public int Agility { get; set; } = 0;
    public int Endurance { get; set; } = 0;
    public int Will { get; set; } = 0;

    /// <summary>
    /// Creature level (affects difficulty and XP rewards).
    /// Default level 1.
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Base MaxHP before Endurance modifiers.
    /// Actual MaxHP will be: MaxHP + (Endurance × Level)
    /// </summary>
    public int MaxHP { get; set; } = 1;

    public int VisionRange { get; set; } = 10;

    public bool HasMovement { get; set; } = true;

    public bool HasAI { get; set; } = true;

    /// <summary>
    /// Faction allegiance of this creature.
    /// Defaults to "Hostile". Can be "Player" or "Neutral".
    /// Player faction creatures are allies that follow and defend the player.
    /// </summary>
    public string Faction { get; set; } = "Hostile";

    public List<AttackData> Attacks { get; set; } = new();

    /// <summary>
    /// List of goal IDs for goal-based AI (e.g., "MeleeAttack", "Wander").
    /// DEPRECATED: Use AIComponents instead. This field is ignored by the new goal-stack AI system.
    /// </summary>
    public List<string> Goals { get; set; } = new();

    /// <summary>
    /// List of AI component configurations for goal-stack AI system.
    /// Supports short type names (Cowardly) or full names (CowardlyComponent).
    /// Only specify properties when overriding defaults.
    /// Example YAML:
    ///   ai:
    ///     - type: Cowardly
    ///     - type: ShootAndScoot
    ///       fleeTurns: 2
    /// </summary>
    public List<Dictionary<string, object>> Ai { get; set; } = new();

    /// <summary>
    /// List of item IDs that this creature starts equipped with.
    /// If specified, the creature will have an EquipComponent and items will be added and equipped.
    /// </summary>
    public List<string> Equipment { get; set; } = new();

    /// <summary>
    /// Damage types this creature is immune to (takes 0 damage).
    /// </summary>
    public List<DamageType> Immunities { get; set; } = new();

    /// <summary>
    /// Damage types this creature resists (takes half damage, rounded down).
    /// </summary>
    public List<DamageType> Resistances { get; set; } = new();

    /// <summary>
    /// Damage types this creature is vulnerable to (takes double damage).
    /// </summary>
    public List<DamageType> Vulnerabilities { get; set; } = new();

    /// <summary>
    /// Gets whether this creature can equip items.
    /// Returns true if Equipment list is defined and not empty.
    /// </summary>
    public bool GetCanEquip()
    {
        return Equipment != null && Equipment.Count > 0;
    }

    /// <summary>
    /// Converts this data to a Godot Color object.
    /// </summary>
    public Color GetColor()
    {
        return new Color(Color);
    }

    /// <summary>
    /// Gets the faction as an enum value.
    /// Parses the Faction string, defaulting to Hostile if invalid.
    /// </summary>
    public Core.Faction GetFaction()
    {
        if (System.Enum.TryParse<Core.Faction>(Faction, true, out var result))
        {
            return result;
        }
        return Core.Faction.Hostile;
    }

    /// <summary>
    /// Applies type-based defaults for properties not explicitly set in YAML.
    /// Should be called after deserialization.
    /// </summary>
    public void ApplyDefaults()
    {
        if (string.IsNullOrEmpty(Type))
        {
            return; // No type means no inherited defaults
        }

        var typeKey = Type.ToLower();
        if (TypeInfo.TryGetValue(typeKey, out var info))
        {
            // Apply defaults only if not explicitly set
            if (Glyph == DataDefaults.UnknownGlyph)
            {
                Glyph = info.DefaultGlyph;
            }

            if (Color == DataDefaults.DefaultColor)
            {
                Color = info.DefaultColor;
            }
        }
    }
}
