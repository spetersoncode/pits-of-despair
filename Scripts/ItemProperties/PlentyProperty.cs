using PitsOfDespair.Data;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Spawn-time property for wands that multiplies charge rolls.
/// A "Plentiful wand of Fireball" rolls charges twice and sums them.
/// </summary>
public class PlentyProperty : ItemProperty
{
    private readonly int _chargeMultiplier;

    public override string Name => "plentiful";
    public override string TypeId => "plenty";
    public override ItemType ValidItemTypes => ItemType.Wand;

    public PlentyProperty(int chargeMultiplier = 2)
    {
        _chargeMultiplier = chargeMultiplier > 1 ? chargeMultiplier : 2;
    }

    /// <summary>
    /// Gets the charge multiplier (how many times to roll charges).
    /// </summary>
    public int ChargeMultiplier => _chargeMultiplier;

    public override string? GetPrefix() => "plentiful";
}
