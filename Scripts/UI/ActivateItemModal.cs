using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a selection menu for activating (using) items from inventory.
/// Shows items with their keys, greying out non-activatable items.
/// </summary>
public partial class ActivateItemModal : ItemSelectionModal
{
    protected override void UpdateDisplay()
    {
        if (_itemsLabel == null || _player == null)
        {
            return;
        }

        var inventory = _player.Inventory;

        if (inventory.Count == 0)
        {
            ShowEmptyMessage("Activate which item?");
            return;
        }

        var sb = new StringBuilder();
        sb.Append(BuildHeader("Activate which item?"));

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
        var skillComponent = _player.GetNodeOrNull<SkillComponent>("SkillComponent");
        bool hasAttunement = skillComponent?.HasSkill("attunement") ?? false;

        foreach (var slot in inventory.OrderBy(s => s.Key))
        {
            // Check if item is equipped
            bool isEquipped = equipComponent != null && equipComponent.IsEquipped(slot.Key);

            // Check if item is activatable
            // - Consumables and charged items: must not be equipped
            // - Reach weapons (melee with range > 1): can be equipped
            bool isActivatable;
            if (isEquipped)
            {
                // Equipped items can only be activated if they're reach weapons
                isActivatable = slot.Item.Template.Attack != null &&
                               slot.Item.Template.Attack.Type == AttackType.Melee &&
                               slot.Item.Template.Attack.Range > 1;
            }
            else
            {
                // Non-equipped items: activatable if consumable or have charges
                isActivatable = slot.Item.Template.IsActivatable() &&
                               (slot.Item.Template.GetIsConsumable() || slot.Item.CurrentCharges > 0);
            }

            // Determine display options
            var options = ItemDisplayOptions.ShowCount | ItemDisplayOptions.ShowSlot | ItemDisplayOptions.ShowEquipped;
            if (hasAttunement)
            {
                options |= ItemDisplayOptions.ShowChargeBrackets;
            }
            if (!isActivatable)
            {
                options |= ItemDisplayOptions.IsDisabled;
            }

            string line = ItemFormatter.FormatItemLine(slot, options, equipComponent);
            sb.AppendLine(line);
        }

        _itemsLabel.Text = sb.ToString();
    }
}
