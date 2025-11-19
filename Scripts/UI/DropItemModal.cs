using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a selection menu for dropping items from inventory.
/// Shows all items with their keys for selection.
/// </summary>
public partial class DropItemModal : ItemSelectionModal
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
            ShowEmptyMessage("Drop which item?");
            return;
        }

        var sb = new StringBuilder();
        sb.Append(BuildHeader("Drop which item?"));

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");

        foreach (var slot in inventory.OrderBy(s => s.Key))
        {
            var options = ItemDisplayOptions.ShowCount | ItemDisplayOptions.ShowSlot | ItemDisplayOptions.ShowEquipped;
            string line = ItemFormatter.FormatItemLine(slot, options, equipComponent);
            sb.AppendLine(line);
        }

        _itemsLabel.Text = sb.ToString();
    }
}
