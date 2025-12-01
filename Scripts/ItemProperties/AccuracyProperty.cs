using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// A property that provides a hit/accuracy bonus to weapon attacks.
/// Examples: Accurate Sword, Precise Dagger
/// </summary>
public class AccuracyProperty : ItemProperty, IHitProperty
{
    private readonly int _amount;

    public override string Name => _amount >= 0 ? "Accurate" : "Unwieldy";
    public override string TypeId => "accuracy";

    public AccuracyProperty(int amount, string duration = "permanent", string? sourceId = null)
    {
        _amount = amount;
        Duration = duration;
        SourceId = sourceId;
    }

    public int GetHitBonus() => _amount;

    public override string? GetPrefix()
    {
        // Only show prefix for positive bonuses
        if (_amount > 0)
            return "Accurate";
        if (_amount < 0)
            return "Unwieldy";
        return null;
    }

    public override PropertyMessage OnApplied(ItemInstance item)
    {
        if (_amount > 0)
        {
            return new PropertyMessage(
                $"{item.Template.Name} feels more accurate!",
                Palette.ToHex(Palette.StatusBuff)
            );
        }
        return PropertyMessage.Empty;
    }

    public override PropertyMessage OnRemoved(ItemInstance item)
    {
        if (IsTemporary)
        {
            return new PropertyMessage(
                $"The precision enchantment on {item.Template.Name} fades.",
                Palette.ToHex(Palette.StatusNeutral)
            );
        }
        return PropertyMessage.Empty;
    }
}
