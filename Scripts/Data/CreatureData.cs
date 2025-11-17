using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Type information for creature categories.
/// Defines default glyph and color for creature types.
/// </summary>
public class CreatureTypeInfo
{
    public string DefaultGlyph { get; set; } = "?";
    public string DefaultColor { get; set; } = "#FFFFFF";
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
            DefaultColor = "#808080"
        },
        ["vermin"] = new CreatureTypeInfo
        {
            DefaultGlyph = "r",
            DefaultColor = "#808080"
        }
    };

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Creature type for category-based defaults (e.g., "goblinoid", "vermin").
    /// Optional - blank type means no inherited defaults.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string Glyph { get; set; } = "?";

    public string Color { get; set; } = "#FFFFFF";

    // Stats
    public int Strength { get; set; } = 0;
    public int Agility { get; set; } = 0;
    public int Endurance { get; set; } = 0;
    public int Will { get; set; } = 0;

    /// <summary>
    /// Base MaxHP before Endurance modifiers.
    /// Actual MaxHP will be: MaxHP + (Endurance × 2)
    /// </summary>
    public int MaxHP { get; set; } = 1;

    public int VisionRange { get; set; } = 0;

    public bool HasMovement { get; set; } = false;

    public bool HasAI { get; set; } = false;

    public List<AttackData> Attacks { get; set; } = new();

    /// <summary>
    /// List of goal IDs for goal-based AI (e.g., "MeleeAttack", "Wander").
    /// Only used if HasAI is true.
    /// </summary>
    public List<string> Goals { get; set; } = new();

    /// <summary>
    /// List of item IDs that this creature starts equipped with.
    /// If specified, the creature will have an EquipComponent and items will be added and equipped.
    /// </summary>
    public List<string> Equipment { get; set; } = new();

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
            if (Glyph == "?")
            {
                Glyph = info.DefaultGlyph;
            }

            if (Color == "#FFFFFF")
            {
                Color = info.DefaultColor;
            }
        }
    }
}
