using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that provides immunity to paralysis and stun conditions.
/// Ring-only property.
/// </summary>
public class FreeActionProperty : ItemProperty, IConditionImmunityProperty
{
    public override string Name => "free action";
    public override string TypeId => "free_action";
    public override ItemType ValidItemTypes => ItemType.Ring;

    public bool PreventsCondition(string conditionTypeId)
    {
        // Prevents paralysis, stun, and similar movement-impairing conditions
        return conditionTypeId.ToLower() switch
        {
            "paralyzed" or "paralysis" => true,
            "stunned" or "stun" => true,
            "frozen" => true,
            "held" => true,
            "immobilized" => true,
            _ => false
        };
    }

    public override string? GetSuffix() => "of free action";
    public override Color? GetColorOverride() => Palette.Lightning;
}
