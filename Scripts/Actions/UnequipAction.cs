using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for unequipping an item from an equipment slot back to inventory.
/// </summary>
public class UnequipAction : Action
{
    private readonly EquipmentSlot _slot;

    public override string Name => "Unequip";

    public UnequipAction(EquipmentSlot slot)
    {
        _slot = slot;
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

        // Check if the slot is occupied
        var equippedKey = equipComponent.GetEquippedKey(_slot);
        if (equippedKey == null)
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
            return ActionResult.CreateFailure("You cannot unequip items.");
        }

        // Get the equipped item
        var equippedKey = equipComponent.GetEquippedKey(_slot);
        if (equippedKey == null)
        {
            return ActionResult.CreateFailure($"No item equipped in {_slot} slot.");
        }

        // Get item info for feedback (if player)
        string itemName = "item";
        string itemGlyph = "?";
        string itemColor = "#FFFFFF";
        if (actor is Player player)
        {
            var slot = player.GetInventorySlot(equippedKey.Value);
            if (slot != null)
            {
                itemName = slot.Item.Template.Name;
                itemGlyph = slot.Item.Template.GetGlyph();
                itemColor = slot.Item.Template.Color;
            }
        }

        // Attempt to unequip the item
        bool success = equipComponent.Unequip(_slot);
        if (!success)
        {
            return ActionResult.CreateFailure($"Failed to unequip {itemName}.");
        }

        // Emit unequipment signal for UI feedback (if player)
        if (actor is Player player2)
        {
            player2.EmitSignal(Player.SignalName.ItemUnequipped, itemName, itemGlyph, itemColor);
        }

        return ActionResult.CreateSuccess($"Unequipped {itemName}.");
    }
}
