namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that extends the range of staff effects.
/// </summary>
public class RangeProperty : ItemProperty
{
    private readonly int _bonusRange;

    public override string Name => "far-reaching";
    public override string TypeId => "range";
    public override ItemType ValidItemTypes => ItemType.Staff;

    public RangeProperty(int bonusRange = 2)
    {
        _bonusRange = bonusRange > 0 ? bonusRange : 2;
    }

    /// <summary>
    /// Gets the bonus range to add to staff effects.
    /// </summary>
    public int BonusRange => _bonusRange;

    public override string? GetPrefix() => "far-reaching";
}
