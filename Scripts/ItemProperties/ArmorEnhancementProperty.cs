using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Combined enhancement property for armor.
/// Provides armor bonus AND reduces the armor's evasion penalty (capped at 0).
/// A +2 enhancement on plate with -3 evasion results in -1 evasion.
/// </summary>
public class ArmorEnhancementProperty : ItemProperty, IStatBonusProperty
{
    private readonly int _bonus;

    public override string Name => $"+{_bonus} armor";
    public override string TypeId => "armor_enhancement";
    public override ItemType ValidItemTypes => ItemType.Armor;

    public StatBonusType BonusType => StatBonusType.Armor;

    public ArmorEnhancementProperty(int bonus = 1, string duration = "permanent", string? sourceId = null)
    {
        _bonus = bonus > 0 ? bonus : 1;
        Duration = duration;
        SourceId = sourceId;
    }

    /// <summary>
    /// Gets the armor bonus provided.
    /// </summary>
    public int GetBonus() => _bonus;

    /// <summary>
    /// Gets the evasion bonus (reduces penalty, capped at 0 total).
    /// </summary>
    public int GetEvasionBonus() => _bonus;

    public override string? GetPrefix() => $"+{_bonus}";

    public override Color? GetColorOverride() => Palette.Steel;
}
