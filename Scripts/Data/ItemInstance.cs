using Godot;

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
    /// Only relevant for items with MaxCharges > 0.
    /// </summary>
    public int CurrentCharges { get; set; }

    /// <summary>
    /// Turn counter for recharging. Increments each turn and resets when reaching RechargeTurns.
    /// </summary>
    public int RechargeTurnCounter { get; set; }

    public ItemInstance(ItemData template)
    {
        Template = template;
        RechargeTurnCounter = 0;

        // Initialize charges based on template configuration
        if (template.Charges > 0)
        {
            // Explicit charges specified in YAML - use that value
            CurrentCharges = template.Charges;
        }
        else if (template.MaxCharges > 0)
        {
            // No explicit charges - randomize between 1 and MaxCharges
            CurrentCharges = GD.RandRange(1, template.MaxCharges);
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
        if (Template.RechargeTurns <= 0 || CurrentCharges >= Template.MaxCharges)
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
        if (Template.RechargeTurns <= 0 || CurrentCharges >= Template.MaxCharges)
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
}
