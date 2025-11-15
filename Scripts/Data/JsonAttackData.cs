using System.Text.Json.Serialization;

namespace PitsOfDespair.Data;

/// <summary>
/// JSON-serializable attack data structure.
/// Embedded within creature definitions.
/// </summary>
public class JsonAttackData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("minDamage")]
    public int MinDamage { get; set; } = 1;

    [JsonPropertyName("maxDamage")]
    public int MaxDamage { get; set; } = 1;

    [JsonPropertyName("range")]
    public int Range { get; set; } = 1;
}
