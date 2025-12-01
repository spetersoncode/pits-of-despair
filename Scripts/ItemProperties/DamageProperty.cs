using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// A property that provides a flat damage bonus to weapon attacks.
/// Examples: +1 Sword, +3 Axe (Keen weapons)
/// </summary>
public class DamageProperty : ItemProperty, IDamageProperty
{
    private readonly int _amount;

    public override string Name => _amount >= 0 ? "Keen" : "Blunted";
    public override string TypeId => "damage";

    public DamageProperty(int amount, string duration = "permanent", string? sourceId = null)
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

    public override PropertyMessage OnApplied(ItemInstance item)
    {
        if (_amount > 0)
        {
            return new PropertyMessage(
                $"{item.Template.Name} is now a +{_amount} weapon!",
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
                $"The enhancement on {item.Template.Name} fades.",
                Palette.ToHex(Palette.StatusNeutral)
            );
        }
        return PropertyMessage.Empty;
    }
}
