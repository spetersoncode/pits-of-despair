using System.Collections.Generic;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Types of encounters that can be spawned.
/// Each type has different composition rules and AI configurations.
/// </summary>
public enum EncounterType
{
    /// <summary>Leader at/near centroid with followers distributed throughout region.</summary>
    Lair,
    /// <summary>Small group (2-4) spawning in passages/edges with patrol waypoints.</summary>
    Patrol,
    /// <summary>Creatures near chokepoints, initially sleeping, trigger on proximity.</summary>
    Ambush,
    /// <summary>1-2 creatures at chokepoint holding position.</summary>
    GuardPost,
    /// <summary>Guardian(s) placed near valuable item. Risk = Reward.</summary>
    TreasureGuard,
    /// <summary>Many weak creatures spread throughout region, no leader.</summary>
    Infestation,
    /// <summary>Alpha + pack members that follow the alpha.</summary>
    Pack
}

/// <summary>
/// Defines a slot in an encounter composition.
/// Slots specify archetype requirements and count ranges.
/// </summary>
public class EncounterSlot
{
    /// <summary>
    /// Role requirement for this slot (e.g., "leader", "follower", "minion", "elite").
    /// Used to select creatures from the faction theme.
    /// </summary>
    public string Role { get; set; } = "any";

    /// <summary>
    /// Preferred archetypes for this slot (e.g., ["tank", "warrior"]).
    /// If empty, any archetype is acceptable.
    /// </summary>
    public List<string> PreferredArchetypes { get; set; } = new();

    /// <summary>
    /// Minimum count for this slot (dice notation supported).
    /// </summary>
    public string MinCount { get; set; } = "1";

    /// <summary>
    /// Maximum count for this slot (dice notation supported).
    /// </summary>
    public string MaxCount { get; set; } = "1";

    /// <summary>
    /// Threat budget multiplier for this slot.
    /// Leader slots might have 1.5x, minion slots 0.5x.
    /// </summary>
    public float ThreatMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Placement strategy for this slot (e.g., "center", "surrounding", "edge").
    /// </summary>
    public string Placement { get; set; } = "random";
}

/// <summary>
/// YAML-configurable encounter template.
/// Defines the composition and behavior of an encounter type.
/// </summary>
public class EncounterTemplate
{
    /// <summary>
    /// Unique identifier for this template.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for debugging/logging.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of encounter (determines AI configuration and placement).
    /// </summary>
    public EncounterType Type { get; set; } = EncounterType.Lair;

    /// <summary>
    /// Minimum threat budget required to spawn this encounter.
    /// </summary>
    public int MinBudget { get; set; } = 1;

    /// <summary>
    /// Maximum threat budget this encounter can consume.
    /// </summary>
    public int MaxBudget { get; set; } = 100;

    /// <summary>
    /// Minimum region size (tile count) required for this encounter.
    /// </summary>
    public int MinRegionSize { get; set; } = 9;

    /// <summary>
    /// Slots defining the composition of this encounter.
    /// </summary>
    public List<EncounterSlot> Slots { get; set; } = new();

    /// <summary>
    /// Placement preferences for the encounter as a whole.
    /// </summary>
    public EncounterPlacement Placement { get; set; } = new();

    /// <summary>
    /// AI behavior configuration for spawned creatures.
    /// </summary>
    public EncounterAIConfig AIConfig { get; set; } = new();
}

/// <summary>
/// Placement preferences for an encounter.
/// </summary>
public class EncounterPlacement
{
    /// <summary>
    /// Preferred region types (e.g., ["large", "dead_end", "passage"]).
    /// Empty means any region is acceptable.
    /// </summary>
    public List<string> PreferredRegions { get; set; } = new();

    /// <summary>
    /// Minimum distance from entrance (tiles).
    /// </summary>
    public int MinDistanceFromEntrance { get; set; } = 0;

    /// <summary>
    /// Whether this encounter prefers chokepoint-adjacent regions.
    /// </summary>
    public bool PreferNearChokepoints { get; set; } = false;

    /// <summary>
    /// Whether this encounter prefers edge tiles of regions.
    /// </summary>
    public bool PreferEdges { get; set; } = false;
}

/// <summary>
/// AI configuration for encounter creatures.
/// </summary>
public class EncounterAIConfig
{
    /// <summary>
    /// Initial state for creatures (e.g., "idle", "sleeping", "patrolling", "guarding").
    /// </summary>
    public string InitialState { get; set; } = "idle";

    /// <summary>
    /// Whether followers should protect the leader.
    /// </summary>
    public bool FollowersProtectLeader { get; set; } = false;

    /// <summary>
    /// Whether creatures should stay in their spawn region.
    /// </summary>
    public bool TerritoryBound { get; set; } = false;

    /// <summary>
    /// Whether the leader can yell for help.
    /// </summary>
    public bool LeaderYellsForHelp { get; set; } = false;

    /// <summary>
    /// Whether to generate patrol waypoints for this encounter.
    /// </summary>
    public bool GeneratePatrolRoute { get; set; } = false;

    /// <summary>
    /// Wake radius for sleeping creatures (0 = default vision range).
    /// </summary>
    public int WakeRadius { get; set; } = 0;
}
