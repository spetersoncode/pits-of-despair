using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.VisualEffects;
using PitsOfDespair.Targeting;
using TargetingType = PitsOfDespair.Targeting.TargetingType;

namespace PitsOfDespair.Skills;

/// <summary>
/// Central execution logic for skill activation.
/// Handles validation, WP spending, and effect application.
/// </summary>
public static class SkillExecutor
{
    /// <summary>
    /// Checks if a skill can be executed by the caster.
    /// </summary>
    /// <param name="caster">The entity attempting to use the skill</param>
    /// <param name="skillDef">The skill definition</param>
    /// <param name="failureReason">Output: reason for failure if cannot execute</param>
    /// <returns>True if the skill can be executed</returns>
    public static bool CanExecuteSkill(BaseEntity caster, SkillDefinition skillDef, out string failureReason)
    {
        failureReason = string.Empty;

        // Check if skill is learned
        var skillComponent = caster.GetNodeOrNull<SkillComponent>("SkillComponent");
        if (skillComponent == null)
        {
            failureReason = $"{caster.DisplayName} cannot use skills.";
            return false;
        }

        if (!skillComponent.HasSkill(skillDef.Id))
        {
            failureReason = $"{caster.DisplayName} hasn't learned {skillDef.Name}.";
            return false;
        }

        // Check Willpower cost
        var willpowerComponent = caster.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        if (willpowerComponent == null)
        {
            failureReason = $"{caster.DisplayName} has no Willpower.";
            return false;
        }

        if (willpowerComponent.CurrentWillpower < skillDef.WillpowerCost)
        {
            failureReason = $"Not enough Willpower ({skillDef.WillpowerCost} Willpower required).";
            return false;
        }

        // Check if skill is active type (only active skills can be used via action)
        if (skillDef.GetCategory() != SkillCategory.Active)
        {
            failureReason = $"{skillDef.Name} cannot be activated directly.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Executes a skill on the given targets.
    /// </summary>
    /// <param name="caster">The entity using the skill</param>
    /// <param name="skillDef">The skill definition</param>
    /// <param name="targets">List of target entities</param>
    /// <param name="context">The action context</param>
    /// <param name="targetPosition">Optional target position for directional skills</param>
    /// <returns>The result of the skill execution</returns>
    public static SkillResult ExecuteSkill(
        BaseEntity caster,
        SkillDefinition skillDef,
        List<BaseEntity> targets,
        ActionContext context,
        GridPosition? targetPosition = null)
    {
        // Validate skill can be used
        if (!CanExecuteSkill(caster, skillDef, out string failureReason))
        {
            return SkillResult.CreateFailure(failureReason);
        }

        // For line/area targeting without entity targets, use caster as effect target
        // (the effect itself will use targetPosition for direction)
        var targetingType = skillDef.GetTargetingType();
        bool isPositionalTargeting = targetingType == TargetingType.Line ||
                                     targetingType == TargetingType.Tile ||
                                     targetingType == TargetingType.Area;

        // Cleave can execute with zero targets (swing at empty air)
        bool allowEmptyTargets = targetingType == TargetingType.Cleave;

        if (targets.Count == 0 && isPositionalTargeting && targetPosition != null)
        {
            // For positional skills, caster is the effect target
            targets.Add(caster);
        }
        else if (targets.Count == 0 && !skillDef.IsSelfTargeting() && !allowEmptyTargets)
        {
            return SkillResult.CreateFailure("No valid targets.");
        }

        // For self-targeting skills, add caster as target if not already included
        if (skillDef.IsSelfTargeting() && targets.Count == 0)
        {
            targets.Add(caster);
        }

        // Spend Willpower
        var willpowerComponent = caster.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        if (willpowerComponent != null && skillDef.WillpowerCost > 0)
        {
            if (!willpowerComponent.SpendWillpower(skillDef.WillpowerCost))
            {
                return SkillResult.CreateFailure($"Not enough Willpower.");
            }
        }

        var result = SkillResult.CreateSuccess();
        result.WillpowerSpent = skillDef.WillpowerCost;

        // Process effects
        if (skillDef.Effects.Count == 0)
        {
            result.AddMessage($"{caster.DisplayName} uses {skillDef.Name}!");
            return result;
        }

        // Check if skill uses a projectile
        if (skillDef.HasProjectile)
        {
            return ExecuteProjectileSkill(caster, skillDef, targets, context, result, targetPosition);
        }

        // Non-projectile skill: apply effects immediately
        return ExecuteImmediateSkill(caster, skillDef, targets, context, result, targetPosition);
    }

    /// <summary>
    /// Executes a skill that spawns projectiles with deferred effects.
    /// </summary>
    private static SkillResult ExecuteProjectileSkill(
        BaseEntity caster,
        SkillDefinition skillDef,
        List<BaseEntity> targets,
        ActionContext context,
        SkillResult result,
        GridPosition? targetPosition = null)
    {
        var projectileDef = VisualEffectDefinitions.GetById(skillDef.Projectile!);
        if (projectileDef == null || projectileDef.Type != VisualEffectType.Projectile)
        {
            GD.PrintErr($"SkillExecutor: Unknown projectile type '{skillDef.Projectile}' in skill '{skillDef.Id}'");
            return ExecuteImmediateSkill(caster, skillDef, targets, context, result, targetPosition);
        }

        // Spawn projectile for each target
        foreach (var target in targets)
        {
            // Create effect chain for this target
            foreach (var effectDef in skillDef.Effects)
            {
                var effect = Effect.CreateFromSkillDefinition(effectDef, skillDef.Name);
                if (effect == null)
                {
                    GD.PrintErr($"SkillExecutor: Unknown effect type '{effectDef.Type}' in skill '{skillDef.Id}'");
                    continue;
                }

                var effectContext = EffectContext.ForSkill(target, caster, context, skillDef, targetPosition);
                var capturedTarget = target;

                // Spawn projectile - effect will be applied on impact via callback
                // TODO: Sound is played in effect.Apply() for projectile effects, but in
                // ApplyToTargets() for immediate effects. Unify this when refactoring sound timing.
                context.VisualEffectSystem.SpawnProjectile(
                    projectileDef,
                    caster.GridPosition,
                    target.GridPosition,
                    () =>
                    {
                        var effectResult = effect.Apply(effectContext);
                        // Emit damage signal for message log if damage was dealt
                        if (effectResult.Success && effectResult.DamageDealt > 0)
                        {
                            string skillName = effectContext.Skill?.Name ?? "skill";
                            context.CombatSystem?.EmitSkillDamageDealt(
                                caster,
                                capturedTarget,
                                effectResult.DamageDealt,
                                skillName);
                        }
                    });

                result.AddAffectedEntity(target);
            }
        }

        result.AddMessage($"{caster.DisplayName} uses {skillDef.Name}!");
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Executes a skill with immediate effect application.
    /// </summary>
    private static SkillResult ExecuteImmediateSkill(
        BaseEntity caster,
        SkillDefinition skillDef,
        List<BaseEntity> targets,
        ActionContext context,
        SkillResult result,
        GridPosition? targetPosition = null)
    {
        bool anyEffectSucceeded = false;
        var targetingType = skillDef.GetTargetingType();

        // Spawn visual effects based on targeting type (before applying effects)
        if (targetPosition.HasValue)
        {
            SpawnTargetingVisual(caster, targetPosition.Value, skillDef, context);
        }

        foreach (var effectDef in skillDef.Effects)
        {
            var effect = Effect.CreateFromSkillDefinition(effectDef, skillDef.Name);
            if (effect == null)
            {
                GD.PrintErr($"SkillExecutor: Unknown effect type '{effectDef.Type}' in skill '{skillDef.Id}'");
                continue;
            }

            // Expand targets for multi-target melee effects (e.g., Cleave)
            var effectTargets = targets;
            if (effectDef.Targets > 1 && effectDef.Type == "melee_attack" && targets.Count > 0)
            {
                effectTargets = ExpandMeleeTargets(caster, targets[0], effectDef.Targets, context);
            }

            // Use unified ApplyToTargets for all effects
            var effectResults = effect.ApplyToTargets(caster, effectTargets, context);

            foreach (var effectResult in effectResults)
            {
                if (effectResult.Success)
                {
                    anyEffectSucceeded = true;
                }

                if (!string.IsNullOrEmpty(effectResult.Message))
                {
                    result.AddMessage(effectResult.Message);
                }

                if (effectResult.AffectedEntity != null)
                {
                    result.AddAffectedEntity(effectResult.AffectedEntity);
                }
            }

            // Add "miss" message for empty results on positional targeting
            if (effectResults.Count == 0 && (targetingType == TargetingType.Line || targetingType == TargetingType.Area || targetingType == TargetingType.Cone))
            {
                result.AddMessage($"The {skillDef.Name} affects nothing.");
                anyEffectSucceeded = true; // Skill was cast successfully, just no targets
            }
        }

        if (result.Messages.Count == 0)
        {
            result.AddMessage($"{caster.DisplayName} uses {skillDef.Name}!");
        }

        result.Success = anyEffectSucceeded || skillDef.Effects.Count == 0;
        return result;
    }

    /// <summary>
    /// Spawns visual effects based on skill targeting type.
    /// </summary>
    private static void SpawnTargetingVisual(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skillDef,
        ActionContext context)
    {
        if (context.VisualEffectSystem == null)
            return;

        var targetingType = skillDef.GetTargetingType();
        int range = skillDef.Range > 0 ? skillDef.Range : 8;

        switch (targetingType)
        {
            case TargetingType.Line:
                // Calculate line end position for beam visual
                var linePositions = LineTargetingHandler.GetLinePositions(
                    caster.GridPosition,
                    targetPosition,
                    range,
                    context.MapSystem,
                    stopAtWalls: true
                );
                var endPos = linePositions.Count > 0 ? linePositions[^1] : targetPosition;
                context.VisualEffectSystem.SpawnLightningBeam(caster.GridPosition, endPos);
                break;

            case TargetingType.Cone:
                int coneRadius = skillDef.Radius > 0 ? skillDef.Radius : 3;
                context.VisualEffectSystem.SpawnConeOfCold(caster.GridPosition, targetPosition, range, coneRadius);
                break;

            case TargetingType.Area:
                int areaRadius = skillDef.Radius > 0 ? skillDef.Radius : 2;
                context.VisualEffectSystem.SpawnExplosion(targetPosition, areaRadius, Palette.Fire);
                break;
        }
    }

    /// <summary>
    /// Expands a single target into multiple adjacent targets for multi-target melee effects.
    /// </summary>
    /// <param name="caster">The entity using the skill</param>
    /// <param name="primaryTarget">The initially selected target</param>
    /// <param name="maxTargets">Maximum number of targets</param>
    /// <param name="context">The action context</param>
    /// <returns>List of targets including primary and additional adjacent enemies</returns>
    private static List<BaseEntity> ExpandMeleeTargets(
        BaseEntity caster,
        BaseEntity primaryTarget,
        int maxTargets,
        ActionContext context)
    {
        var expandedTargets = new List<BaseEntity> { primaryTarget };

        if (maxTargets <= 1 || context.EntityManager == null)
        {
            return expandedTargets;
        }

        // Find additional enemies adjacent to the caster (within melee range)
        var adjacentEntities = context.EntityManager.GetEntitiesInRadius(caster.GridPosition, 1);

        // Filter to hostile entities with health that aren't already targeted
        var additionalTargets = adjacentEntities
            .Where(e => e != primaryTarget &&
                       e != caster &&
                       !e.IsDead &&
                       e.Faction != caster.Faction &&
                       e.GetNodeOrNull<HealthComponent>("HealthComponent") != null)
            .Take(maxTargets - 1)
            .ToList();

        expandedTargets.AddRange(additionalTargets);

        return expandedTargets;
    }

    /// <summary>
    /// Executes a skill targeting a single entity.
    /// </summary>
    public static SkillResult ExecuteSkill(
        BaseEntity caster,
        SkillDefinition skillDef,
        BaseEntity target,
        ActionContext context)
    {
        return ExecuteSkill(caster, skillDef, new List<BaseEntity> { target }, context);
    }

    /// <summary>
    /// Executes a self-targeting skill.
    /// </summary>
    public static SkillResult ExecuteSkill(
        BaseEntity caster,
        SkillDefinition skillDef,
        ActionContext context)
    {
        return ExecuteSkill(caster, skillDef, new List<BaseEntity> { caster }, context);
    }
}
