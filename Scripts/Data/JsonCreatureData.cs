using Godot;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PitsOfDespair.Data;

/// <summary>
/// JSON-serializable creature data structure.
/// Loaded from Data/Creatures/*.json files.
/// </summary>
public class JsonCreatureData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("glyph")]
    public string Glyph { get; set; } = "?";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";

    [JsonPropertyName("maxHP")]
    public int MaxHP { get; set; } = 1;

    [JsonPropertyName("visionRange")]
    public int VisionRange { get; set; } = 0;

    [JsonPropertyName("hasMovement")]
    public bool HasMovement { get; set; } = false;

    [JsonPropertyName("hasAI")]
    public bool HasAI { get; set; } = false;

    [JsonPropertyName("attacks")]
    public List<JsonAttackData> Attacks { get; set; } = new();

    /// <summary>
    /// Converts this JSON data to a Godot Color object.
    /// </summary>
    public Color GetColor()
    {
        return new Color(Color);
    }
}
