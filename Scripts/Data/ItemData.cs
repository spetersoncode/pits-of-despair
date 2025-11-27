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
/// Contains functional defaults required for game logic.
/// Visual defaults (glyph, color) are defined in YAML sheets.
/// </summary>
public class ItemTypeInfo
{
    public bool IsEquippable { get; set; } = false;
    public bool IsConsumable { get; set; } = false;
    public string? EquipSlot { get; set; } = null;
    public bool AutoPickup { get; set; } = false;
    public bool UsesOfPattern { get; set; } = false;
}

/// <summary>
/// Serializable item data structure.
/// Loaded from Data/Items/*.yaml sheet files.
/// Functional defaults come from code, visual defaults from YAML.
/// </summary>
public class ItemData
{
    /// <summary>
    /// Type metadata for item categories.
    /// Contains functional defaults only - visual defaults are in YAML sheets.
    /// </summary>
    private static readonly Dictionary<string, ItemTypeInfo> TypeInfo = new()
    {
        ["potion"] = new ItemTypeInfo
        {
            IsConsumable = true,
            UsesOfPattern = true
        },
        ["scroll"] = new ItemTypeInfo
        {
            IsConsumable = true,
            UsesOfPattern = true
        },
        ["weapon"] = new ItemTypeInfo
        {
            IsEquippable = true,
            EquipSlot = "MeleeWeapon"
        },
        ["armor"] = new ItemTypeInfo
        {
            IsEquippable = true,
            EquipSlot = "Armor"
        },
        ["ammo"] = new ItemTypeInfo
        {
            IsEquippable = true,
            IsConsumable = true,
            EquipSlot = "Ammo",
            AutoPickup = true
        },
        ["ring"] = new ItemTypeInfo
        {
            IsEquippable = true,
            EquipSlot = "Ring",
            UsesOfPattern = true
        },
        ["wand"] = new ItemTypeInfo
        {
            UsesOfPattern = true
        },
        ["staff"] = new ItemTypeInfo
        {
            UsesOfPattern = true
        }
    };

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Atmospheric description of the item.
    /// Used for examine command and item detail modal.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gold value of this item. Used for shop pricing and loot budget calculations.
    /// Higher value items are placed with stronger guardians.
    /// </summary>
    public int Value { get; set; } = 0;

    /// <summary>
    /// Rarity tier for loot distribution (common, uncommon, rare, epic).
    /// Determines which item pools this item appears in.
    /// </summary>
    public string Rarity { get; set; } = "common";

    /// <summary>
    /// If true, this item will not be automatically spawned by the dungeon generator.
    /// Use for quest items, unique rewards, or items that should only appear in specific contexts.
    /// </summary>
    public bool NoAutoSpawn { get; set; } = false;

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
    /// Whether this item should be automatically picked up when the player walks over it.
    /// Defaults to false (manual pickup required).
    /// </summary>
    public bool AutoPickup { get; set; } = false;

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

    #region Equipment Stat Modifiers
    // These properties provide clean YAML syntax for equipment bonuses.
    // EquipComponent reads these and creates conditions via ConditionFactory.
    // Example YAML: armor: 3, evasion: -1

    /// <summary>Armor modifier when equipped.</summary>
    public int? Armor { get; set; }

    /// <summary>Evasion modifier when equipped.</summary>
    public int? Evasion { get; set; }

    /// <summary>Strength modifier when equipped.</summary>
    public int? Strength { get; set; }

    /// <summary>Agility modifier when equipped.</summary>
    public int? Agility { get; set; }

    /// <summary>Endurance modifier when equipped.</summary>
    public int? Endurance { get; set; }

    /// <summary>Will modifier when equipped.</summary>
    public int? Will { get; set; }

    /// <summary>Max Health modifier when equipped.</summary>
    [YamlMember(Alias = "health")]
    public int? MaxHealth { get; set; }

    /// <summary>Max Willpower modifier when equipped.</summary>
    [YamlMember(Alias = "willpower")]
    public int? MaxWillpower { get; set; }

    /// <summary>Regeneration modifier when equipped (in basis points, 100 = 1%).</summary>
    public int? Regen { get; set; }
    #endregion

