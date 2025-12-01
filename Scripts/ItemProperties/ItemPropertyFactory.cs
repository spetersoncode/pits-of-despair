using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Data;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Single source of truth for creating ItemProperty instances from type strings.
/// Used by effects, item generation, and any other system that needs to apply properties.
/// </summary>
public static class ItemPropertyFactory
{
    /// <summary>
    /// Registry of all spawnable properties with their metadata.
    /// </summary>
    private static readonly Dictionary<string, PropertyMetadata> PropertyRegistry = new()
    {
        // Weapon properties
        ["weapon_enhancement"] = new PropertyMetadata
        {
            TypeId = "weapon_enhancement",
            IntroFloor = 2,
            SpawnWeight = 1.0f,
            MinAmount = 1,
            MaxAmount = 3,
            ValidTypes = ItemType.Weapon
        },
        ["flaming"] = new PropertyMetadata
        {
            TypeId = "flaming",
            IntroFloor = 4,
            SpawnWeight = 0.7f,
            MinAmount = 1,
            MaxAmount = 2,
            ValidTypes = ItemType.Weapon | ItemType.Ammo
        },
        ["freezing"] = new PropertyMetadata
        {
            TypeId = "freezing",
            IntroFloor = 4,
            SpawnWeight = 0.7f,
            MinAmount = 1,
            MaxAmount = 2,
            ValidTypes = ItemType.Weapon | ItemType.Ammo
        },
        ["electrified"] = new PropertyMetadata
        {
            TypeId = "electrified",
            IntroFloor = 5,
            SpawnWeight = 0.6f,
            MinAmount = 1,
            MaxAmount = 2,
            ValidTypes = ItemType.Weapon | ItemType.Ammo
        },
        ["venomous"] = new PropertyMetadata
        {
            TypeId = "venomous",
            IntroFloor = 3,
            SpawnWeight = 0.7f,
            MinAmount = 1,
            MaxAmount = 2,
            ValidTypes = ItemType.Weapon | ItemType.Ammo
        },
        ["vampiric"] = new PropertyMetadata
        {
            TypeId = "vampiric",
            IntroFloor = 5,
            SpawnWeight = 0.5f,
            MinAmount = 25,
            MaxAmount = 35,
            ValidTypes = ItemType.Weapon
        },
        ["silver"] = new PropertyMetadata
        {
            TypeId = "silver",
            IntroFloor = 4,
            SpawnWeight = 0.3f,
            MinAmount = 1,
            MaxAmount = 1,
            ValidTypes = ItemType.Weapon | ItemType.Ammo
        },

        // Armor properties
        ["armor_enhancement"] = new PropertyMetadata
        {
            TypeId = "armor_enhancement",
            IntroFloor = 2,
            SpawnWeight = 1.0f,
            MinAmount = 1,
            MaxAmount = 3,
            ValidTypes = ItemType.Armor
        },

        // Ring properties (stat bonuses for rings only)
        ["evasion"] = new PropertyMetadata
        {
            TypeId = "evasion",
            IntroFloor = 3,
            SpawnWeight = 1.0f,
            MinAmount = 1,
            MaxAmount = 2,
            ValidTypes = ItemType.Ring
        },
        ["regen"] = new PropertyMetadata
        {
            TypeId = "regen",
            IntroFloor = 4,
            SpawnWeight = 0.9f,
            MinAmount = 50,
            MaxAmount = 100,
            ValidTypes = ItemType.Ring
        },
        ["armor"] = new PropertyMetadata
        {
            TypeId = "armor",
            IntroFloor = 2,
            SpawnWeight = 1.0f,
            MinAmount = 1,
            MaxAmount = 2,
            ValidTypes = ItemType.Ring
        },
        ["max_health"] = new PropertyMetadata
        {
            TypeId = "max_health",
            IntroFloor = 4,
            SpawnWeight = 0.8f,
            MinAmount = 5,
            MaxAmount = 15,
            ValidTypes = ItemType.Ring
        },
        ["thorns"] = new PropertyMetadata
        {
            TypeId = "thorns",
            IntroFloor = 4,
            SpawnWeight = 0.6f,
            MinAmount = 1,
            MaxAmount = 3,
            ValidTypes = ItemType.Ring
        },
        ["resistance_fire"] = new PropertyMetadata
        {
            TypeId = "resistance_fire",
            IntroFloor = 5,
            SpawnWeight = 0.5f,
            MinAmount = 50,
            MaxAmount = 50,
            ValidTypes = ItemType.Ring
        },
        ["resistance_cold"] = new PropertyMetadata
        {
            TypeId = "resistance_cold",
            IntroFloor = 5,
            SpawnWeight = 0.5f,
            MinAmount = 50,
            MaxAmount = 50,
            ValidTypes = ItemType.Ring
        },
        ["resistance_poison"] = new PropertyMetadata
        {
            TypeId = "resistance_poison",
            IntroFloor = 4,
            SpawnWeight = 0.6f,
            MinAmount = 50,
            MaxAmount = 50,
            ValidTypes = ItemType.Ring
        },
        ["see_invisible"] = new PropertyMetadata
        {
            TypeId = "see_invisible",
            IntroFloor = 5,
            SpawnWeight = 0.5f,
            MinAmount = 1,
            MaxAmount = 1,
            ValidTypes = ItemType.Ring
        },
        ["free_action"] = new PropertyMetadata
        {
            TypeId = "free_action",
            IntroFloor = 6,
            SpawnWeight = 0.4f,
            MinAmount = 1,
            MaxAmount = 1,
            ValidTypes = ItemType.Ring
        },

        // Ammo properties (elemental, bodkin, silver only)
        ["bodkin"] = new PropertyMetadata
        {
            TypeId = "bodkin",
            IntroFloor = 3,
            SpawnWeight = 0.4f,
            MinAmount = 2,
            MaxAmount = 3,
            ValidTypes = ItemType.Ammo
        },

        // Wand properties
        ["plenty"] = new PropertyMetadata
        {
            TypeId = "plenty",
            IntroFloor = 3,
            SpawnWeight = 0.15f,
            MinAmount = 2,
            MaxAmount = 2,
            ValidTypes = ItemType.Wand,
            IsSpawnTimeModifier = true
        },

        // Staff properties
        ["fast_recharge"] = new PropertyMetadata
        {
            TypeId = "fast_recharge",
            IntroFloor = 4,
            SpawnWeight = 0.2f,
            MinAmount = 25,
            MaxAmount = 50,
            ValidTypes = ItemType.Staff
        },
        ["range"] = new PropertyMetadata
        {
            TypeId = "range",
            IntroFloor = 3,
            SpawnWeight = 0.2f,
            MinAmount = 1,
            MaxAmount = 2,
            ValidTypes = ItemType.Staff
        },
        ["capacity"] = new PropertyMetadata
        {
            TypeId = "capacity",
            IntroFloor = 4,
            SpawnWeight = 0.15f,
            MinAmount = 2,
            MaxAmount = 4,
            ValidTypes = ItemType.Staff,
            IsSpawnTimeModifier = true
        }
    };

