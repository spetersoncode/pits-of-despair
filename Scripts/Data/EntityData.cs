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
    /// ASCII character representing this entity (e.g., 'g' for goblin, '@' for player).
    /// </summary>
    [Export]
    public char Glyph { get; set; } = '?';

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