    /// <summary>
    /// Explicit targeting configuration for this item.
    /// If null, smart defaults are used based on item type and effects.
    /// </summary>
    public ItemTargeting? Targeting { get; set; } = null;

    /// <summary>
    /// Applies type-based functional defaults from code.
    /// Visual defaults (glyph, color) come from YAML sheets.
    /// </summary>
    public void ApplyDefaults()
    {
        if (!string.IsNullOrEmpty(Type))
        {
            var typeKey = Type.ToLower();
            if (TypeInfo.TryGetValue(typeKey, out var info))
            {
                // Apply functional defaults only if not explicitly set
                IsConsumable ??= info.IsConsumable;
                IsEquippable ??= info.IsEquippable;
                EquipSlot ??= info.EquipSlot;

                // AutoPickup only if not already true
                if (!AutoPickup)
                {
                    AutoPickup = info.AutoPickup;
                }
            }

            // Set weapon attack name to the display name
            if (typeKey == "weapon" && Attack != null)
            {
                Attack.Name = GetDisplayName();
            }
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
    /// Note: "Ring" maps to Ring1 by default - callers should use GetAvailableRingSlot()
    /// for dynamic slot selection.
    /// </summary>
    public EquipmentSlot GetEquipmentSlot()
    {
        if (string.IsNullOrEmpty(EquipSlot))
        {
            return Scripts.Data.EquipmentSlot.None;
        }

        // Handle "Ring" specially - it maps to Ring1 (callers can check for Ring2 if needed)
        if (EquipSlot.Equals("Ring", System.StringComparison.OrdinalIgnoreCase))
        {
            return Scripts.Data.EquipmentSlot.Ring1;
        }

        if (System.Enum.TryParse<EquipmentSlot>(EquipSlot, ignoreCase: true, out var slot))
        {
            return slot;
        }

        GD.PrintErr($"ItemData: Invalid equipment slot '{EquipSlot}' for item '{Name}'");
        return Scripts.Data.EquipmentSlot.None;
    }

    /// <summary>
    /// Returns true if this item is a ring (can go in Ring1 or Ring2).
    /// </summary>
    public bool IsRing => EquipSlot?.Equals("Ring", System.StringComparison.OrdinalIgnoreCase) == true;

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
    /// For count=1: returns "potion of healing" or "short sword"
    /// For count>1: returns "3 potions of healing" or "short sword (3)"
    /// </summary>
    public string GetDisplayName(int count = 1)
    {
        if (string.IsNullOrEmpty(Type))
        {
            return count == 1 ? Name : $"{Name} ({count})";
        }

        var typeKey = Type.ToLower();

        // Check if this type uses "X of Y" pattern
        if (TypeInfo.TryGetValue(typeKey, out var info) && info.UsesOfPattern)
        {
            if (count == 1)
            {
                // Single item: "potion of healing"
                return $"{typeKey} of {Name}";
            }
            else
            {
                // Multiple items: "3 potions of healing"
                string pluralType = GetPluralType(typeKey);
                return $"{count} {pluralType} of {Name}";
            }
        }

        // Special case for ammo: name is already plural, just prepend count
        if (typeKey == "ammo")
        {
            return count == 1 ? Name : $"{count} {Name}";
        }

        // Other items (weapon, armor): just append count in parens
        return count == 1 ? Name : $"{Name} ({count})";
    }

    /// <summary>
    /// Gets the plural form of an item type for display names.
    /// </summary>
    private string GetPluralType(string typeKey)
    {
        return typeKey switch
        {
            "potion" => "potions",
            "scroll" => "scrolls",
            "ring" => "rings",
            "wand" => "wands",
            "staff" => "staves",
            _ => typeKey + "s"
        };
    }

    /// <summary>
    /// Determines if this item requires targeting when activated.
    /// Items with explicit targeting config, AOE effects, or certain effect types require targeting.
    /// </summary>
    public bool RequiresTargeting()
    {
        // Items with explicit targeting configuration always require targeting
        if (Targeting != null)
        {
            return true;
        }

        foreach (var effectDef in Effects)
        {
            var effectType = effectDef.Type?.ToLower();

            // Charm effects require targeting
            if (effectType == "charm")
            {
                return true;
            }

            // Fireball and other AOE effects require targeting
            if (effectType == "fireball")
            {
                return true;
            }

            // apply_condition with confusion requires targeting
            if (effectType == "apply_condition" &&
                effectDef.ConditionType?.ToLower() == "confusion")
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
    /// Uses the unified Effect.CreateFromDefinition factory.
    /// </summary>
    public List<Effect> GetEffects()
    {
        var effects = new List<Effect>();

        foreach (var effectDef in Effects)
        {
            // Convert item EffectDefinition to unified Effects.EffectDefinition
            var unifiedDef = PitsOfDespair.Effects.EffectDefinition.FromItemEffect(effectDef);
            var effect = Effect.CreateFromDefinition(unifiedDef);
            if (effect != null)
            {
                effects.Add(effect);
            }
            else
            {
                GD.PrintErr($"ItemData: Unknown effect type '{effectDef.Type}' in item '{Name}'");
            }
        }

        return effects;
    }
}

/// <summary>
/// Represents an effect definition loaded from YAML.
/// </summary>
public class EffectDefinition
{
    /// <summary>
    /// The type of effect (e.g., "heal", "damage", "teleport", "apply_condition").
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
    /// Duration parameter for condition effects (in turns).
    /// </summary>
    public int Duration { get; set; } = 0;

    /// <summary>
    /// Dice notation for duration (e.g., "2d3", "1d4"). Overrides Duration if specified.
    /// </summary>
    public string? DurationDice { get; set; } = null;

    /// <summary>
    /// Condition type for apply_condition effects (e.g., "confusion", "armor_buff").
    /// </summary>
    public string? ConditionType { get; set; } = null;

    /// <summary>
    /// Dice notation for variable amounts (e.g., "3d6", "2d8+2").
    /// </summary>
    public string? Dice { get; set; } = null;

    /// <summary>
    /// Damage type for damage effects (e.g., "fire", "cold", "poison").
    /// </summary>
    public string? DamageType { get; set; } = null;

    /// <summary>
    /// Hazard type for create_hazard effects (e.g., "poison_cloud", "fire").
    /// </summary>
    public string? HazardType { get; set; } = null;

    /// <summary>
    /// Area radius for AOE effects (in tiles).
    /// </summary>
    [YamlMember(Alias = "radius")]
    public int Radius { get; set; } = 0;

    /// <summary>
    /// Target's save stat for saving throw (e.g., "end" for physical, "wil" for mental).
    /// If set, a saving throw is required before the effect applies.
    /// </summary>
    public string? SaveStat { get; set; } = null;

    /// <summary>
    /// Caster's attack stat for the opposed saving throw roll.
    /// Defaults to "wil" if SaveStat is set but AttackStat is not.
    /// </summary>
    public string? AttackStat { get; set; } = null;

    /// <summary>
    /// Modifier to the caster's save roll. Positive = harder to resist, negative = easier.
    /// </summary>
    public int SaveModifier { get; set; } = 0;

    /// <summary>
    /// Dice notation for damage-over-time effects (e.g., "1d3" for acid DoT).
    /// </summary>
    public string? DotDamage { get; set; } = null;
}

/// <summary>
/// Explicit targeting configuration for items.
/// Allows items to specify how they select targets in YAML.
/// </summary>
public class ItemTargeting
{
    /// <summary>
    /// Target type: "self", "enemy", "ally", "tile", "creature", "area".
    /// </summary>
    public string? Type { get; set; } = null;

    /// <summary>
    /// Targeting range in tiles. If 0, uses item's default range.
    /// </summary>
    public int Range { get; set; } = 0;

    /// <summary>
    /// Area radius for AoE items (in tiles).
    /// </summary>
    [YamlMember(Alias = "radius")]
    public int Radius { get; set; } = 0;

    /// <summary>
    /// Whether line-of-sight is required. Defaults to true.
    /// </summary>
    [YamlMember(Alias = "requiresLos")]
    public bool RequiresLOS { get; set; } = true;
}
