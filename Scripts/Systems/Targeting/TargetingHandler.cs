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
    public abstract List<GridPosition> GetValidTargetPositions(
        BaseEntity caster,
        TargetingDefinition definition,
        ActionContext context);

    /// <summary>
    /// Checks if a specific position is a valid target.
    /// </summary>
    public abstract bool IsValidTarget(
        BaseEntity caster,
        GridPosition targetPosition,
        TargetingDefinition definition,
        ActionContext context);

    /// <summary>
    /// Gets entities at the target position that would be affected.
    /// </summary>
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
    /// Creates a targeting handler for the given targeting type.
    /// </summary>
    public static TargetingHandler CreateForType(TargetingType type)
    {
        return type switch
        {
            TargetingType.Creature => new CreatureTargetingHandler(),
            TargetingType.Tile => new TileTargetingHandler(),
            TargetingType.Area => new AreaTargetingHandler(),
            TargetingType.Line => new LineTargetingHandler(),
            TargetingType.Cone => new ConeTargetingHandler(),
            TargetingType.Cleave => new CleaveTargetingHandler(),
            _ => new CreatureTargetingHandler()
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
