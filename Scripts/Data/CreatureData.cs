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
    /// Converts this data to a Godot Color object.
    /// </summary>
    public Color GetColor()
    {
        return new Color(Color);
    }
}
