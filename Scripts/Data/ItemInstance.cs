using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using PitsOfDespair.Brands;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Data;

/// <summary>
/// Represents a specific instance of an item with its own state.
/// Wraps the shared ItemData template with per-instance properties.
/// </summary>
public class ItemInstance
{
    /// <summary>
    /// Reference to the shared item template data loaded from YAML.
    /// </summary>
    public ItemData Template { get; set; }

    /// <summary>
    /// Current number of charges for this item instance.
    /// Only relevant for items with charges dice notation set.
    /// </summary>
    public int CurrentCharges { get; set; }

    /// <summary>
    /// Turn counter for recharging. Increments each turn and resets when reaching RechargeTurns.
    /// </summary>
    public int RechargeTurnCounter { get; set; }

    /// <summary>
    /// The quantity/count of this item. Used for stackable items (consumables, ammo).
    /// Defaults to 1 for non-stacking items.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Whether this specific item instance should be automatically picked up.
    /// Initialized from the template, but can be toggled (e.g. when dropped).
    /// </summary>
    public bool AutoPickup { get; set; }

    /// <summary>
    /// Brands applied to this item instance.
    /// </summary>
    private readonly List<Brand> _brands = new();

    public ItemInstance(ItemData template)
    {
        Template = template;
        RechargeTurnCounter = 0;
        Quantity = 1; // Default quantity for all items
        AutoPickup = template.AutoPickup; // Initialize from template default

        // Initialize charges based on template configuration
        if (!string.IsNullOrEmpty(template.ChargesDice))
        {
            // Roll dice for starting charges
            CurrentCharges = DiceRoller.Roll(template.ChargesDice);
        }
        else
        {
            // Not a charged item
            CurrentCharges = 0;
        }
    }

    /// <summary>
    /// Uses one charge from this item.
    /// </summary>
    /// <returns>True if a charge was consumed, false if no charges available.</returns>
    public bool UseCharge()
    {
        if (CurrentCharges <= 0)
        {
            return false;
        }

        CurrentCharges--;
        return true;
    }

    /// <summary>
    /// Recharges this item by one charge if applicable.
    /// </summary>
    /// <returns>True if a charge was added, false if already at max or not rechargeable.</returns>
    public bool Recharge()
    {
        if (Template.RechargeTurns <= 0 || CurrentCharges >= Template.GetMaxCharges())
        {
            return false;
        }

        CurrentCharges++;
        return true;
    }

    /// <summary>
    /// Processes one turn for this item's recharge system.
    /// </summary>
    public void ProcessTurn()
    {
        if (Template.RechargeTurns <= 0 || CurrentCharges >= Template.GetMaxCharges())
        {
            return;
        }

        RechargeTurnCounter++;
        if (RechargeTurnCounter >= Template.RechargeTurns)
        {
            Recharge();
            RechargeTurnCounter = 0;
        }
    }

    /// <summary>
    /// Creates a copy of this ItemInstance with the same template and state.
    /// Used when dropping items from inventory to ensure inventory and ground entities don't share state.
    /// </summary>
    public ItemInstance Clone()
    {
        var clone = new ItemInstance(Template)
        {
            CurrentCharges = this.CurrentCharges,
            RechargeTurnCounter = this.RechargeTurnCounter,
            Quantity = this.Quantity,
            AutoPickup = this.AutoPickup
        };

        // Clone brands
        foreach (var brand in _brands)
        {
            // Create a new brand of the same type with the same properties
            var clonedBrand = BrandFactory.Create(
                brand.TypeId,
                GetBrandAmount(brand),
                brand.Duration,
                brand.SourceId
            );
            if (clonedBrand != null)
            {
                clonedBrand.RemainingTurns = brand.RemainingTurns;
                clone._brands.Add(clonedBrand);
            }
        }

        return clone;
    }

    #region Brand Management

    /// <summary>
    /// Adds a brand to this item. If a brand of the same type exists, refreshes its duration.
    /// </summary>
    /// <param name="brand">The brand to add.</param>
    /// <returns>Message describing what happened.</returns>
    public BrandMessage AddBrand(Brand brand)
    {
        // Check for existing brand of same type
        var existing = _brands.FirstOrDefault(b => b.TypeId == brand.TypeId);
        if (existing != null)
        {
            // Refresh duration instead of stacking
            int newDuration = brand.ResolveDuration();
            existing.RefreshDuration(newDuration);
            return new BrandMessage($"{Template.Name}'s {brand.Name} brand refreshed.", Palette.ToHex(Palette.StatusBuff));
        }

        // Resolve duration for new brand
        if (brand.IsTemporary)
        {
            brand.RemainingTurns = brand.ResolveDuration();
        }

        _brands.Add(brand);
        return brand.OnApplied(this);
    }

