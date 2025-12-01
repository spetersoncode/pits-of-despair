namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Spawn-time property for staves that increases maximum charges.
/// </summary>
public class CapacityProperty : ItemProperty
{
    private readonly int _bonusCharges;

    public override string Name => "capacious";
    public override string TypeId => "capacity";
    public override ItemType ValidItemTypes => ItemType.Staff;

    public CapacityProperty(int bonusCharges = 2)
    {
        _bonusCharges = bonusCharges > 0 ? bonusCharges : 2;
    }

    /// <summary>
    /// Gets the number of bonus charges to add.
    /// </summary>
    public int BonusCharges => _bonusCharges;

    public override string? GetPrefix() => "capacious";
}
