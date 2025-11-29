using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Brands;

/// <summary>
/// Represents a brand message with associated color.
/// </summary>
public readonly struct BrandMessage
{
    public string Message { get; init; }
    public string Color { get; init; }

    public BrandMessage(string message, string color)
    {
        Message = message;
        Color = color;
    }

    public static BrandMessage Empty => new(string.Empty, Palette.ToHex(Palette.Default));
}

/// <summary>
/// Interface for brands that provide flat damage bonuses.
/// </summary>
public interface IDamageBrand
{
    int GetDamageBonus();
}

/// <summary>
/// Interface for brands that provide hit/accuracy bonuses.
/// </summary>
public interface IHitBrand
{
    int GetHitBonus();
}

/// <summary>
/// Result of an on-hit brand effect.
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
/// Interface for brands that trigger effects when the weapon hits.
/// </summary>
public interface IOnHitBrand
{
    OnHitResult OnHit(BaseEntity attacker, BaseEntity target, int damage);
}

/// <summary>
/// Base class for all item brands.
/// Brands are properties attached to items that modify their behavior or stats.
/// Unlike conditions (which affect entities), brands describe the item itself.
/// </summary>
public abstract class Brand
{
    /// <summary>
    /// Display name of this brand (e.g., "Flaming", "Vampiric").
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Type identifier for this brand (used for non-stacking logic).
    /// Brands with the same TypeId will not stack on the same item.
    /// </summary>
    public abstract string TypeId { get; }

    /// <summary>
    /// Optional source identifier for tracking brand origin.
    /// Used to identify which system/effect applied this brand.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Duration of this brand as dice notation (e.g., "10", "2d3", "permanent").
    /// Only used for temporary brands.
    /// </summary>
    public string Duration { get; set; } = "permanent";

    /// <summary>
    /// Remaining turns before this brand expires.
    /// Only decremented for temporary brands.
    /// </summary>
    public int RemainingTurns { get; set; }

    /// <summary>
    /// Whether this brand has a limited duration.
    /// </summary>
    public bool IsTemporary => Duration != "permanent";

    /// <summary>
    /// Resolves the duration by rolling dice notation.
    /// Called when adding this brand to an item.
    /// </summary>
    public int ResolveDuration()
    {
        if (!IsTemporary)
            return 0;
        return DiceRoller.Roll(Duration);
    }

    /// <summary>
    /// Called when this brand is first applied to an item.
    /// </summary>
    /// <param name="item">The item receiving the brand.</param>
    /// <returns>BrandMessage with text and color, or BrandMessage.Empty for no message.</returns>
    public virtual BrandMessage OnApplied(ItemInstance item)
    {
        return BrandMessage.Empty;
    }

    /// <summary>
    /// Called when this brand is removed from an item.
    /// </summary>
    /// <param name="item">The item losing the brand.</param>
    /// <returns>BrandMessage with text and color, or BrandMessage.Empty for no message.</returns>
    public virtual BrandMessage OnRemoved(ItemInstance item)
    {
        return BrandMessage.Empty;
    }

    /// <summary>
    /// Called each round while this brand is active.
    /// Used for temporary brands to decrement duration.
    /// </summary>
    /// <param name="item">The item with this brand.</param>
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
    /// Gets the suffix to append to item name (e.g., "of Slaying").
    /// Return null for no suffix.
    /// </summary>
    public virtual string? GetSuffix() => null;

    /// <summary>
    /// Refreshes the remaining turns of this brand.
    /// Used when the same brand type is applied again.
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
