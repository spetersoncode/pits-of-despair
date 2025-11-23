using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills.Targeting;

/// <summary>
/// Targeting handler for self-targeting skills.
/// No selection required - always targets the caster.
/// </summary>
public class SelfTargeting : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Self;

    public override bool RequiresSelection => false;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        SkillDefinition skill,
        ActionContext context)
    {
        // Only valid position is the caster's position
        return new List<GridPosition> { caster.GridPosition };
    }

    public override bool IsValidTarget(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skill,
        ActionContext context)
    {
        // Only the caster's position is valid
        return targetPosition == caster.GridPosition;
    }

    public override List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skill,
        ActionContext context)
    {
        // Self-targeting always affects the caster
        return new List<BaseEntity> { caster };
    }
}
