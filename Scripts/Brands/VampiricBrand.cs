using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Brands;

/// <summary>
/// A brand that heals the attacker for a percentage of damage dealt.
/// Example: Vampiric Dagger - heal 25% of damage dealt on hit
/// </summary>
public class VampiricBrand : Brand, IOnHitBrand
{
    private readonly int _healPercent;

    public override string Name => "Vampiric";
    public override string TypeId => "vampiric";

    /// <param name="healPercent">Percentage of damage to heal (default 25%).</param>
    public VampiricBrand(int healPercent = 25, string duration = "permanent", string? sourceId = null)
    {
        _healPercent = healPercent > 0 ? healPercent : 25;
        Duration = duration;
        SourceId = sourceId;
    }

    public override string? GetPrefix() => "Vampiric";

    public OnHitResult OnHit(BaseEntity attacker, BaseEntity target, int damage)
    {
        if (damage <= 0) return OnHitResult.None;

        int healAmount = (damage * _healPercent) / 100;
        if (healAmount <= 0) return OnHitResult.None;

        var attackerHealth = attacker.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (attackerHealth != null && attackerHealth.IsAlive())
        {
            attackerHealth.Heal(healAmount);
            return new OnHitResult { HealingDone = healAmount };
        }
        return OnHitResult.None;
    }

    public override BrandMessage OnApplied(ItemInstance item)
    {
        return new BrandMessage(
            $"{item.Template.Name} thirsts for blood!",
            Palette.ToHex(Palette.Blood)
        );
    }

    public override BrandMessage OnRemoved(ItemInstance item)
    {
        if (IsTemporary)
        {
            return new BrandMessage(
                $"The hunger in {item.Template.Name} fades.",
                Palette.ToHex(Palette.StatusNeutral)
            );
        }
        return BrandMessage.Empty;
    }
}
