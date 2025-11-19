using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Core;
using PitsOfDespair.Effects;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Data;
using YamlDotNet.Serialization;

namespace PitsOfDespair.Data;

/// <summary>
/// Type information for item categories.
/// Defines default properties and display name prefixes.
/// </summary>
public class ItemTypeInfo
{
    public string Prefix { get; set; } = string.Empty;
    public string? PluralType { get; set; } = null;
    public string DefaultGlyph { get; set; } = DataDefaults.UnknownGlyph;
    public string DefaultColor { get; set; } = DataDefaults.DefaultColor;
    public bool IsEquippable { get; set; } = false;
    public bool IsConsumable { get; set; } = false;
    public string? EquipSlot { get; set; } = null;
}

/// <summary>
/// Serializable item data structure.
/// Loaded from Data/Items/*.yaml files.
/// </summary>
public class ItemData
{
    /// <summary>
    /// Type metadata for item categories.
    /// Maps type string (e.g., "potion") to default properties.
    /// </summary>
    private static readonly Dictionary<string, ItemTypeInfo> TypeInfo = new()
    {
        ["potion"] = new ItemTypeInfo
        {
            Prefix = "potion of ",
            PluralType = "potions",
            DefaultGlyph = "!",
            DefaultColor = Palette.ToHex(Palette.Default),
            IsEquippable = false,
            IsConsumable = true
        },
        ["scroll"] = new ItemTypeInfo
        {
            Prefix = "scroll of ",
            PluralType = "scrolls",
            DefaultGlyph = "♪",
            DefaultColor = Palette.ToHex(Palette.Default),
            IsEquippable = false,
            IsConsumable = true
        },
        ["weapon"] = new ItemTypeInfo
        {
            Prefix = "",
            DefaultGlyph = "/",
            DefaultColor = Palette.ToHex(Palette.Silver),
            IsEquippable = true,
            IsConsumable = false,
            EquipSlot = "MeleeWeapon"
        },
        ["armor"] = new ItemTypeInfo
        {
            Prefix = "",
            DefaultGlyph = "[",
            DefaultColor = Palette.ToHex(Palette.Iron),
            IsEquippable = true,
            IsConsumable = false,
            EquipSlot = "Armor"
        }
    };

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Item type for category-based defaults (e.g., "potion", "scroll").
    /// Optional - blank type means no inherited defaults.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = string.Empty;

    [YamlMember(Alias = "glyph")]
    public string? Glyph { get; set; } = null;

    public string Color { get; set; } = DataDefaults.DefaultColor;

    /// <summary>
    /// Unique identifier for the source data file.
    /// Used for inventory stacking - items with the same DataFileId can stack.
    /// </summary>
    public string DataFileId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this item is consumable (one-time use, stackable).
    /// If null, will be set by ApplyDefaults based on Type.
    /// </summary>
    [YamlMember(Alias = "isConsumable")]
    public bool? IsConsumable { get; set; } = null;

    /// <summary>
    /// Dice notation for item charges (e.g., "1d6", "2d3+1").
    /// If empty/null, this item does not use charges. Optional in YAML.
    /// </summary>
    [YamlMember(Alias = "charges")]
    public string? ChargesDice { get; set; } = null;

    /// <summary>
    /// Number of turns required to recharge 1 charge.
    /// If 0, this item does not recharge. Optional in YAML.
    /// </summary>
    public int RechargeTurns { get; set; } = 0;

    /// <summary>
    /// Raw effect definitions from YAML.
    /// These are deserialized from the YAML file and then converted to Effect instances.
    /// </summary>
    public List<EffectDefinition> Effects { get; set; } = new();

    /// <summary>
    /// Whether this item can be equipped.
    /// If null, will be set by ApplyDefaults based on Type.
    /// </summary>
    [YamlMember(Alias = "isEquippable")]
    public bool? IsEquippable { get; set; } = null;

