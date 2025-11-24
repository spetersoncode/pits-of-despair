using System.Collections.Generic;
using System.Text;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Targeting;

namespace PitsOfDespair.Effects;

/// <summary>
/// Area-of-effect fire damage effect.
/// Deals fire damage to all entities within the area radius (Euclidean distance).
/// Caster can be damaged by their own fireball (friendly fire enabled).
/// </summary>
public class FireballEffect : Effect
{
    public override string Type => "fireball";
    public override string Name => "Fireball";

    /// <summary>
    /// Flat damage amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable damage (e.g., "3d6").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Area radius for the explosion (Euclidean distance).
    /// </summary>
    public int Radius { get; set; } = 2;

    public FireballEffect() { }

    /// <summary>
    /// Creates a fireball effect from a unified effect definition.
    /// </summary>
    public FireballEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        Radius = definition.Radius > 0 ? definition.Radius : 2;
    }

    public override EffectResult Apply(EffectContext context)
    {
        // FireballEffect is AOE - it should be applied via ApplyToArea instead
        // If called with single target, just damage that target
        var target = context.Target;
        var targetName = target.DisplayName;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} is unaffected by the flames.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate damage with dice
        int damage = CalculateScaledAmount(Amount, Dice, null, 0f, context);

        // Ensure minimum of 1 damage
        if (damage < 1 && (Amount > 0 || !string.IsNullOrEmpty(Dice)))
        {
            damage = 1;
        }

        if (damage <= 0)
        {
            return EffectResult.CreateFailure(
                $"The flames have no effect on {targetName}.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply fire damage
        int actualDamage = healthComponent.TakeDamage(damage, DamageType.Fire, context.Caster);

        var result = EffectResult.CreateSuccess(
            $"{targetName} is engulfed in flames for {actualDamage} damage!",
            Palette.ToHex(Palette.Fire),
            target
        );
        result.DamageDealt = actualDamage;
        return result;
    }

    /// <summary>
    /// Applies the fireball effect to all entities within the area.
    /// This is the primary method for AOE application.
    /// </summary>
    /// <param name="caster">The entity casting the fireball.</param>
    /// <param name="centerPosition">The center of the explosion.</param>
    /// <param name="context">The action context.</param>
    /// <returns>Combined result of all damage applications.</returns>
    public List<EffectResult> ApplyToArea(BaseEntity caster, GridPosition centerPosition, ActionContext context)
    {
        var results = new List<EffectResult>();
        var affectedEntities = GetEntitiesInArea(centerPosition, context);

        foreach (var entity in affectedEntities)
        {
            var effectContext = EffectContext.ForItem(entity, caster, context);
            var result = Apply(effectContext);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Gets all entities within the fireball's area of effect.
    /// Uses Euclidean distance for circular blast radius.
    /// </summary>
    private List<BaseEntity> GetEntitiesInArea(GridPosition center, ActionContext context)
    {
        var entities = new List<BaseEntity>();

        for (int dx = -Radius; dx <= Radius; dx++)
        {
            for (int dy = -Radius; dy <= Radius; dy++)
            {
                var checkPos = new GridPosition(center.X + dx, center.Y + dy);

                // Use Euclidean distance for circular area
                if (!DistanceHelper.IsInEuclideanRange(center, checkPos, Radius))
                    continue;

                var entity = context.EntityManager.GetEntityAtPosition(checkPos);
                if (entity != null && !entities.Contains(entity))
                {
                    // Only affect entities with health
                    if (entity.GetNodeOrNull<HealthComponent>("HealthComponent") != null)
                    {
                        entities.Add(entity);
                    }
                }
            }
        }

        return entities;
    }
}
