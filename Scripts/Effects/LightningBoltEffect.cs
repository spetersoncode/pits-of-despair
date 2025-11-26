using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Targeting;

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

    /// <summary>
    /// Applies lightning bolt to all entities along a line from caster to target direction.
    /// Also spawns the lightning beam visual effect.
    /// </summary>
    /// <param name="caster">The entity casting the effect.</param>
    /// <param name="targetPosition">The target position defining line direction.</param>
    /// <param name="range">Maximum range from skill definition.</param>
    /// <param name="context">The action context.</param>
    /// <returns>List of effect results for each entity hit.</returns>
    public List<EffectResult> ApplyToLine(BaseEntity caster, GridPosition targetPosition, int range, ActionContext context)
    {
        var results = new List<EffectResult>();

        // Get all positions along the line (full range, stops at walls)
        var linePositions = LineTargetingHandler.GetLinePositions(
            caster.GridPosition,
            targetPosition,
            range,
            context.MapSystem,
            stopAtWalls: true
        );

        // Find the end position for the visual
        GridPosition endPos = linePositions.Count > 0 ? linePositions[^1] : caster.GridPosition;

        // Spawn lightning beam visual
        context.VisualEffectSystem?.SpawnLightningBeam(caster.GridPosition, endPos);

        // Apply damage to each entity along the line
        foreach (var pos in linePositions)
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

        // If no targets were hit, add a message
        if (results.Count == 0)
        {
            results.Add(EffectResult.CreateSuccess(
                "The lightning bolt crackles through the air harmlessly.",
                Palette.ToHex(Palette.Lightning)
            ));
        }

        return results;
    }

    /// <summary>
    /// Gets the end position of the lightning bolt line (for visual effect targeting).
    /// </summary>
    public GridPosition GetBeamEndPosition(BaseEntity caster, GridPosition targetPosition, int range, ActionContext context)
    {
        var linePositions = LineTargetingHandler.GetLinePositions(
            caster.GridPosition,
            targetPosition,
            range,
            context.MapSystem,
            stopAtWalls: true
        );

        return linePositions.Count > 0 ? linePositions[^1] : caster.GridPosition;
    }
}
