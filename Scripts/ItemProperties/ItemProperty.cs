using System;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Item types that properties can apply to. Uses flags for many-to-many mapping.
/// </summary>
[Flags]
public enum ItemType
{
    None = 0,
    Weapon = 1 << 0,
    Armor = 1 << 1,
    Ammo = 1 << 2,
    Ring = 1 << 3,
    Wand = 1 << 4,
    Staff = 1 << 5,

    // Convenience groups
    ChargedItems = Wand | Staff,
    Equipment = Weapon | Armor | Ring,
    Defensive = Armor | Ring
}

/// <summary>
/// Represents a property message with associated color.
/// </summary>
public readonly struct PropertyMessage
{
    public string Message { get; init; }
    public string Color { get; init; }

    public PropertyMessage(string message, string color)
    {
        Message = message;
        Color = color;
    }

    public static PropertyMessage Empty => new(string.Empty, Palette.ToHex(Palette.Default));
}

/// <summary>
/// Interface for properties that provide flat damage bonuses.
/// </summary>
public interface IDamageProperty
{
    int GetDamageBonus();
}

/// <summary>
/// Interface for properties that provide hit/accuracy bonuses.
/// </summary>
public interface IHitProperty
{
    int GetHitBonus();
}

/// <summary>
/// Result of an on-hit property effect.
/// </summary>
public readonly struct OnHitResult
{
    public int DamageDealt { get; init; }
    public string DamageType { get; init; }
    public int HealingDone { get; init; }
    public string? Verb { get; init; }           // e.g., "scorched", "shocked", "frozen"
    public string? MessageColor { get; init; }

    public static OnHitResult None => new() { DamageDealt = 0, DamageType = "", HealingDone = 0 };
}

/// <summary>
/// Interface for properties that trigger effects when the weapon hits.
/// </summary>
public interface IOnHitProperty
{
    OnHitResult OnHit(BaseEntity attacker, BaseEntity target, int damage);
}

/// <summary>
/// Result of an on-damaged property effect.
/// </summary>
public readonly struct OnDamagedResult
{
    public int ReflectedDamage { get; init; }
    public string? DamageType { get; init; }
    public string? Verb { get; init; }           // e.g., "retaliated", "reflected"
    public string? MessageColor { get; init; }

    public static OnDamagedResult None => new() { ReflectedDamage = 0, DamageType = null };
}

/// <summary>
/// Interface for properties that trigger effects when the wearer takes damage.
/// Used for reactive armor effects like thorns, damage reflection, etc.
/// </summary>
public interface IOnDamagedProperty
{
    OnDamagedResult OnDamaged(BaseEntity wearer, BaseEntity attacker, int damage, DamageType damageType);
}

/// <summary>
/// Interface for properties that provide resistance or vulnerability to damage types.
/// Multiplier less than 1.0 = resistance, greater than 1.0 = vulnerability.
/// </summary>
public interface IResistanceProperty
{
    bool AppliesToDamageType(DamageType damageType);
    float GetDamageMultiplier();
}

/// <summary>
/// Stat bonus types that can be provided by item properties.
/// </summary>
public enum StatBonusType
{
    Armor,
    Evasion,
    Regen,
    MaxHealth
}

/// <summary>
/// Interface for properties that provide stat bonuses to the wearer.
/// </summary>
public interface IStatBonusProperty
{
    StatBonusType BonusType { get; }
    int GetBonus();
}

/// <summary>
/// Interface for properties that provide passive abilities while equipped.
/// </summary>
public interface IPassiveAbilityProperty
{
    string AbilityId { get; }
}

/// <summary>
/// Interface for properties that provide immunity to specific conditions.
/// </summary>
public interface IConditionImmunityProperty
{
    bool PreventsCondition(string conditionTypeId);
}

/// <summary>
/// Interface for properties that pierce armor.
/// </summary>
public interface IArmorPiercingProperty
{
    int GetArmorPiercing();
}

/// <summary>
/// Interface for properties that bypass damage type resistances.
/// </summary>
public interface IBypassResistanceProperty
{
    bool BypassesResistance(DamageType damageType);
}

