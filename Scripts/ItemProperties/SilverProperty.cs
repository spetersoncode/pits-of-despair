using PitsOfDespair.Data;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property for silver-tipped ammunition or weapons.
/// Bypasses physical damage resistances (slashing, piercing, bludgeoning).
/// Effective against creatures that resist normal weapons.
/// </summary>
public class SilverProperty : ItemProperty, IBypassResistanceProperty
{
    public override string Name => "silver";
    public override string TypeId => "silver";
    public override ItemType ValidItemTypes => ItemType.Weapon | ItemType.Ammo;

    public bool BypassesResistance(DamageType damageType)
    {
        // Silver bypasses physical damage resistances
        return damageType == DamageType.Slashing ||
               damageType == DamageType.Piercing ||
               damageType == DamageType.Bludgeoning;
    }

    public override string? GetPrefix() => "silver-tipped";
}
