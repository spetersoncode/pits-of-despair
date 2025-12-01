namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Combined enhancement property for weapons.
/// Provides both hit bonus and damage bonus as a single "+X weapon" modifier.
/// </summary>
public class WeaponEnhancementProperty : ItemProperty, IDamageProperty, IHitProperty
{
    private readonly int _bonus;

    public override string Name => $"+{_bonus} weapon";
    public override string TypeId => "weapon_enhancement";
    public override ItemType ValidItemTypes => ItemType.Weapon;

    public WeaponEnhancementProperty(int bonus = 1, string duration = "permanent", string? sourceId = null)
    {
        _bonus = bonus > 0 ? bonus : 1;
        Duration = duration;
        SourceId = sourceId;
    }

    public int GetDamageBonus() => _bonus;
    public int GetHitBonus() => _bonus;

    public override string? GetPrefix() => $"+{_bonus}";
}
