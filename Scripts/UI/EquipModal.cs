using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a selection menu for equipping items from inventory.
/// Shows items with their keys, greying out non-equippable items.
/// </summary>
public partial class EquipModal : ItemSelectionModal
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
            ShowEmptyMessage("Equip which item?");
            return;
        }

        var sb = new StringBuilder();
        sb.Append(BuildHeader("Equip which item?"));

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");

        foreach (var slot in inventory.OrderBy(s => s.Key))
        {
            var itemTemplate = slot.Item.Template;
            bool isEquippable = itemTemplate.GetIsEquippable();

            // Determine display options
            var options = ItemDisplayOptions.ShowCount | ItemDisplayOptions.ShowSlot | ItemDisplayOptions.ShowEquipped;
            if (!isEquippable)
            {
                options |= ItemDisplayOptions.IsDisabled;
            }

            string line = ItemFormatter.FormatItemLine(slot, options, equipComponent);
            sb.AppendLine(line);
        }

        _itemsLabel.Text = sb.ToString();
    }
}