/// <summary>
/// Interface for properties that modify recharge rate on charged items.
/// </summary>
public interface IRechargeModifierProperty
{
    float GetRechargeMultiplier();
}

/// <summary>
/// Interface for properties that modify effect power/damage.
/// </summary>
public interface IEffectPowerProperty
{
    float GetDamageMultiplier();
}

/// <summary>
/// Base class for all item properties.
/// Properties are enhancements attached to items that modify their behavior or stats.
/// Unlike conditions (which affect entities), properties describe the item itself.
/// </summary>
public abstract class ItemProperty
{
    /// <summary>
    /// Display name of this property (e.g., "Flaming", "Vampiric").
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Type identifier for this property (used for non-stacking logic).
    /// Properties with the same TypeId will not stack on the same item.
    /// </summary>
    public abstract string TypeId { get; }

    /// <summary>
    /// Item types this property can be applied to.
    /// Uses ItemType flags for many-to-many mapping.
    /// </summary>
    public abstract ItemType ValidItemTypes { get; }

    /// <summary>
    /// Checks if this property can be applied to the given item type.
    /// </summary>
    public bool CanApplyTo(ItemType itemType) => (ValidItemTypes & itemType) != 0;

    /// <summary>
    /// Optional source identifier for tracking property origin.
    /// Used to identify which system/effect applied this property.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Duration of this property as dice notation (e.g., "10", "2d3", "permanent").
    /// Only used for temporary properties.
    /// </summary>
    public string Duration { get; set; } = "permanent";

    /// <summary>
    /// Remaining turns before this property expires.
    /// Only decremented for temporary properties.
    /// </summary>
    public int RemainingTurns { get; set; }

    /// <summary>
    /// Whether this property has a limited duration.
    /// </summary>
    public bool IsTemporary => Duration != "permanent";

    /// <summary>
    /// Resolves the duration by rolling dice notation.
    /// Called when adding this property to an item.
    /// </summary>
    public int ResolveDuration()
    {
        if (!IsTemporary)
            return 0;
        return DiceRoller.Roll(Duration);
    }

    /// <summary>
    /// Called when this property is first applied to an item.
    /// </summary>
    /// <param name="item">The item receiving the property.</param>
    /// <returns>PropertyMessage with text and color, or PropertyMessage.Empty for no message.</returns>
    public virtual PropertyMessage OnApplied(ItemInstance item)
    {
        return PropertyMessage.Empty;
    }

    /// <summary>
    /// Called when this property is removed from an item.
    /// </summary>
    /// <param name="item">The item losing the property.</param>
    /// <returns>PropertyMessage with text and color, or PropertyMessage.Empty for no message.</returns>
    public virtual PropertyMessage OnRemoved(ItemInstance item)
    {
        return PropertyMessage.Empty;
    }

    /// <summary>
    /// Called each round while this property is active.
    /// Used for temporary properties to decrement duration.
    /// </summary>
    /// <param name="item">The item with this property.</param>
    public virtual void OnTurnProcessed(ItemInstance item)
    {
        // Default: do nothing each turn
    }

    /// <summary>
    /// Gets the prefix to prepend to item name (e.g., "Flaming", "+2").
    /// Return null for no prefix.
    /// </summary>
    public virtual string? GetPrefix() => null;

    /// <summary>
    /// Gets the suffix to append to item name (e.g., "of slaying").
    /// Return null for no suffix.
    /// </summary>
    public virtual string? GetSuffix() => null;

    /// <summary>
    /// Gets a color override for items with this property.
    /// Used primarily for rings to set their display color.
    /// Return null to use default item color.
    /// </summary>
    public virtual Color? GetColorOverride() => null;

    /// <summary>
    /// Refreshes the remaining turns of this property.
    /// Used when the same property type is applied again.
    /// Only extends if the new duration is longer than remaining.
    /// </summary>
    /// <param name="newDuration">The resolved duration to compare/set.</param>
    public void RefreshDuration(int newDuration)
    {
        if (newDuration > RemainingTurns)
        {
            RemainingTurns = newDuration;
        }
    }
}
