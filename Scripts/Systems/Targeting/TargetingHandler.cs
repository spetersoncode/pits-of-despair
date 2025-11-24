using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Base class for targeting handlers.
/// Determines valid targets based on targeting definition.
/// </summary>
public abstract class TargetingHandler
{
    /// <summary>
    /// The targeting type this handler supports.
    /// </summary>
    public abstract TargetingType TargetType { get; }

    /// <summary>
    /// Gets all valid target positions for the given targeting definition.
    /// </summary>
    /// <param name="caster">The entity using the targeting</param>
    /// <param name="definition">The targeting definition</param>
    /// <param name="context">The action context</param>
    /// <returns>List of valid target positions</returns>
    public abstract List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        TargetingDefinition definition,
        ActionContext context);

    /// <summary>
    /// Checks if a specific position is a valid target.
    /// </summary>
    /// <param name="caster">The entity using the targeting</param>
    /// <param name="targetPosition">The position to check</param>
    /// <param name="definition">The targeting definition</param>
    /// <param name="context">The action context</param>
    /// <returns>True if the position is valid</returns>
    public abstract bool IsValidTarget(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context);

    /// <summary>
    /// Gets entities at the target position that would be affected.
    /// </summary>
    /// <param name="caster">The entity using the targeting</param>
    /// <param name="targetPosition">The selected target position</param>
    /// <param name="definition">The targeting definition</param>
    /// <param name="context">The action context</param>
    /// <returns>List of entities that would be affected</returns>
    public virtual List<BaseEntity> GetAffectedEntities(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
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
    /// Gets all positions that would be affected by targeting this position.
    /// Used for area preview. Default returns just the target position.
    /// </summary>
    public virtual List<GridPosition> GetAffectedPositions(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context)
    {
        return new List<GridPosition> { targetPosition };
    }

    /// <summary>
    /// Whether this targeting type requires player selection.
    /// </summary>
    public virtual bool RequiresSelection => true;

    /// <summary>
    /// Creates a targeting handler for the given targeting type.
    /// </summary>
    public static TargetingHandler CreateForType(TargetingType type)
    {
        return type switch
        {
            TargetingType.Self => new SelfTargetingHandler(),
            TargetingType.Adjacent => new AdjacentTargetingHandler(),
            TargetingType.Tile => new TileTargetingHandler(),
            TargetingType.Enemy => new EnemyTargetingHandler(),
            TargetingType.Ally => new AllyTargetingHandler(),
            TargetingType.Creature => new CreatureTargetingHandler(),
            TargetingType.Area => new AreaTargetingHandler(),
            TargetingType.Ranged => new RangedTargetingHandler(),
            TargetingType.Reach => new ReachTargetingHandler(),
            TargetingType.Line => new LineTargetingHandler(),
            TargetingType.Cone => new TileTargetingHandler(), // Fallback for now
            _ => new SelfTargetingHandler()
        };
    }

    /// <summary>
    /// Creates a targeting handler appropriate for the given definition.
    /// </summary>
    public static TargetingHandler CreateForDefinition(TargetingDefinition definition)
    {
        return CreateForType(definition.Type);
    }
}
