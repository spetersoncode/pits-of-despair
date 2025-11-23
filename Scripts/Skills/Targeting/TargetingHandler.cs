using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills.Targeting;

/// <summary>
/// Base class for skill targeting handlers.
/// Determines valid targets based on skill targeting type.
/// </summary>
public abstract class TargetingHandler
{
    /// <summary>
    /// The targeting type this handler supports.
    /// </summary>
    public abstract TargetingType TargetType { get; }

    /// <summary>
    /// Gets all valid target positions for a skill.
    /// </summary>
    /// <param name="caster">The entity casting the skill</param>
    /// <param name="skill">The skill definition</param>
    /// <param name="context">The action context</param>
    /// <returns>List of valid target positions</returns>
    public abstract List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        SkillDefinition skill,
        ActionContext context);

    /// <summary>
    /// Checks if a specific position is a valid target.
    /// </summary>
    /// <param name="caster">The entity casting the skill</param>
    /// <param name="targetPosition">The position to check</param>
    /// <param name="skill">The skill definition</param>
    /// <param name="context">The action context</param>
    /// <returns>True if the position is valid</returns>
    public abstract bool IsValidTarget(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skill,
        ActionContext context);

    /// <summary>
    /// Gets entities at the target position that would be affected.
    /// </summary>
    /// <param name="caster">The entity casting the skill</param>
    /// <param name="targetPosition">The selected target position</param>
    /// <param name="skill">The skill definition</param>
    /// <param name="context">The action context</param>
    /// <returns>List of entities that would be affected</returns>
    public virtual List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        SkillDefinition skill,
        ActionContext context)
    {
        var entities = new List<BaseEntity>();
        var entity = context.EntityManager.GetEntityAtPosition(targetPosition);
        if (entity != null)
        {
            entities.Add(entity);
        }
        return entities;
    }

    /// <summary>
    /// Creates a targeting handler for the given targeting type.
    /// </summary>
    public static TargetingHandler CreateForType(TargetingType type)
    {
        return type switch
        {
            TargetingType.Self => new SelfTargeting(),
            TargetingType.Adjacent => new AdjacentTargeting(),
            TargetingType.Tile => new TileTargeting(),
            TargetingType.Enemy => new EnemyTargeting(),
            TargetingType.Ally => new AllyTargeting(),
            TargetingType.Area => new AreaTargeting(),
            TargetingType.Line => new TileTargeting(), // Fallback to tile for now
            TargetingType.Cone => new TileTargeting(), // Fallback to tile for now
            _ => new SelfTargeting()
        };
    }

    /// <summary>
    /// Whether this targeting type requires player selection.
    /// </summary>
    public virtual bool RequiresSelection => true;
}
