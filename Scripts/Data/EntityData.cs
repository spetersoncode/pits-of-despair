using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Resource defining entity properties and component configuration.
/// Used by EntityFactory to create data-driven entities.
/// </summary>
[GlobalClass]
public partial class EntityData : Resource
{
    /// <summary>
    /// Display name of the entity (e.g., "Goblin", "Orc", "Dragon").
    /// </summary>
    [Export]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Single character representing this entity (e.g., "g" for goblin, "@" for player).
    /// Must be exactly one character. Using string instead of char because Godot's
    /// inspector treats char properties as integer fields (ASCII codes), making them
    /// unintuitive to edit. String properties display as text fields in the inspector.
    /// </summary>
    [Export]
    public string Glyph
    {
        get => _glyph;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                GD.PushWarning("EntityData.Glyph cannot be empty. Defaulting to '?'.");
                _glyph = "?";
            }
            else if (value.Length > 1)
            {
                GD.PushWarning($"EntityData.Glyph must be exactly 1 character. Received '{value}'. Using first character '{value[0]}'.");
                _glyph = value[0].ToString();
            }
            else
            {
                _glyph = value;
            }
        }
    }
    private string _glyph = "?";

    /// <summary>
    /// Color to render the glyph.
    /// </summary>
    [Export]
    public Color GlyphColor { get; set; } = Colors.White;

    /// <summary>
    /// Movement configuration. If null, entity will not have a MovementComponent.
    /// </summary>
    [Export]
    public MovementData? Movement { get; set; }

    // Future component data properties:
    // [Export] public HealthData? Health { get; set; }
    // [Export] public AIData? AI { get; set; }
    // [Export] public CombatData? Combat { get; set; }
}
