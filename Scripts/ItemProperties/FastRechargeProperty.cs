namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that makes staves recharge faster.
/// The recharge counter increments by the multiplier each turn.
/// </summary>
public class FastRechargeProperty : ItemProperty, IRechargeModifierProperty
{
    private readonly float _rechargeMultiplier;

    public override string Name => "swift";
    public override string TypeId => "fast_recharge";
    public override ItemType ValidItemTypes => ItemType.Staff;

    /// <param name="percentFaster">Percentage faster (e.g., 25 = 25% faster recharge).</param>
    public FastRechargeProperty(int percentFaster = 25)
    {
        // 25% faster = 1.25x recharge rate
        _rechargeMultiplier = 1.0f + (percentFaster / 100f);
    }

    public float GetRechargeMultiplier() => _rechargeMultiplier;

    public override string? GetPrefix() => "swift";
}
