using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Serializable creature data structure.
/// Loaded from Data/Creatures/*.yaml files.
/// </summary>
public class CreatureData
{
    public string Name { get; set; } = string.Empty;

    public string Glyph { get; set; } = "?";

    public string Color { get; set; } = "#FFFFFF";

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
}
