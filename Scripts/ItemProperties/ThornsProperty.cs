using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that reflects damage back to attackers when the wearer is hit.
/// </summary>
public class ThornsProperty : ItemProperty, IOnDamagedProperty
{
    private readonly int _damage;

    public override string Name => $"thorns +{_damage}";
    public override string TypeId => "thorns";
    public override ItemType ValidItemTypes => ItemType.Ring;

    /// <summary>
    /// Creates a new thorns property.
    /// </summary>
    /// <param name="damage">Flat damage to reflect back to attackers.</param>
    public ThornsProperty(int damage = 1)
    {
        _damage = damage;
    }

    public OnDamagedResult OnDamaged(BaseEntity wearer, BaseEntity attacker, int damage, DamageType damageType)
    {
        // Only reflect damage if there's an attacker and damage was dealt
        if (attacker == null || damage <= 0)
            return OnDamagedResult.None;

        return new OnDamagedResult
        {
            ReflectedDamage = _damage,
            DamageType = "physical",
            Verb = "retaliated",
            MessageColor = Palette.ToHex(Palette.Blood)
        };
    }

    public override string? GetSuffix() => "of thorns";
    public override Color? GetColorOverride() => Palette.Blood;
}
