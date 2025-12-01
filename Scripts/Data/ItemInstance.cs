using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using PitsOfDespair.ItemProperties;
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
    /// Properties applied to this item instance.
    /// </summary>
    private readonly List<ItemProperty> _properties = new();

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

        // Clone properties
        foreach (var property in _properties)
        {
            // Create a new property of the same type with the same values
            var clonedProperty = ItemPropertyFactory.Create(
                property.TypeId,
                GetPropertyAmount(property),
                property.Duration,
                property.SourceId
            );
            if (clonedProperty != null)
            {
                clonedProperty.RemainingTurns = property.RemainingTurns;
                clone._properties.Add(clonedProperty);
            }
        }

        return clone;
    }

    #region Property Management

    /// <summary>
    /// Adds a property to this item. If a property of the same type exists, refreshes its duration.
    /// </summary>
    /// <param name="property">The property to add.</param>
    /// <returns>Message describing what happened.</returns>
    public PropertyMessage AddProperty(ItemProperty property)
    {
        // Check for existing property of same type
        var existing = _properties.FirstOrDefault(p => p.TypeId == property.TypeId);
        if (existing != null)
        {
            // Refresh duration instead of stacking
            int newDuration = property.ResolveDuration();
            existing.RefreshDuration(newDuration);
            return new PropertyMessage($"{Template.Name}'s {property.Name} property refreshed.", Palette.ToHex(Palette.StatusBuff));
        }

        // Resolve duration for new property
        if (property.IsTemporary)
        {
            property.RemainingTurns = property.ResolveDuration();
        }

        _properties.Add(property);
        return property.OnApplied(this);
    }

    /// <summary>
    /// Removes a specific property from this item.
    /// </summary>
    /// <param name="property">The property to remove.</param>
    /// <returns>Message describing the removal.</returns>
    public PropertyMessage RemoveProperty(ItemProperty property)
    {
        if (_properties.Remove(property))
        {
            return property.OnRemoved(this);
        }
        return PropertyMessage.Empty;
    }

    /// <summary>
    /// Removes a property by its type ID.
    /// </summary>
    /// <param name="typeId">The type ID of the property to remove.</param>
    /// <returns>Message describing the removal.</returns>
    public PropertyMessage RemovePropertyByType(string typeId)
    {
        var property = _properties.FirstOrDefault(p => p.TypeId == typeId);
        if (property != null)
        {
            return RemoveProperty(property);
        }
        return PropertyMessage.Empty;
    }

    /// <summary>
    /// Checks if this item has a property of the specified type.
    /// </summary>
    public bool HasProperty(string typeId)
    {
        return _properties.Any(p => p.TypeId == typeId);
    }

    /// <summary>
    /// Gets all properties on this item.
    /// </summary>
    public IReadOnlyList<ItemProperty> GetProperties()
    {
        return _properties.AsReadOnly();
    }

    /// <summary>
    /// Processes turn for all properties (decrements temporary property durations).
    /// Called at the end of each round.
    /// </summary>
    /// <returns>List of messages from expired properties.</returns>
    public List<PropertyMessage> ProcessPropertyTurns()
    {
        var messages = new List<PropertyMessage>();
        var expiredProperties = new List<ItemProperty>();

        foreach (var property in _properties)
        {
            property.OnTurnProcessed(this);

            if (property.IsTemporary)
            {
                property.RemainingTurns--;
                if (property.RemainingTurns <= 0)
                {
                    expiredProperties.Add(property);
                }
            }
        }

        // Remove expired properties
        foreach (var property in expiredProperties)
        {
            var msg = RemoveProperty(property);
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
    /// Gets total damage bonus from all properties implementing IDamageProperty.
    /// </summary>
    public int GetTotalDamageBonus()
    {
        return _properties
            .OfType<IDamageProperty>()
            .Sum(p => p.GetDamageBonus());
    }

    /// <summary>
    /// Gets total hit bonus from all properties implementing IHitProperty.
    /// </summary>
    public int GetTotalHitBonus()
    {
        return _properties
            .OfType<IHitProperty>()
            .Sum(p => p.GetHitBonus());
    }

    /// <summary>
    /// Gets all on-hit properties for processing during combat.
    /// </summary>
    public IEnumerable<IOnHitProperty> GetOnHitProperties()
    {
        return _properties.OfType<IOnHitProperty>();
    }

    #endregion

    #region Display

    /// <summary>
    /// Gets the display name with property prefixes and suffixes.
    /// </summary>
    public string GetDisplayName()
    {
        if (_properties.Count == 0)
            return Template.Name;

        var prefixes = _properties
            .Select(p => p.GetPrefix())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var suffixes = _properties
            .Select(p => p.GetSuffix())
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

    /// <summary>
    /// Gets a color override from properties, if any property specifies one.
    /// Returns the first property's color override found.
    /// </summary>
    public Godot.Color? GetColorOverride()
    {
        foreach (var property in _properties)
        {
            var color = property.GetColorOverride();
            if (color.HasValue)
                return color;
        }
        return null;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Gets the amount value from a property for cloning purposes.
    /// </summary>
    private static int GetPropertyAmount(ItemProperty property)
    {
        return property switch
        {
            IDamageProperty dp => dp.GetDamageBonus(),
            IHitProperty hp => hp.GetHitBonus(),
            _ => 0
        };
    }

    #endregion
}
