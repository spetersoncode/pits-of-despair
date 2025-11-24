using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Targeting;

namespace PitsOfDespair.Effects;

/// <summary>
/// Cone-shaped cold damage effect.
/// Deals cold damage to all entities within a cone from the caster toward the target.
/// </summary>
public class ConeOfColdEffect : Effect
{
    public override string Type => "cone_of_cold";
    public override string Name => "Cone of Cold";

    /// <summary>
    /// Flat damage amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable damage (e.g., "2d8").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Length of the cone (how far it extends).
    /// </summary>
    public int Range { get; set; } = 4;

    /// <summary>
    /// Spread of the cone (width at max range).
    /// </summary>
    public int Radius { get; set; } = 3;

    public ConeOfColdEffect() { }

    public ConeOfColdEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        Range = definition.Range > 0 ? definition.Range : 4;
        Radius = definition.Radius > 0 ? definition.Radius : 3;
    }

    public override EffectResult Apply(EffectContext context)
    {
        // ConeOfColdEffect is AOE - if called with single target, just damage that target
        var target = context.Target;
        var targetName = target.DisplayName;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} is unaffected by the cold.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate damage with dice
        int damage = CalculateScaledAmount(Amount, Dice, null, 0f, context);
        damage = System.Math.Max(1, damage);

        if (damage <= 0)
        {
            return EffectResult.CreateFailure(
                $"The cold has no effect on {targetName}.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply cold damage
        int actualDamage = healthComponent.TakeDamage(damage, DamageType.Cold, context.Caster);

        var result = EffectResult.CreateSuccess(
            $"{targetName} is frozen for {actualDamage} damage!",
            Palette.ToHex(Palette.Ice),
            target
        );
        result.DamageDealt = actualDamage;
        return result;
    }

    /// <summary>
    /// Applies the cone of cold effect to all entities within the cone.
    /// </summary>
    /// <param name="caster">The entity casting the effect.</param>
    /// <param name="targetPosition">The target position defining cone direction.</param>
    /// <param name="context">The action context.</param>
    /// <returns>Combined result of all damage applications.</returns>
    public List<EffectResult> ApplyToCone(BaseEntity caster, GridPosition targetPosition, ActionContext context)
    {
        var results = new List<EffectResult>();
        var affectedPositions = ConeTargetingHandler.GetConePositions(
            caster.GridPosition,
            targetPosition,
            Range,
            Radius,
            context.MapSystem,
            stopAtWalls: true
        );

        foreach (var pos in affectedPositions)
        {
            var entity = context.EntityManager.GetEntityAtPosition(pos);
            if (entity != null)
            {
                var healthComponent = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
                if (healthComponent != null)
                {
                    var effectContext = EffectContext.ForItem(entity, caster, context);
                    var result = Apply(effectContext);
                    results.Add(result);
                }
            }
        }

        return results;
    }
}
