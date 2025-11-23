using System.Collections.Generic;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Skills;
using PitsOfDespair.Skills.Effects;
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
            failureReason = $"Not enough Willpower ({skillDef.WillpowerCost} WP required).";
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
    /// <returns>The result of the skill execution</returns>
    public static SkillResult ExecuteSkill(
        BaseEntity caster,
        SkillDefinition skillDef,
        List<BaseEntity> targets,
        ActionContext context)
    {
        // Validate skill can be used
        if (!CanExecuteSkill(caster, skillDef, out string failureReason))
        {
            return SkillResult.CreateFailure(failureReason);
        }

        // Validate we have targets
        if (targets.Count == 0 && skillDef.GetTargetingType() != TargetingType.Self)
        {
            return SkillResult.CreateFailure("No valid targets.");
        }

        // For self-targeting skills, add caster as target if not already included
        if (skillDef.GetTargetingType() == TargetingType.Self && targets.Count == 0)
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

        // Create effect context
        var effectContext = new SkillEffectContext(caster, context, skillDef);

        // Process effects
        if (skillDef.Effects.Count == 0)
        {
            result.AddMessage($"{caster.DisplayName} uses {skillDef.Name}!");
            return result;
        }

        bool anyEffectSucceeded = false;

        foreach (var effectDef in skillDef.Effects)
        {
            var effect = SkillEffect.CreateFromDefinition(effectDef);
            if (effect == null)
            {
                GD.PrintErr($"SkillExecutor: Unknown effect type '{effectDef.Type}' in skill '{skillDef.Id}'");
                continue;
            }

            // Apply effect to all targets
            foreach (var target in targets)
            {
                var effectResult = effect.Apply(target, effectContext);

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
        }

        // If no messages were added, add a generic one
        if (result.Messages.Count == 0)
        {
            result.AddMessage($"{caster.DisplayName} uses {skillDef.Name}!");
        }

        result.Success = anyEffectSucceeded || skillDef.Effects.Count == 0;

        return result;
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
