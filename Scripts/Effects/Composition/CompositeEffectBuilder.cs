using System.Collections.Generic;
using Godot;
using PitsOfDespair.Effects.Steps;

namespace PitsOfDespair.Effects.Composition;

/// <summary>
/// Factory for building CompositeEffect instances from effect definitions.
/// Maps step type strings to concrete IEffectStep implementations.
/// </summary>
public static class CompositeEffectBuilder
{
    /// <summary>
    /// Builds a CompositeEffect from an EffectDefinition containing steps.
    /// </summary>
    /// <param name="definition">The effect definition with Steps, Name, and Sound.</param>
    /// <returns>A CompositeEffect ready to execute, or null if building fails.</returns>
    public static CompositeEffect? Build(EffectDefinition definition)
    {
        if (definition.Steps == null || definition.Steps.Count == 0)
        {
            GD.PrintErr($"CompositeEffectBuilder: No steps defined for effect '{definition.Name}'");
            return null;
        }

        var steps = new List<IEffectStep>();

        foreach (var stepDef in definition.Steps)
        {
            var step = CreateStep(stepDef);
            if (step == null)
            {
                GD.PrintErr($"CompositeEffectBuilder: Unknown step type '{stepDef.Type}' in effect '{definition.Name}'");
                continue;
            }
            steps.Add(step);
        }

        if (steps.Count == 0)
        {
            GD.PrintErr($"CompositeEffectBuilder: No valid steps created for effect '{definition.Name}'");
            return null;
        }

        return new CompositeEffect(
            definition.Type ?? "composite",
            definition.Name ?? "Effect",
            definition.Sound,
            steps,
            definition.Visual,
            definition.Range,
            definition.Radius
        );
    }

    /// <summary>
    /// Creates a step instance from a step definition.
    /// </summary>
    private static IEffectStep? CreateStep(StepDefinition stepDef)
    {
        return stepDef.Type?.ToLower() switch
        {
            "save_check" => new SaveCheckStep(stepDef),
            "attack_roll" => new AttackRollStep(stepDef),
            "damage" => new DamageStep(stepDef),
            "heal" => new HealStep(stepDef),
            "heal_caster" => new HealCasterStep(stepDef),
            "apply_condition" => new ApplyConditionStep(stepDef),
            "knockback" => new KnockbackStep(stepDef),
            "modify_willpower" => new ModifyWillpowerStep(stepDef),
            "teleport" => new TeleportStep(stepDef),
            "blink" => new BlinkStep(stepDef),
            "magic_mapping" => new MagicMappingStep(stepDef),
            "move_tiles" => new MoveTilesStep(stepDef),
            "charm" => new CharmStep(stepDef),
            "spawn_hazard" => new SpawnHazardStep(stepDef),
            "chain_damage" => new ChainDamageStep(stepDef),
            "clone" => new CloneStep(stepDef),
            "weapon_damage" => new WeaponDamageStep(stepDef),
            "apply_prime" => new ApplyPrimeStep(stepDef),
            _ => null
        };
    }
}
