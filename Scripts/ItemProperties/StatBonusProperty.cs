using PitsOfDespair.Core;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that provides a stat bonus to the wearer.
/// Supports armor, evasion, regen, and max health bonuses.
/// Enhancement bonus (armor/evasion) shows as prefix: "+1 Studded Leather"
/// Other bonuses show as suffix: "Studded Leather of Health"
/// </summary>
public class StatBonusProperty : ItemProperty, IStatBonusProperty
{
    private readonly StatBonusType _bonusType;
    private readonly int _bonus;

    public override string Name => $"+{_bonus} {_bonusType}";
    public override string TypeId => $"stat_{_bonusType}";

    public StatBonusType BonusType => _bonusType;

    /// <summary>
    /// Creates a new stat bonus property.
    /// </summary>
    /// <param name="bonusType">The type of stat to modify.</param>
    /// <param name="bonus">The amount of bonus to provide.</param>
    public StatBonusProperty(StatBonusType bonusType, int bonus = 1)
    {
        _bonusType = bonusType;
        _bonus = bonus;
    }

    public int GetBonus()
    {
        return _bonus;
    }

    public override string? GetPrefix()
    {
        // Armor and Evasion bonuses show as enhancement prefix: "+1 Studded Leather"
        return _bonusType switch
        {
            StatBonusType.Armor => $"+{_bonus}",
            StatBonusType.Evasion => $"+{_bonus}",
            _ => null
        };
    }

    public override string? GetSuffix()
    {
        // Regen and MaxHealth show as suffix: "Studded Leather of Vitality"
        return _bonusType switch
        {
            StatBonusType.Regen => "of Regeneration",
            StatBonusType.MaxHealth => "of Vitality",
            _ => null
        };
    }
}
