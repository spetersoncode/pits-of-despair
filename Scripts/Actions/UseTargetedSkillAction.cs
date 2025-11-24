using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Skills;
using PitsOfDespair.Targeting;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for using a skill that requires targeting.
/// Resolves targets at the specified position and executes the skill.
/// </summary>
public class UseTargetedSkillAction : Action
{
    private readonly SkillDefinition _skill;
    private readonly GridPosition _targetPosition;

    public override string Name => "UseTargetedSkill";

    public UseTargetedSkillAction(SkillDefinition skill, GridPosition targetPosition)
    {
        _skill = skill;
        _targetPosition = targetPosition;
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

        // Create targeting definition from skill
        var definition = TargetingDefinition.FromSkill(_skill);

        // Get the targeting handler
        var handler = TargetingHandler.CreateForDefinition(definition);

        // Validate the target position
        if (!handler.IsValidTarget(actor, _targetPosition, definition, context))
        {
            return ActionResult.CreateFailure("Invalid target.");
        }

        // Get affected entities at the target position
        var targets = handler.GetAffectedEntities(actor, _targetPosition, definition, context);

        // For skills that require a target entity, ensure we have one
        var targetingType = _skill.GetTargetingType();
        if ((targetingType == TargetingType.Enemy || targetingType == TargetingType.Ally) && targets.Count == 0)
        {
            return ActionResult.CreateFailure("No target at that location.");
        }

        // Execute the skill, passing target position for directional effects
        var result = SkillExecutor.ExecuteSkill(actor, _skill, targets, context, _targetPosition);

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
}
