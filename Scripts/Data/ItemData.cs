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
            DefaultGlyph = "!",
            DefaultColor = Palette.ToHex(Palette.Default),
            IsEquippable = false,
            IsConsumable = true
        },
        ["scroll"] = new ItemTypeInfo
        {
            Prefix = "scroll of ",
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
    /// Items are activatable if they are consumable or have charges.
    /// </summary>
    public bool IsActivatable()
    {
        return GetIsConsumable() || !string.IsNullOrEmpty(ChargesDice);
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

            case "armor_buff":
                return new ApplyStatusEffect("armor_buff", definition.Amount, definition.Duration);

            case "strength_buff":
                return new ApplyStatusEffect("strength_buff", definition.Amount, definition.Duration);

            case "agility_buff":
                return new ApplyStatusEffect("agility_buff", definition.Amount, definition.Duration);

            case "endurance_buff":
                return new ApplyStatusEffect("endurance_buff", definition.Amount, definition.Duration);

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

    /// <summary>
    /// Duration parameter for status effects (in turns).
    /// </summary>
    public int Duration { get; set; } = 0;
}
