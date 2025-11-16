using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Effects;

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
    /// Whether this item can be activated (used) from the inventory.
    /// </summary>
    public bool IsActivatable { get; set; } = false;

    /// <summary>
    /// Raw effect definitions from YAML.
    /// These are deserialized from the YAML file and then converted to Effect instances.
    /// </summary>
    public List<EffectDefinition> Effects { get; set; } = new();

    /// <summary>
    /// Converts this data to a Godot Color object.
    /// </summary>
    public Color GetColor()
    {
        return new Color(Color);
    }

    /// <summary>
    /// Converts the YAML effect definitions into actual Effect instances.
    /// </summary>
    public List<Effect> GetEffects()
    {
        var effects = new List<Effect>();

        foreach (var effectDef in Effects)
        {
            var effect = CreateEffect(effectDef);
            if (effect != null)
            {
                effects.Add(effect);
            }
        }

        return effects;
    }

    private Effect CreateEffect(EffectDefinition definition)
    {
        switch (definition.Type?.ToLower())
        {
            case "heal":
                return new HealEffect(definition.Amount);

            case "blink":
                return new BlinkEffect(definition.Range);

            default:
                GD.PrintErr($"ItemData: Unknown effect type '{definition.Type}' in item '{Name}'");
                return null;
        }
    }
}

/// <summary>
/// Represents an effect definition loaded from YAML.
/// </summary>
public class EffectDefinition
{
    /// <summary>
    /// The type of effect (e.g., "heal", "damage", "teleport").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Numeric parameter for the effect (e.g., heal amount, damage amount).
    /// </summary>
    public int Amount { get; set; } = 0;

    /// <summary>
    /// Range parameter for area/distance effects (e.g., teleport range).
    /// </summary>
    public int Range { get; set; } = 0;
}