    /// <summary>
    /// Converts a string item type to the ItemType enum.
    /// </summary>
    public static ItemType ParseItemType(string? typeString)
    {
        if (string.IsNullOrEmpty(typeString))
            return ItemType.None;

        return typeString.ToLower() switch
        {
            "weapon" => ItemType.Weapon,
            "armor" => ItemType.Armor,
            "ammo" => ItemType.Ammo,
            "ring" => ItemType.Ring,
            "wand" => ItemType.Wand,
            "staff" => ItemType.Staff,
            _ => ItemType.None
        };
    }

    /// <summary>
    /// Converts an equipment slot name to the ItemType enum.
    /// </summary>
    public static ItemType ParseSlotType(string? slotName)
    {
        if (string.IsNullOrEmpty(slotName))
            return ItemType.None;

        return slotName.ToLower() switch
        {
            "melee" or "meleeweapon" => ItemType.Weapon,
            "ranged" or "rangedweapon" => ItemType.Weapon,
            "armor" => ItemType.Armor,
            "ammo" => ItemType.Ammo,
            "ring" or "ring1" or "ring2" => ItemType.Ring,
            _ => ItemType.None
        };
    }

    /// <summary>
    /// Gets all properties eligible for spawning on the given floor and item type.
    /// </summary>
    public static List<PropertyMetadata> GetEligibleProperties(int floor, ItemType itemType)
    {
        return PropertyRegistry.Values
            .Where(p => p.IntroFloor <= floor)
            .Where(p => (p.ValidTypes & itemType) != 0)
            .ToList();
    }

    /// <summary>
    /// Gets all valid property type IDs for a given item type.
    /// </summary>
    public static List<string> GetValidPropertyTypes(ItemType itemType)
    {
        return PropertyRegistry.Values
            .Where(p => (p.ValidTypes & itemType) != 0)
            .Select(p => p.TypeId)
            .ToList();
    }

