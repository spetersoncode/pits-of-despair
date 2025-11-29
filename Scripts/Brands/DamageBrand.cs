using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Brands;

/// <summary>
/// A brand that provides a flat damage bonus to weapon attacks.
/// Examples: +1 Sword, +3 Axe (Keen weapons)
/// </summary>
public class DamageBrand : Brand, IDamageBrand
{
    private readonly int _amount;

    public override string Name => _amount >= 0 ? "Keen" : "Blunted";
    public override string TypeId => "damage";

    public DamageBrand(int amount, string duration = "permanent", string? sourceId = null)
    {
        _amount = amount;
        Duration = duration;
        SourceId = sourceId;
    }

    public int GetDamageBonus() => _amount;

    public override string? GetPrefix()
    {
        // Show as "+N" or "-N" for numeric bonuses
        return _amount >= 0 ? $"+{_amount}" : _amount.ToString();
    }

    public override BrandMessage OnApplied(ItemInstance item)
    {
        if (_amount > 0)
        {
            return new BrandMessage(
                $"{item.Template.Name} is now a +{_amount} weapon!",
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
                $"The enhancement on {item.Template.Name} fades.",
                Palette.ToHex(Palette.StatusNeutral)
            );
        }
        return BrandMessage.Empty;
    }
}