    /// <summary>
    /// Equipment slot this item occupies when equipped (e.g., "MeleeWeapon", "Armor").
    /// Case-insensitive, parsed to EquipmentSlot enum.
    /// </summary>
    [YamlMember(Alias = "equipSlot")]
    public string? EquipSlot { get; set; } = null;

    /// <summary>
    /// Attack data for weapon items.
    /// Defines damage range and attack properties.
    /// </summary>
    public AttackData? Attack { get; set; } = null;

    // Equipment Stat Bonuses
    /// <summary>
    /// Armor value provided by this item (reduces incoming damage).
    /// </summary>
    public int ArmorValue { get; set; } = 0;

    /// <summary>
    /// Evasion penalty from this item (typically negative for heavy armor).
    /// </summary>
    public int EvasionPenalty { get; set; } = 0;

    /// <summary>
    /// Strength bonus from this item (e.g., Ring of Strength).
    /// </summary>
    public int StrengthBonus { get; set; } = 0;

    /// <summary>
    /// Agility bonus from this item (e.g., Ring of Agility).
    /// </summary>
    public int AgilityBonus { get; set; } = 0;

    /// <summary>
    /// Endurance bonus from this item (e.g., Amulet of Health).
    /// </summary>
    public int EnduranceBonus { get; set; } = 0;

    /// <summary>
    /// Will bonus from this item (e.g., Ring of Will).
    /// </summary>
    public int WillBonus { get; set; } = 0;

    /// <summary>
    /// Applies type-based defaults for properties not explicitly set in YAML.
    /// Generates display name with prefix (e.g., "potion of cure light wounds").
    /// Should be called after deserialization.
    /// </summary>
    public void ApplyDefaults()
    {
        if (string.IsNullOrEmpty(Type))
        {
            return; // No type means no inherited defaults
        }

        var typeKey = Type.ToLower();
        if (TypeInfo.TryGetValue(typeKey, out var info))
        {
            // Apply prefix to name if one exists and hasn't been applied yet
            if (!string.IsNullOrEmpty(info.Prefix) && !Name.StartsWith(info.Prefix))
            {
                Name = info.Prefix + Name;
            }

            // Apply defaults for properties not explicitly set
            Glyph ??= info.DefaultGlyph;

            // Only set color if it's still the default white
            if (Color == DataDefaults.DefaultColor)
            {
                Color = info.DefaultColor;
            }

            IsConsumable ??= info.IsConsumable;
            IsEquippable ??= info.IsEquippable;
            EquipSlot ??= info.EquipSlot;
        }

        // Set weapon attack name to the full display name (including prefix)
        if (typeKey == "weapon" && Attack != null)
        {
            Attack.Name = Name;
        }
    }

    /// <summary>
    /// Gets the glyph for this item, using type-based default if not explicitly set.
    /// </summary>
    public string GetGlyph()
    {
        return Glyph ?? DataDefaults.UnknownGlyph;
    }

    /// <summary>
    /// Gets whether this item is consumable, using type-based default if not explicitly set.
    /// </summary>
    public bool GetIsConsumable()
    {
        return IsConsumable ?? false;
    }

    /// <summary>
    /// Gets whether this item is equippable, using type-based default if not explicitly set.
    /// </summary>
    public bool GetIsEquippable()
    {
        return IsEquippable ?? false;
    }

    /// <summary>
    /// Parses and returns the equipment slot for this item.
    /// Returns EquipmentSlot.None if not set or invalid.
    /// </summary>
    public EquipmentSlot GetEquipmentSlot()
    {
        if (string.IsNullOrEmpty(EquipSlot))
        {
            return Scripts.Data.EquipmentSlot.None;
        }

        if (System.Enum.TryParse<EquipmentSlot>(EquipSlot, ignoreCase: true, out var slot))
        {
            return slot;
        }

        GD.PrintErr($"ItemData: Invalid equipment slot '{EquipSlot}' for item '{Name}'");
        return Scripts.Data.EquipmentSlot.None;
    }

