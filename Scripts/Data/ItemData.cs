using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Serializable item data structure.
/// Loaded from Data/Items/*.yaml files.
/// </summary>
public class ItemData
{
    public string Name { get; set; } = string.Empty;

    public string Glyph { get; set; } = "?";

    public string Color { get; set; } = "#FFFFFF";

    public string ItemType { get; set; } = "generic";

    /// <summary>
    /// Unique identifier for the source data file.
    /// Used for inventory stacking - items with the same DataFileId can stack.
    /// </summary>
    public string DataFileId { get; set; } = string.Empty;

    /// <summary>
    /// Converts this data to a Godot Color object.
    /// </summary>
    public Color GetColor()
    {
        return new Color(Color);
    }
}