    /// <summary>
    /// Selects a property using decay-weighted random selection.
    /// Properties further from their intro floor are less likely to be selected.
    /// </summary>
    public static PropertyMetadata? SelectPropertyWithDecay(
        List<PropertyMetadata> eligible,
        int currentFloor,
        RandomNumberGenerator rng,
        float decayRate = 0.05f)
    {
        if (eligible.Count == 0)
            return null;

        // Calculate weights with decay
        var weights = new List<float>();
        float totalWeight = 0f;

        foreach (var prop in eligible)
        {
            int floorsAboveIntro = currentFloor - prop.IntroFloor;
            float decayFactor = 1.0f / (1.0f + floorsAboveIntro * decayRate);
            float weight = decayFactor * prop.SpawnWeight;
            weights.Add(weight);
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return eligible[rng.RandiRange(0, eligible.Count - 1)];

        // Weighted random selection
        float roll = rng.Randf() * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < eligible.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return eligible[i];
        }

        return eligible[^1];
    }

    /// <summary>
    /// Creates a property instance from metadata with a random amount in range.
    /// </summary>
    public static ItemProperty? CreateFromMetadata(PropertyMetadata metadata, RandomNumberGenerator rng)
    {
        int amount = rng.RandiRange(metadata.MinAmount, metadata.MaxAmount);
        return Create(metadata.TypeId, amount, "permanent", "spawn");
    }

