using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Targeting handler for self-targeting.
/// No selection required - always targets the caster.
/// </summary>
public class SelfTargetingHandler : TargetingHandler
{
    public override TargetingType TargetType => TargetingType.Self;

    public override bool RequiresSelection => false;

    public override List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        TargetingDefinition definition,
        ActionContext context)
    {
        return new List<GridPosition> { caster.GridPosition };
    }

    public override bool IsValidTarget(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        return targetPosition == caster.GridPosition;
    }

    public override List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        return new List<BaseEntity> { caster };
    }
}
