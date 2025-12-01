using Godot;
using PitsOfDespair.ItemProperties;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Data;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that applies a property to the caster's equipped weapon.
/// Used for weapon enchantment spells, oils, etc.
/// </summary>
public class ApplyPropertyStep : IEffectStep
{
    private readonly string? _propertyType;
    private readonly string _targetSlot;
    private readonly int _amount;
    private readonly string _duration;

    public ApplyPropertyStep(StepDefinition definition)
    {
        _propertyType = definition.PropertyType;
        _targetSlot = definition.TargetSlot?.ToLower() ?? "melee";
        _amount = definition.Amount;
        _duration = definition.GetDurationString();
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        if (string.IsNullOrEmpty(_propertyType))
        {
            GD.PrintErr("ApplyPropertyStep: No property type specified");
            return;
        }

        // Get the target item from caster's equipment
        var item = GetTargetItem(context.Caster, _targetSlot);
        if (item == null)
        {
            messages.Add($"No weapon equipped to enchant!", Palette.ToHex(Palette.Danger));
            return;
        }

        // Create the property
        var property = ItemPropertyFactory.Create(_propertyType, _amount, _duration);
        if (property == null)
        {
            GD.PrintErr($"ApplyPropertyStep: Unknown property type '{_propertyType}'");
            return;
        }

        // Apply the property to the item
        var propertyMessage = item.AddProperty(property);

        // Add message if one was returned
        if (!string.IsNullOrEmpty(propertyMessage.Message))
        {
            messages.Add(propertyMessage.Message, propertyMessage.Color);
        }

        state.Success = true;
    }

    /// <summary>
    /// Gets the ItemInstance for the specified equipment slot.
    /// </summary>
    private static Data.ItemInstance? GetTargetItem(Entities.BaseEntity caster, string slotType)
    {
        var equipComponent = caster.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null) return null;

        var slot = slotType switch
        {
            "melee" => EquipmentSlot.MeleeWeapon,
            "ranged" => EquipmentSlot.RangedWeapon,
            _ => EquipmentSlot.MeleeWeapon
        };

        var inventoryKey = equipComponent.GetEquippedKey(slot);
        if (inventoryKey == null) return null;

        var inventoryComponent = caster.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventoryComponent == null) return null;

        var inventorySlot = inventoryComponent.GetSlot(inventoryKey.Value);
        return inventorySlot?.Item;
    }
}