    /// <summary>
    /// Calculates the maximum possible charges from dice notation.
    /// Returns 0 if no charges dice notation is set.
    /// </summary>
    public int GetMaxCharges()
    {
        if (string.IsNullOrEmpty(ChargesDice))
        {
            return 0;
        }

        if (DiceRoller.TryParse(ChargesDice, out int count, out int sides, out int modifier))
        {
            return (count * sides) + modifier;
        }

        GD.PrintErr($"ItemData: Invalid charges dice notation '{ChargesDice}' for item '{Name}'");
        return 0;
    }

    /// <summary>
    /// Determines if this item can be activated from inventory.
    /// Items are activatable if they are consumable, have charges, or are reach weapons (melee weapons with range > 1).
    /// </summary>
    public bool IsActivatable()
    {
        // Consumables and charged items
        if (GetIsConsumable() || !string.IsNullOrEmpty(ChargesDice))
            return true;

        // Reach weapons (melee weapons with range > 1)
        if (Attack != null && Attack.Type == AttackType.Melee && Attack.Range > 1)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the display name for this item with proper pluralization based on count.
    /// For count=1: returns "potion of healing"
    /// For count>1 with plural type: returns "3 potions of healing"
    /// For count>1 without plural type: returns "sword (3)"
    /// </summary>
    public string GetDisplayName(int count = 1)
    {
        // Single item - just return the name
        if (count == 1)
        {
            return Name;
        }

        // Multiple items - check if we have a plural type
        if (!string.IsNullOrEmpty(Type))
        {
            var typeKey = Type.ToLower();
            if (TypeInfo.TryGetValue(typeKey, out var info) && !string.IsNullOrEmpty(info.PluralType))
            {
                // Extract the base name (everything after the prefix)
                string baseName = Name;
                if (!string.IsNullOrEmpty(info.Prefix) && Name.StartsWith(info.Prefix))
                {
                    baseName = Name.Substring(info.Prefix.Length);
                }

                // Construct plural form: "3 potions of healing"
                return $"{count} {info.PluralType} of {baseName}";
            }
        }

        // Fallback for items without plural type (weapons, armor, etc.)
        return $"{Name} ({count})";
    }

    /// <summary>
    /// Determines if this item requires targeting when activated.
    /// Currently, items with "confusion" status effects require targeting.
    /// </summary>
    public bool RequiresTargeting()
    {
        // Check if any effect is an apply_status effect with confusion type
        foreach (var effectDef in Effects)
        {
            if (effectDef.Type?.ToLower() == "apply_status" &&
                effectDef.StatusType?.ToLower() == "confusion")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the targeting range for this item.
    /// Returns the range specified in effects, or a default of 8 if not specified.
    /// </summary>
    public int GetTargetingRange()
    {
        // For now, return a default range for all targeted items
        // In the future, this could be specified in the YAML
        return 8;
    }

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

            case "apply_status":
                // Generic status effect - uses StatusType and DurationDice from definition
                string statusType = definition.StatusType ?? string.Empty;
                string durationDice = definition.DurationDice ?? string.Empty;

                if (string.IsNullOrEmpty(statusType))
                {
                    GD.PrintErr($"ItemData.CreateEffect: apply_status effect in item '{Name}' has no statusType specified");
                    return null;
                }

                return new ApplyStatusEffect(statusType, definition.Amount, definition.Duration, durationDice);

            case "teleport":
                return new TeleportEffect();

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
    /// The type of effect (e.g., "heal", "damage", "teleport", "apply_status").
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

    /// <summary>
    /// Duration parameter for status effects (in turns).
    /// </summary>
    public int Duration { get; set; } = 0;

    /// <summary>
    /// Dice notation for duration (e.g., "2d3", "1d4"). Overrides Duration if specified.
    /// </summary>
    public string? DurationDice { get; set; } = null;

    /// <summary>
    /// Status type for apply_status effects (e.g., "confusion", "armor_buff").
    /// </summary>
    public string? StatusType { get; set; } = null;
}
