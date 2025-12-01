using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that allows the wearer to see invisible creatures.
/// Ring-only property.
/// </summary>
public class SeeInvisibleProperty : ItemProperty, IPassiveAbilityProperty
{
    public override string Name => "true sight";
    public override string TypeId => "see_invisible";
    public override ItemType ValidItemTypes => ItemType.Ring;

    public string AbilityId => "see_invisible";

    public override string? GetSuffix() => "of true sight";
    public override Color? GetColorOverride() => Palette.Arcane;
}
