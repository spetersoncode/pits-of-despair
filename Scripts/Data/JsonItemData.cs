using Godot;
using System.Text.Json.Serialization;

namespace PitsOfDespair.Data;

/// <summary>
/// JSON-serializable item data structure.
/// Loaded from Data/Items/*.json files.
/// </summary>
public class JsonItemData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("glyph")]
    public string Glyph { get; set; } = "?";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";

    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = "generic";

    /// <summary>
    /// Converts this JSON data to a Godot Color object.
    /// </summary>
    public Color GetColor()
    {
        return new Color(Color);
    }
}