    /// <summary>
    /// Gets the metadata for a property type, or null if not found.
    /// </summary>
    public static PropertyMetadata? GetMetadata(string typeId)
    {
        return PropertyRegistry.TryGetValue(typeId.ToLower(), out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Creates a property instance from a type string.
    /// </summary>
    /// <param name="propertyType">The property type (e.g., "damage", "accuracy", "flaming").</param>
    /// <param name="amount">The amount/magnitude of the property (if applicable).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3", "permanent").</param>
    /// <param name="sourceId">Optional source identifier for tracking property origin.</param>
    /// <returns>A new ItemProperty instance, or null if the type is unknown.</returns>
    public static ItemProperty? Create(
        string? propertyType,
        int amount = 0,
        string duration = "permanent",
        string? sourceId = null)
    {
        if (string.IsNullOrEmpty(propertyType))
        {
            GD.PrintErr("ItemPropertyFactory: propertyType is null or empty");
            return null;
        }

        ItemProperty? property = propertyType.ToLower() switch
        {
            // Weapon enhancement (+X hit and damage)
            "weapon_enhancement" => new WeaponEnhancementProperty(amount > 0 ? amount : 1, duration, sourceId),

            // Armor enhancement (+X armor and reduces evasion penalty)
            "armor_enhancement" => new ArmorEnhancementProperty(amount > 0 ? amount : 1, duration, sourceId),

            // Elemental properties (all use ElementalProperty with different damage types)
            "flaming" => new ElementalProperty(DamageType.Fire, amount, duration, sourceId),
            "freezing" => new ElementalProperty(DamageType.Cold, amount, duration, sourceId),
            "electrified" => new ElementalProperty(DamageType.Lightning, amount, duration, sourceId),
            "venomous" => new ElementalProperty(DamageType.Poison, amount, duration, sourceId),

            // On-hit properties
            "vampiric" => new VampiricProperty(amount, duration, sourceId),

            // Resistance properties (amount is percentage: 50 = half damage)
            "resistance_fire" => CreateResistanceProperty(DamageType.Fire, amount, duration, sourceId),
            "resistance_cold" => CreateResistanceProperty(DamageType.Cold, amount, duration, sourceId),
            "resistance_lightning" => CreateResistanceProperty(DamageType.Lightning, amount, duration, sourceId),
            "resistance_poison" => CreateResistanceProperty(DamageType.Poison, amount, duration, sourceId),
            "resistance_necrotic" => CreateResistanceProperty(DamageType.Necrotic, amount, duration, sourceId),
            "resistance_acid" => CreateResistanceProperty(DamageType.Acid, amount, duration, sourceId),
            "resistance_slashing" => CreateResistanceProperty(DamageType.Slashing, amount, duration, sourceId),
            "resistance_piercing" => CreateResistanceProperty(DamageType.Piercing, amount, duration, sourceId),
            "resistance_bludgeoning" => CreateResistanceProperty(DamageType.Bludgeoning, amount, duration, sourceId),

            // On-damaged properties
            "thorns" => CreateThornsProperty(amount, duration, sourceId),

            // Ring stat bonus properties
            "armor" => CreateStatBonusProperty(StatBonusType.Armor, amount, duration, sourceId),
            "evasion" => CreateStatBonusProperty(StatBonusType.Evasion, amount, duration, sourceId),
            "regen" => CreateStatBonusProperty(StatBonusType.Regen, amount, duration, sourceId),
            "max_health" => CreateStatBonusProperty(StatBonusType.MaxHealth, amount, duration, sourceId),

            // Ammo properties
            "bodkin" => new BodkinProperty(amount > 0 ? amount : 2),
            "silver" => new SilverProperty(),

            // Ring-specific properties
            "see_invisible" => new SeeInvisibleProperty(),
            "free_action" => new FreeActionProperty(),

            // Wand properties
            "plenty" => new PlentyProperty(amount > 0 ? amount : 2),

            // Staff properties
            "fast_recharge" => new FastRechargeProperty(amount > 0 ? amount : 25),
            "range" => new RangeProperty(amount > 0 ? amount : 2),
            "capacity" => new CapacityProperty(amount > 0 ? amount : 2),

            _ => null
        };

        if (property == null)
        {
            GD.PrintErr($"ItemPropertyFactory: Unknown property type '{propertyType}'");
        }

        return property;
    }

    /// <summary>
    /// Checks if a property type string is valid and recognized.
    /// </summary>
    public static bool IsValidType(string? propertyType)
    {
        if (string.IsNullOrEmpty(propertyType))
            return false;

        return propertyType.ToLower() switch
        {
            // Weapon properties
            "weapon_enhancement" => true,
            "flaming" => true,
            "freezing" => true,
            "electrified" => true,
            "venomous" => true,
            "vampiric" => true,
            "silver" => true,
            // Armor properties
            "armor_enhancement" => true,
            // Resistance properties (ring only)
            "resistance_fire" => true,
            "resistance_cold" => true,
            "resistance_lightning" => true,
            "resistance_poison" => true,
            "resistance_necrotic" => true,
            "resistance_acid" => true,
            "resistance_slashing" => true,
            "resistance_piercing" => true,
            "resistance_bludgeoning" => true,
            // On-damaged properties
            "thorns" => true,
            // Ring stat bonus properties
            "armor" => true,
            "evasion" => true,
            "regen" => true,
            "max_health" => true,
            // Ammo properties
            "bodkin" => true,
            // Ring-specific properties
            "see_invisible" => true,
            "free_action" => true,
            // Wand properties
            "plenty" => true,
            // Staff properties
            "fast_recharge" => true,
            "range" => true,
            "capacity" => true,
            _ => false
        };
    }

    /// <summary>
    /// Creates a resistance property with amount as percentage reduction.
    /// </summary>
    private static ResistanceProperty CreateResistanceProperty(DamageType damageType, int amount, string duration, string? sourceId)
    {
        // Amount is percentage: 50 = half damage (0.5 multiplier), 75 = 25% damage (0.25 multiplier)
        float multiplier = amount > 0 ? (100f - amount) / 100f : 0.5f;
        var property = new ResistanceProperty(damageType, multiplier)
        {
            Duration = duration,
            SourceId = sourceId
        };
        if (property.IsTemporary)
        {
            property.RemainingTurns = property.ResolveDuration();
        }
        return property;
    }

    /// <summary>
    /// Creates a thorns property with the specified damage amount.
    /// </summary>
    private static ThornsProperty CreateThornsProperty(int amount, string duration, string? sourceId)
    {
        var property = new ThornsProperty(amount > 0 ? amount : 1)
        {
            Duration = duration,
            SourceId = sourceId
        };
        if (property.IsTemporary)
        {
            property.RemainingTurns = property.ResolveDuration();
        }
        return property;
    }

    /// <summary>
    /// Creates a stat bonus property with the specified bonus type and amount.
    /// </summary>
    private static StatBonusProperty CreateStatBonusProperty(StatBonusType bonusType, int amount, string duration, string? sourceId)
    {
        var property = new StatBonusProperty(bonusType, amount > 0 ? amount : 1)
        {
            Duration = duration,
            SourceId = sourceId
        };
        if (property.IsTemporary)
        {
            property.RemainingTurns = property.ResolveDuration();
        }
        return property;
    }
}
