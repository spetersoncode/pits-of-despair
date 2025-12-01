using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that provides a stat bonus to the wearer.
/// Supports armor, evasion, regen, and max health bonuses.
/// Enhancement bonus (armor/evasion) shows as prefix: "+1 studded leather"
/// Other bonuses show as suffix: "studded leather of vitality"
/// </summary>
public class StatBonusProperty : ItemProperty, IStatBonusProperty
{
    private readonly StatBonusType _bonusType;
    private readonly int _bonus;

    public override string Name => $"+{_bonus} {_bonusType.ToString().ToLower()}";
    public override string TypeId => $"stat_{_bonusType}";
    public override ItemType ValidItemTypes => ItemType.Ring;

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
        // Armor and Evasion bonuses show as enhancement prefix: "+1 studded leather"
        return _bonusType switch
        {
            StatBonusType.Armor => $"+{_bonus}",
            StatBonusType.Evasion => $"+{_bonus}",
            _ => null
        };
    }

    public override string? GetSuffix()
    {
        // All stat types have suffixes for items that use suffix naming (rings)
        // Armor/Evasion also have prefixes for enhancement naming on armor
        return _bonusType switch
        {
            StatBonusType.Armor => "of protection",
            StatBonusType.Evasion => "of evasion",
            StatBonusType.Regen => "of regeneration",
            StatBonusType.MaxHealth => "of vitality",
            _ => null
        };
    }

    public override Color? GetColorOverride()
    {
        // Colors for ring variants
        return _bonusType switch
        {
            StatBonusType.Evasion => Palette.Jade,
            StatBonusType.Regen => Palette.Crimson,
            StatBonusType.MaxHealth => Palette.Ruby,
            StatBonusType.Armor => Palette.Steel,
            _ => null
        };
    }
}
