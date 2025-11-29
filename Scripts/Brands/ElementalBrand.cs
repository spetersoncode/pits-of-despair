using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Brands;

/// <summary>
/// A brand that deals bonus elemental damage on hit.
/// Supports Fire, Cold, Lightning, and Poison damage types.
/// Example: Flaming Sword - deals +Nd6 fire damage on hit
/// </summary>
public class ElementalBrand : Brand, IOnHitBrand
{
    private readonly int _diceCount;
    private readonly DamageType _damageType;

    public override string Name => GetNameForType(_damageType);
    public override string TypeId => GetTypeIdForType(_damageType);

    public ElementalBrand(DamageType damageType, int diceCount = 1, string duration = "permanent", string? sourceId = null)
    {
        _damageType = damageType;
        _diceCount = diceCount > 0 ? diceCount : 1;
        Duration = duration;
        SourceId = sourceId;
    }

    public override string? GetPrefix() => Name;

    public OnHitResult OnHit(BaseEntity attacker, BaseEntity target, int damage)
    {
        int elementalDamage = DiceRoller.Roll(_diceCount, 6);
        if (elementalDamage <= 0) return OnHitResult.None;

        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth != null && targetHealth.IsAlive())
        {
            int actualDamage = targetHealth.CalculateDamage(elementalDamage, _damageType);
            targetHealth.TakeDamage(elementalDamage, _damageType, attacker);

            return new OnHitResult
            {
                DamageDealt = actualDamage,
                DamageType = Name.ToLower(),
                Verb = GetVerbForType(_damageType),
                MessageColor = Palette.ToHex(GetColorForType(_damageType))
            };
        }
        return OnHitResult.None;
    }

    private static string GetVerbForType(DamageType type) => type switch
    {
        DamageType.Fire => "scorched",
        DamageType.Cold => "frozen",
        DamageType.Lightning => "shocked",
        DamageType.Poison => "poisoned",
        _ => "blasted"
    };

    public override BrandMessage OnApplied(ItemInstance item)
    {
        string message = _damageType switch
        {
            DamageType.Fire => $"{item.Template.Name} bursts into flames!",
            DamageType.Cold => $"{item.Template.Name} radiates an icy chill!",
            DamageType.Lightning => $"{item.Template.Name} crackles with electricity!",
            DamageType.Poison => $"{item.Template.Name} drips with venom!",
            _ => $"{item.Template.Name} is imbued with elemental power!"
        };

        return new BrandMessage(message, Palette.ToHex(GetColorForType(_damageType)));
    }

    public override BrandMessage OnRemoved(ItemInstance item)
    {
        if (!IsTemporary) return BrandMessage.Empty;

        string message = _damageType switch
        {
            DamageType.Fire => $"The flames on {item.Template.Name} die out.",
            DamageType.Cold => $"The frost on {item.Template.Name} melts away.",
            DamageType.Lightning => $"The sparks on {item.Template.Name} fade.",
            DamageType.Poison => $"The venom on {item.Template.Name} dries up.",
            _ => $"The enchantment on {item.Template.Name} fades."
        };

        return new BrandMessage(message, Palette.ToHex(Palette.StatusNeutral));
    }

    private static string GetNameForType(DamageType type) => type switch
    {
        DamageType.Fire => "Flaming",
        DamageType.Cold => "Freezing",
        DamageType.Lightning => "Electrified",
        DamageType.Poison => "Venomous",
        _ => "Elemental"
    };

    private static string GetTypeIdForType(DamageType type) => type switch
    {
        DamageType.Fire => "flaming",
        DamageType.Cold => "freezing",
        DamageType.Lightning => "electrified",
        DamageType.Poison => "venomous",
        _ => "elemental"
    };

    private static Godot.Color GetColorForType(DamageType type) => type switch
    {
        DamageType.Fire => Palette.Fire,
        DamageType.Cold => Palette.Ice,
        DamageType.Lightning => Palette.Lightning,
        DamageType.Poison => Palette.Poison,
        _ => Palette.Default
    };
}
