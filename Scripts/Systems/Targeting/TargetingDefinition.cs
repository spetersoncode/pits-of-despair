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
    /// Maximum range for targeting.
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    /// For Area targeting, the radius of the effect around the target point.
    /// </summary>
    public int Radius { get; init; } = 0;

    /// <summary>
    /// Whether line-of-sight is required for valid targets.
    /// </summary>
    public bool RequiresLOS { get; init; } = true;

    /// <summary>
    /// The distance metric to use for range calculation.
    /// Euclidean for circular range (ranged weapons), Chebyshev for grid range (melee/reach).
    /// </summary>
    public DistanceMetric Metric { get; init; } = DistanceMetric.Chebyshev;

    /// <summary>
    /// What types of targets are valid (for Creature targeting).
    /// </summary>
    public TargetFilter Filter { get; init; } = TargetFilter.Enemy;

    /// <summary>
    /// Whether this targeting type requires player input to select a target.
    /// Always true - self-targeting is handled at action level, not targeting level.
    /// </summary>
    public bool RequiresSelection => true;

    /// <summary>
    /// Creates a targeting definition for creature targeting.
    /// This is the primary factory for targeting enemies, allies, or any creature.
    /// </summary>
    public static TargetingDefinition Creature(
        int range,
        TargetFilter filter = TargetFilter.Enemy,
        DistanceMetric metric = DistanceMetric.Chebyshev,
        bool requiresLOS = true) => new()
    {
        Type = TargetingType.Creature,
        Range = range,
        RequiresLOS = requiresLOS,
        Metric = metric,
        Filter = filter
    };

    /// <summary>
    /// Creates a targeting definition for tile targeting within range.
    /// </summary>
    public static TargetingDefinition Tile(int range, bool requiresLOS = true) => new()
    {
        Type = TargetingType.Tile,
        Range = range,
        RequiresLOS = requiresLOS,
        Metric = DistanceMetric.Euclidean,
        Filter = TargetFilter.Tile
    };

    /// <summary>
    /// Creates a targeting definition for area-of-effect targeting.
    /// </summary>
    public static TargetingDefinition Area(int range, int radius, bool requiresLOS = true) => new()
    {
        Type = TargetingType.Area,
        Range = range,
        Radius = radius,
        RequiresLOS = requiresLOS,
        Metric = DistanceMetric.Euclidean,
        Filter = TargetFilter.Tile
    };

    /// <summary>
    /// Creates a targeting definition for line/beam targeting.
    /// </summary>
    /// <param name="range">Maximum range in tiles</param>
    /// <param name="requiresLOS">Whether line-of-sight is required</param>
    /// <param name="metric">Distance metric (Euclidean for beams, Chebyshev for movement)</param>
    public static TargetingDefinition Line(int range, bool requiresLOS = true, DistanceMetric metric = DistanceMetric.Euclidean) => new()
    {
        Type = TargetingType.Line,
        Range = range,
        RequiresLOS = requiresLOS,
        Metric = metric,
        Filter = TargetFilter.Tile
    };

    /// <summary>
    /// Creates a targeting definition for cone targeting.
    /// </summary>
    public static TargetingDefinition Cone(int range, int radius = 2, bool requiresLOS = true) => new()
    {
        Type = TargetingType.Cone,
        Range = range,
        Radius = radius,
        RequiresLOS = requiresLOS,
        Metric = DistanceMetric.Euclidean,
        Filter = TargetFilter.Tile
    };

    /// <summary>
    /// Creates a targeting definition for cleave attacks (3-tile arc).
    /// </summary>
    public static TargetingDefinition Cleave() => new()
    {
        Type = TargetingType.Cleave,
        Range = 1,
        RequiresLOS = false,
        Metric = DistanceMetric.Chebyshev,
        Filter = TargetFilter.Tile
    };

    /// <summary>
    /// Creates a targeting definition from a skill definition.
    /// </summary>
    public static TargetingDefinition FromSkill(SkillDefinition skill)
    {
        return FromSkill(skill, skill.Range, skill.Radius);
    }

    /// <summary>
    /// Creates a targeting definition from a skill definition with effective values.
    /// Use this overload when the caster has improvement skills that modify range/radius.
    /// </summary>
    /// <param name="skill">The skill definition</param>
    /// <param name="effectiveRange">The effective range after applying improvements</param>
    /// <param name="effectiveRadius">The effective radius after applying improvements</param>
    public static TargetingDefinition FromSkill(SkillDefinition skill, int effectiveRange, int effectiveRadius)
    {
        var skillTargeting = skill.Targeting?.ToLower() ?? "creature";
        int range = effectiveRange > 0 ? effectiveRange : 1;
        int radius = effectiveRadius;

        // Movement skills use Chebyshev for 8-directional targeting
        bool isMovementSkill = skill.Tags.Contains("movement");

        return skillTargeting switch
        {
            "self" => Creature(0, TargetFilter.Ally, DistanceMetric.Chebyshev, false), // Self handled at action level
            "adjacent" => Creature(1, TargetFilter.Enemy, DistanceMetric.Chebyshev, false),
            "melee" => Creature(1, TargetFilter.Enemy, DistanceMetric.Chebyshev, false),
            "reach" => Creature(range, TargetFilter.Enemy, DistanceMetric.Chebyshev),
            "ranged" => Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean),
            "enemy" => Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean),
            "ally" => Creature(range, TargetFilter.Ally, DistanceMetric.Euclidean),
            "creature" => Creature(range, TargetFilter.Creature, DistanceMetric.Euclidean),
            "cleave" => Cleave(),
            "tile" => Tile(range),
            "area" => Area(range, radius),
            "line" => Line(range, true, isMovementSkill ? DistanceMetric.Chebyshev : DistanceMetric.Euclidean),
            "cone" => Cone(range, radius > 0 ? radius : 2),
            _ => Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean)
        };
    }

    /// <summary>
    /// Creates a targeting definition from an item data.
    /// </summary>
    public static TargetingDefinition FromItem(ItemData item)
    {
        if (item.Targeting != null)
        {
            return FromItemTargeting(item.Targeting, item.GetTargetingRange());
        }

        int range = item.GetTargetingRange();
        if (range <= 0) range = 5;

        if (item.Effects.Count > 0)
        {
            bool isBeneficial = item.Effects.Exists(e =>
                e.Type?.ToLower() == "heal" ||
                e.Type?.ToLower() == "restore_wp");

            if (isBeneficial)
            {
                return Creature(range, TargetFilter.Ally, DistanceMetric.Euclidean);
            }

            return Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean);
        }

        return Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean);
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
            "self" => Creature(0, TargetFilter.Ally, DistanceMetric.Chebyshev, false),
            "enemy" => Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean, los),
            "ally" => Creature(range, TargetFilter.Ally, DistanceMetric.Euclidean, los),
            "creature" => Creature(range, TargetFilter.Creature, DistanceMetric.Euclidean, los),
            "adjacent" => Creature(1, TargetFilter.Enemy, DistanceMetric.Chebyshev, false),
            "melee" => Creature(1, TargetFilter.Enemy, DistanceMetric.Chebyshev, false),
            "reach" => Creature(range, TargetFilter.Enemy, DistanceMetric.Chebyshev, los),
            "ranged" => Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean, los),
            "tile" => Tile(range, los),
            "area" => Area(range, targeting.Radius, los),
            "line" => Line(range, los),
            "cone" => Cone(range, targeting.Radius, los),
            _ => Creature(range, TargetFilter.Enemy, DistanceMetric.Euclidean, los)
        };
    }
}