    /// <summary>
    /// Removes a specific brand from this item.
    /// </summary>
    /// <param name="brand">The brand to remove.</param>
    /// <returns>Message describing the removal.</returns>
    public BrandMessage RemoveBrand(Brand brand)
    {
        if (_brands.Remove(brand))
        {
            return brand.OnRemoved(this);
        }
        return BrandMessage.Empty;
    }

    /// <summary>
    /// Removes a brand by its type ID.
    /// </summary>
    /// <param name="typeId">The type ID of the brand to remove.</param>
    /// <returns>Message describing the removal.</returns>
    public BrandMessage RemoveBrandByType(string typeId)
    {
        var brand = _brands.FirstOrDefault(b => b.TypeId == typeId);
        if (brand != null)
        {
            return RemoveBrand(brand);
        }
        return BrandMessage.Empty;
    }

    /// <summary>
    /// Checks if this item has a brand of the specified type.
    /// </summary>
    public bool HasBrand(string typeId)
    {
        return _brands.Any(b => b.TypeId == typeId);
    }

    /// <summary>
    /// Gets all brands on this item.
    /// </summary>
    public IReadOnlyList<Brand> GetBrands()
    {
        return _brands.AsReadOnly();
    }

    /// <summary>
    /// Processes turn for all brands (decrements temporary brand durations).
    /// Called at the end of each round.
    /// </summary>
    /// <returns>List of messages from expired brands.</returns>
    public List<BrandMessage> ProcessBrandTurns()
    {
        var messages = new List<BrandMessage>();
        var expiredBrands = new List<Brand>();

        foreach (var brand in _brands)
        {
            brand.OnTurnProcessed(this);

            if (brand.IsTemporary)
            {
                brand.RemainingTurns--;
                if (brand.RemainingTurns <= 0)
                {
                    expiredBrands.Add(brand);
                }
            }
        }

        // Remove expired brands
        foreach (var brand in expiredBrands)
        {
            var msg = RemoveBrand(brand);
            if (!string.IsNullOrEmpty(msg.Message))
            {
                messages.Add(msg);
            }
        }

        return messages;
    }

    #endregion

    #region Combat Queries

    /// <summary>
    /// Gets total damage bonus from all brands implementing IDamageBrand.
    /// </summary>
    public int GetTotalDamageBonus()
    {
        return _brands
            .OfType<IDamageBrand>()
            .Sum(b => b.GetDamageBonus());
    }

    /// <summary>
    /// Gets total hit bonus from all brands implementing IHitBrand.
    /// </summary>
    public int GetTotalHitBonus()
    {
        return _brands
            .OfType<IHitBrand>()
            .Sum(b => b.GetHitBonus());
    }

    /// <summary>
    /// Gets all on-hit brands for processing during combat.
    /// </summary>
    public IEnumerable<IOnHitBrand> GetOnHitBrands()
    {
        return _brands.OfType<IOnHitBrand>();
    }

    #endregion

    #region Display

    /// <summary>
    /// Gets the display name with brand prefixes and suffixes.
    /// </summary>
    public string GetBrandedDisplayName()
    {
        if (_brands.Count == 0)
            return Template.Name;

        var prefixes = _brands
            .Select(b => b.GetPrefix())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var suffixes = _brands
            .Select(b => b.GetSuffix())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var sb = new StringBuilder();

        if (prefixes.Count > 0)
            sb.Append(string.Join(" ", prefixes)).Append(' ');

        sb.Append(Template.Name);

        if (suffixes.Count > 0)
            sb.Append(' ').Append(string.Join(" ", suffixes));

        return sb.ToString();
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Gets the amount value from a brand for cloning purposes.
    /// </summary>
    private static int GetBrandAmount(Brand brand)
    {
        return brand switch
        {
            IDamageBrand db => db.GetDamageBonus(),
            IHitBrand hb => hb.GetHitBonus(),
            _ => 0
        };
    }

    #endregion
}
