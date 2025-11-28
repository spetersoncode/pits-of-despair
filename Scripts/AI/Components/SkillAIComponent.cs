using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Skills;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// AI component that enables creatures to use learned skills.
/// Uses inverted responsibility pattern: skills self-categorize via their metadata,
/// and this component offers them to the appropriate AI events.
///
/// Event mapping based on skill metadata:
/// - OnGetRangedActions: targeting=enemy AND range > 1
/// - OnGetDefensiveActions: has "heal" tag OR targeting=self/ally
/// - OnGetMeleeActions: targeting=enemy AND range <= 1
/// </summary>
public partial class SkillAIComponent : Node, IAIEventHandler
{
    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private WillpowerComponent? _willpowerComponent;
    private DataLoader? _dataLoader;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity != null)
        {
            _skillComponent = _entity.GetNodeOrNull<SkillComponent>("SkillComponent");
            _willpowerComponent = _entity.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        }
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
    }

    /// <summary>
    /// Handle AI events - responds to multiple events based on skill metadata.
    /// Skills self-categorize: the component checks each skill's metadata to determine
    /// which event it should respond to.
    /// </summary>
    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (_entity == null || _skillComponent == null || _willpowerComponent == null || _dataLoader == null)
            return;

        if (args.Target == null)
            return;

        switch (eventName)
        {
            case AIEvents.OnGetRangedActions:
                AddMatchingSkills(args, IsRangedOffensive);
                break;
            case AIEvents.OnGetDefensiveActions:
                AddMatchingSkills(args, IsDefensive);
                break;
            case AIEvents.OnGetMeleeActions:
                AddMatchingSkills(args, IsMeleeOffensive);
                break;
        }
    }

    /// <summary>
    /// Adds skills matching the predicate to the action list.
    /// </summary>
    private void AddMatchingSkills(GetActionsEventArgs args, System.Func<SkillDefinition, bool> predicate)
    {
        foreach (var skillId in _skillComponent!.LearnedSkills)
        {
            var skill = _dataLoader!.Skills.Get(skillId);
            if (skill == null)
                continue;

            // Only active skills can be used
            if (skill.GetCategory() != SkillCategory.Active)
                continue;

            // Check if skill matches this event's category
            if (!predicate(skill))
                continue;

            // Check willpower cost
            if (_willpowerComponent!.CurrentWillpower < skill.WillpowerCost)
                continue;

            // Validate skill can reach target
            if (!CanUseSkillOnTarget(skill, args.Target!, args.Context))
                continue;

            // Create and add the action
            var action = new UseTargetedSkillAction(skill, args.Target!.GridPosition);
            var aiAction = new AIAction(
                action: action,
                weight: 1,
                debugName: skill.Name
            );
            args.ActionList.Add(aiAction);
        }
    }

    /// <summary>
    /// Validates that a skill can reach and affect the target.
    /// </summary>
    private bool CanUseSkillOnTarget(SkillDefinition skill, BaseEntity target, AIContext context)
    {
        // Self-targeting skills don't need target validation
        if (skill.IsSelfTargeting())
            return true;

        int range = skill.Range > 0 ? skill.Range : 1;

        // Check range using Euclidean distance (matches FOV and ranged attacks)
        int distanceSquared = DistanceHelper.EuclideanDistance(_entity!.GridPosition, target.GridPosition);
        int rangeSquared = range * range;

        if (distanceSquared > rangeSquared || distanceSquared == 0)
            return false;

        // Check line of sight for ranged skills
        if (range > 1)
        {
            var visibleTiles = FOVCalculator.CalculateVisibleTiles(
                _entity.GridPosition,
                range,
                context.ActionContext.MapSystem);

            if (!visibleTiles.Contains(target.GridPosition))
                return false;
        }

        return true;
    }

    #region Skill Classification (Inverted Responsibility)

    /// <summary>
    /// Returns true if skill is a ranged offensive skill.
    /// Criteria: targets enemy AND range > 1
    /// </summary>
    private bool IsRangedOffensive(SkillDefinition skill)
    {
        var targeting = skill.Targeting?.ToLower() ?? "self";

        // Must target enemies
        if (targeting != "enemy" && targeting != "creature" && targeting != "ranged")
            return false;

        // Must have range > 1 (ranged, not melee)
        return skill.Range > 1;
    }

    /// <summary>
    /// Returns true if skill is defensive (healing, buffs, self-targeting).
    /// Criteria: has "heal" tag OR targeting=self OR targeting=ally
    /// </summary>
    private bool IsDefensive(SkillDefinition skill)
    {
        var targeting = skill.Targeting?.ToLower() ?? "self";

        // Healing skills
        if (skill.Tags.Contains("heal"))
            return true;

        // Self-targeting skills (buffs, etc.)
        if (targeting == "self")
            return true;

        // Ally-targeting skills
        if (targeting == "ally")
            return true;

        return false;
    }

    /// <summary>
    /// Returns true if skill is a melee offensive skill.
    /// Criteria: targets enemy AND range <= 1
    /// </summary>
    private bool IsMeleeOffensive(SkillDefinition skill)
    {
        var targeting = skill.Targeting?.ToLower() ?? "self";

        // Must target enemies
        if (targeting != "enemy" && targeting != "creature" && targeting != "melee" && targeting != "adjacent")
            return false;

        // Must be melee range (1 or less)
        return skill.Range <= 1;
    }

    #endregion
}
