using PitsOfDespair.Data;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Targeting;

/// <summary>
/// Unified configuration for all targeting operations.
/// Used by skills, items, and weapons to define how they select targets.
/// </summary>
public class TargetingDefinition
{
    /// <summary>
    /// The type of targeting this definition uses.
    /// </summary>
    public TargetingType Type { get; init; }

    /// <summary>
    /// Maximum range for targeting. 0 means melee/adjacent only.
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    /// For Area targeting, the radius of the effect around the target point.
    /// </summary>
    public int AreaSize { get; init; } = 0;

    /// <summary>
    /// Whether line-of-sight is required for valid targets.
    /// </summary>
    public bool RequiresLOS { get; init; } = true;

    /// <summary>
    /// The distance metric to use for range calculation.
    /// Euclidean for circular range (ranged weapons), Chebyshev for grid range (reach/melee).
    /// </summary>
    public DistanceMetric Metric { get; init; } = DistanceMetric.Chebyshev;

    /// <summary>
    /// What types of targets are valid.
    /// </summary>
    public TargetFilter Filter { get; init; } = TargetFilter.Enemy;

    /// <summary>
    /// Whether this targeting type requires player input to select a target.
    /// False for Self targeting.
    /// </summary>
    public bool RequiresSelection => Type != TargetingType.Self;

    /// <summary>
    /// Creates a targeting definition for self-targeting (no selection needed).
    /// </summary>
    public static TargetingDefinition Self() => new()
    {
        Type = TargetingType.Self,
        Range = 0,
        RequiresLOS = false,
        Filter = TargetFilter.Self
    };

    /// <summary>
    /// Creates a targeting definition for enemy targeting within range.
    /// </summary>
    public static TargetingDefinition Enemy(int range, bool requiresLOS = true) => new()
    {
        Type = TargetingType.Enemy,
        Range = range,
        RequiresLOS = requiresLOS,
        Metric = DistanceMetric.Chebyshev,
        Filter = TargetFilter.Enemy
    };

    /// <summary>
    /// Creates a targeting definition for ally targeting within range.
    /// </summary>
    public static TargetingDefinition Ally(int range, bool requiresLOS = true) => new()
    {
        Type = TargetingType.Ally,
        Range = range,
        RequiresLOS = requiresLOS,
        Metric = DistanceMetric.Chebyshev,
        Filter = TargetFilter.Ally
    };

    /// <summary>
    /// Creates a targeting definition for tile targeting within range.
    /// </summary>
    public static TargetingDefinition Tile(int range, bool requiresLOS = true) => new()
    {
        Type = TargetingType.Tile,
        Range = range,
        RequiresLOS = requiresLOS,
        Metric = DistanceMetric.Chebyshev,
        Filter = TargetFilter.Tile
    };

    /// <summary>
    /// Creates a targeting definition for area-of-effect targeting.
    /// </summary>
    public static TargetingDefinition Area(int range, int areaSize, bool requiresLOS = true) => new()
    {
        Type = TargetingType.Area,
        Range = range,
        AreaSize = areaSize,
        RequiresLOS = requiresLOS,
        Metric = DistanceMetric.Chebyshev,
        Filter = TargetFilter.Tile
    };

    /// <summary>
    /// Creates a targeting definition for ranged attacks (Euclidean distance with LOS).
    /// </summary>
    public static TargetingDefinition Ranged(int range) => new()
    {
        Type = TargetingType.Ranged,
        Range = range,
        RequiresLOS = true,
        Metric = DistanceMetric.Euclidean,
        Filter = TargetFilter.Enemy
    };

    /// <summary>
    /// Creates a targeting definition for reach attacks (Chebyshev distance with LOS).
    /// </summary>
    public static TargetingDefinition Reach(int range) => new()
    {
        Type = TargetingType.Reach,
        Range = range,
        RequiresLOS = true,
        Metric = DistanceMetric.Chebyshev,
        Filter = TargetFilter.Enemy
    };

    /// <summary>
    /// Creates a targeting definition for adjacent targeting (8 directions).
    /// </summary>
    public static TargetingDefinition Adjacent(TargetFilter filter = TargetFilter.Creature) => new()
    {
        Type = TargetingType.Adjacent,
        Range = 1,
        RequiresLOS = false,
        Metric = DistanceMetric.Chebyshev,
        Filter = filter
    };

    /// <summary>
    /// Creates a targeting definition from a skill definition.
    /// </summary>
    public static TargetingDefinition FromSkill(SkillDefinition skill)
    {
        var skillTargeting = skill.Targeting?.ToLower() ?? "self";
        int range = skill.Range > 0 ? skill.Range : 1;

        return skillTargeting switch
        {
            "self" => Self(),
            "adjacent" => Adjacent(TargetFilter.Creature),
            "tile" => Tile(range, requiresLOS: true),
            "enemy" => Enemy(range, requiresLOS: true),
            "ally" => Ally(range, requiresLOS: true),
            "area" => Area(range, skill.AreaSize, requiresLOS: true),
            "line" => Tile(range, requiresLOS: true), // Fallback for now
            "cone" => Tile(range, requiresLOS: true), // Fallback for now
            _ => Self()
        };
    }

    /// <summary>
    /// Creates a targeting definition from an item data.
    /// Uses smart defaults based on item type.
    /// </summary>
    public static TargetingDefinition FromItem(ItemData item)
    {
        // Check if item has explicit targeting data
        if (item.Targeting != null)
        {
            return FromItemTargeting(item.Targeting, item.GetTargetingRange());
        }

        // Smart defaults based on item type
        int range = item.GetTargetingRange();
        if (range <= 0) range = 5; // Default range

        // Items with effects typically target enemies
        if (item.Effects.Count > 0)
        {
            // Check if any effect is healing/beneficial
            bool isBeneficial = item.Effects.Exists(e =>
                e.Type?.ToLower() == "heal" ||
                e.Type?.ToLower() == "restore_wp");

            if (isBeneficial)
            {
                return Ally(range);
            }

            return Enemy(range);
        }

        // Default to enemy targeting
        return Enemy(range);
    }

    /// <summary>
    /// Creates a targeting definition from explicit item targeting data.
    /// </summary>
    private static TargetingDefinition FromItemTargeting(ItemTargeting targeting, int fallbackRange)
    {
        var type = targeting.Type?.ToLower() ?? "enemy";
        int range = targeting.Range > 0 ? targeting.Range : fallbackRange;
        bool los = targeting.RequiresLOS;

        return type switch
        {
            "self" => Self(),
            "enemy" => Enemy(range, los),
            "ally" => Ally(range, los),
            "tile" => Tile(range, los),
            "creature" => new TargetingDefinition
            {
                Type = TargetingType.Creature,
                Range = range,
                RequiresLOS = los,
                Metric = DistanceMetric.Chebyshev,
                Filter = TargetFilter.Creature
            },
            "area" => Area(range, targeting.AreaSize, los),
            _ => Enemy(range, los)
        };
    }
}
