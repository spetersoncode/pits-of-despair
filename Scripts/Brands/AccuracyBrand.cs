using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Brands;

/// <summary>
/// A brand that provides a hit/accuracy bonus to weapon attacks.
/// Examples: Accurate Sword, Precise Dagger
/// </summary>
public class AccuracyBrand : Brand, IHitBrand
{
    private readonly int _amount;

    public override string Name => _amount >= 0 ? "Accurate" : "Unwieldy";
    public override string TypeId => "accuracy";

    public AccuracyBrand(int amount, string duration = "permanent", string? sourceId = null)
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

    public override BrandMessage OnApplied(ItemInstance item)
    {
        if (_amount > 0)
        {
            return new BrandMessage(
                $"{item.Template.Name} feels more accurate!",
                Palette.ToHex(Palette.StatusBuff)
            );
        }
        return BrandMessage.Empty;
    }

    public override BrandMessage OnRemoved(ItemInstance item)
    {
        if (IsTemporary)
        {
            return new BrandMessage(
                $"The precision enchantment on {item.Template.Name} fades.",
                Palette.ToHex(Palette.StatusNeutral)
            );
        }
        return BrandMessage.Empty;
    }
}
