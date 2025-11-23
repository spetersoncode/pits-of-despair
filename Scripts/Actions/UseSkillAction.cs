using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Skills;
using PitsOfDespair.Targeting;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for using an active skill.
/// Handles self-targeting skills directly; targeted skills should use UseTargetedSkillAction.
/// </summary>
public class UseSkillAction : Action
{
    private readonly SkillDefinition _skill;
    private readonly List<BaseEntity> _targets;

    public override string Name => "UseSkill";

    /// <summary>
    /// Creates a skill action for a self-targeting skill.
    /// </summary>
    /// <param name="skill">The skill definition</param>
    public UseSkillAction(SkillDefinition skill)
    {
        _skill = skill;
        _targets = new List<BaseEntity>();
    }

    /// <summary>
    /// Creates a skill action with specific targets.
    /// </summary>
    /// <param name="skill">The skill definition</param>
    /// <param name="targets">The target entities</param>
    public UseSkillAction(SkillDefinition skill, List<BaseEntity> targets)
    {
        _skill = skill;
        _targets = targets ?? new List<BaseEntity>();
    }

    /// <summary>
    /// Creates a skill action with a single target.
    /// </summary>
    /// <param name="skill">The skill definition</param>
    /// <param name="target">The target entity</param>
    public UseSkillAction(SkillDefinition skill, BaseEntity target)
    {
        _skill = skill;
        _targets = new List<BaseEntity> { target };
    }

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || context == null || _skill == null)
        {
            return false;
        }

        // Use SkillExecutor validation
        return SkillExecutor.CanExecuteSkill(actor, _skill, out _);
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (_skill == null)
        {
            return ActionResult.CreateFailure("No skill specified.");
        }

        // Determine targets based on skill targeting type
        var targets = ResolveTargets(actor, context);

        // Execute the skill
        var result = SkillExecutor.ExecuteSkill(actor, _skill, targets, context);

        if (result.Success)
        {
            // Emit player-specific feedback for UI logging
            if (actor is Player player)
            {
                player.EmitSkillUsed(_skill.Name, true, result.GetCombinedMessage());
            }

            return ActionResult.CreateSuccess(result.GetCombinedMessage());
        }
        else
        {
            return ActionResult.CreateFailure(result.GetCombinedMessage());
        }
    }

    /// <summary>
    /// Resolves the targets for this skill action.
    /// </summary>
    private List<BaseEntity> ResolveTargets(BaseEntity actor, ActionContext context)
    {
        // If targets were explicitly provided, use them
        if (_targets.Count > 0)
        {
            return _targets;
        }

        // For self-targeting skills, target the caster
        var targetingType = _skill.GetTargetingType();
        if (targetingType == TargetingType.Self)
        {
            return new List<BaseEntity> { actor };
        }

        // For other targeting types without explicit targets, return empty
        // The caller should have provided targets via constructor
        return new List<BaseEntity>();
    }
}
