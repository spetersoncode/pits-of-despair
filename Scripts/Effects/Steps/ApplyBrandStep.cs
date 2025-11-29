using Godot;
using PitsOfDespair.Brands;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Data;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that applies a brand to the caster's equipped weapon.
/// Used for weapon enchantment spells, oils, etc.
/// </summary>
public class ApplyBrandStep : IEffectStep
{
    private readonly string? _brandType;
    private readonly string _targetSlot;
    private readonly int _amount;
    private readonly string _duration;

    public ApplyBrandStep(StepDefinition definition)
    {
        _brandType = definition.BrandType;
        _targetSlot = definition.TargetSlot?.ToLower() ?? "melee";
        _amount = definition.Amount;
        _duration = definition.GetDurationString();
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        if (string.IsNullOrEmpty(_brandType))
        {
            GD.PrintErr("ApplyBrandStep: No brand type specified");
            return;
        }

        // Get the target item from caster's equipment
        var item = GetTargetItem(context.Caster, _targetSlot);
        if (item == null)
        {
            messages.Add($"No weapon equipped to enchant!", Palette.ToHex(Palette.Danger));
            return;
        }

        // Create the brand
        var brand = BrandFactory.Create(_brandType, _amount, _duration);
        if (brand == null)
        {
            GD.PrintErr($"ApplyBrandStep: Unknown brand type '{_brandType}'");
            return;
        }

        // Apply the brand to the item
        var brandMessage = item.AddBrand(brand);

        // Add message if one was returned
        if (!string.IsNullOrEmpty(brandMessage.Message))
        {
            messages.Add(brandMessage.Message, brandMessage.Color);
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
