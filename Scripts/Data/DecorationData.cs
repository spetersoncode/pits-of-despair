using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Data definition for a decoration entity.
/// Decorations are flavor entities that populate dungeon rooms for visual atmosphere.
/// </summary>
public class DecorationData
{
    /// <summary>
    /// Unique identifier for this decoration (e.g., "clay_vase", "rubble").
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display name shown in examine/interaction.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Atmospheric description of this decoration.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Character or symbol representing this decoration.
    /// Letters are reserved for creatures - decorations use symbols/Unicode.
    /// </summary>
    public string Glyph { get; set; }

    /// <summary>
    /// Color for rendering the glyph.
    /// Supports "Palette.ColorName" or "#RRGGBB" format.
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// Whether entities can walk through this decoration.
    /// True for most decorations, false for blocking objects (pillars, braziers).
    /// </summary>
    public bool IsWalkable { get; set; } = true;

    /// <summary>
    /// Health points. If > 0, decoration is destructible and can be attacked.
    /// </summary>
    public int Health { get; set; } = 0;

    /// <summary>
    /// Custom message when destroyed (e.g., "shatters into fragments").
    /// Used instead of "dies" for decoration death messages.
    /// </summary>
    public string DestructionMessage { get; set; } = "breaks apart";

    /// <summary>
    /// Theme association. Null means generic/universal decoration.
    /// </summary>
    public string ThemeId { get; set; } = null;

    /// <summary>
    /// Placement hints for positioning logic.
    /// Values: "wall_adjacent", "corner", "center", "scattered", "clustered"
    /// </summary>
    public List<string> PlacementHints { get; set; } = new();
}
