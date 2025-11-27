using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Effects;

/// <summary>
/// Line-based lightning damage effect.
/// Damages all entities along a line from caster to the end of range.
/// Used for Lightning Bolt skill.
/// </summary>
public class LightningBoltEffect : Effect
{
    public override string Type => "lightning_bolt";
    public override string Name => "Lightning Bolt";

    /// <summary>
    /// Flat damage amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable damage (e.g., "2d6").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Damage type (defaults to Lightning).
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Lightning;

    public LightningBoltEffect() { }

    public LightningBoltEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;

        // Parse damage type if specified
        if (!string.IsNullOrEmpty(definition.DamageType))
        {
            if (System.Enum.TryParse<DamageType>(definition.DamageType, ignoreCase: true, out var dt))
            {
                DamageType = dt;
            }
        }
    }

    /// <summary>
    /// Applies lightning damage to a single target.
    /// Called by SkillExecutor for each entity along the line.
    /// </summary>
    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} is unaffected by the lightning.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate damage with dice
        int damage = CalculateScaledAmount(Amount, Dice, null, 0f, context);
        damage = System.Math.Max(1, damage);

        if (damage <= 0)
        {
            return EffectResult.CreateFailure(
                $"The lightning has no effect on {targetName}.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply lightning damage
        int actualDamage = healthComponent.TakeDamage(damage, DamageType, context.Caster);

        var result = EffectResult.CreateSuccess(
            $"{targetName} is struck by lightning for {actualDamage} damage!",
            Palette.ToHex(Palette.Lightning),
            target
        );
        result.DamageDealt = actualDamage;
        return result;
    }
}
