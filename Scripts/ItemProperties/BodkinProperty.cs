namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property for bodkin (armor-piercing) ammunition.
/// Ignores a portion of the target's armor.
/// </summary>
public class BodkinProperty : ItemProperty, IArmorPiercingProperty
{
    private readonly int _armorPiercing;

    public override string Name => "bodkin";
    public override string TypeId => "bodkin";
    public override ItemType ValidItemTypes => ItemType.Ammo;

    public BodkinProperty(int armorPiercing = 2)
    {
        _armorPiercing = armorPiercing;
    }

    public int GetArmorPiercing() => _armorPiercing;

    public override string? GetPrefix() => "bodkin";
}
