using PitsOfDespair.Entities;
using PitsOfDespair.Components;
using PitsOfDespair.Data;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for equipping an item from inventory into an equipment slot.
/// Automatically detects the appropriate slot based on the item's properties.
/// </summary>
public class EquipAction : Action
{
    private readonly char _itemKey;

    public override string Name => "Equip";

    public EquipAction(char itemKey)
    {
        _itemKey = itemKey;
    }

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || context == null)
        {
            return false;
        }

        // Check if the actor has an EquipComponent
        var equipComponent = actor.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null)
        {
            return false;
        }

        // Only players can equip from inventory (for now)
        if (actor is not Player player)
        {
            return false;
        }

        // Check if the item exists in inventory
        var slot = player.GetInventorySlot(_itemKey);
        if (slot == null)
        {
            return false;
        }

        // Check if the item is equippable
        var itemTemplate = slot.Item.Template;
        if (!itemTemplate.GetIsEquippable())
        {
            return false;
        }

        // Check if the item has a valid equipment slot
        var equipSlot = itemTemplate.GetEquipmentSlot();
        if (equipSlot == EquipmentSlot.None)
        {
            return false;
        }

        return true;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        var equipComponent = actor.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null)
        {
            return ActionResult.CreateFailure("You cannot equip items.");
        }

        if (actor is not Player player)
        {
            return ActionResult.CreateFailure("Only the player can equip items.");
        }

        // Get the inventory slot
        var slot = player.GetInventorySlot(_itemKey);
        if (slot == null)
        {
            return ActionResult.CreateFailure("No item in that slot.");
        }

        var itemTemplate = slot.Item.Template;

        // Check if the item is equippable
        if (!itemTemplate.GetIsEquippable())
        {
            return ActionResult.CreateFailure($"You can't equip {itemTemplate.Name}.");
        }

        // Get the equipment slot for this item (handles rings dynamically)
        var equipSlot = equipComponent.GetSlotForItem(itemTemplate);
        if (equipSlot == EquipmentSlot.None)
        {
            return ActionResult.CreateFailure($"{itemTemplate.Name} has no valid equipment slot.");
        }

        // Check if already equipped - if so, unequip it instead
        if (equipComponent.IsEquipped(_itemKey))
        {
            var currentSlot = equipComponent.GetEquippedSlotForItem(_itemKey);
            if (currentSlot != EquipmentSlot.None && equipComponent.Unequip(currentSlot))
            {
                player.EmitSignal(Player.SignalName.ItemUnequipped, itemTemplate.GetDisplayName(1), itemTemplate.GetGlyph(), itemTemplate.Color);
                return ActionResult.CreateSuccess($"Unequipped {itemTemplate.GetDisplayName(1)}.");
            }
            return ActionResult.CreateFailure($"Failed to unequip {itemTemplate.Name}.");
        }

        // Attempt to equip the item
        bool success = equipComponent.Equip(_itemKey, equipSlot);
        if (!success)
        {
            return ActionResult.CreateFailure($"Failed to equip {itemTemplate.Name}.");
        }

        // Emit equipment signal for UI feedback
        player.EmitSignal(Player.SignalName.ItemEquipped, itemTemplate.GetDisplayName(1), itemTemplate.GetGlyph(), itemTemplate.Color);

        return ActionResult.CreateSuccess($"Equipped {itemTemplate.GetDisplayName(1)}.");
    }
}
