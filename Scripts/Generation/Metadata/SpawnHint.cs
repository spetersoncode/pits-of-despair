using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Hint for the spawning system from prefabs or generation passes.
/// Maps to existing ISpawnStrategy/IPlacementStrategy patterns.
/// </summary>
public class SpawnHint
{
    /// <summary>
    /// Semantic tag (e.g., "guardian", "treasure", "trap").
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// Spawn type mapped to SpawnEntryType (e.g., "single", "band", "unique").
    /// </summary>
    public string SpawnType { get; set; }

    /// <summary>
    /// Pool of creature IDs to select from.
    /// </summary>
    public List<string> CreaturePool { get; set; }

    /// <summary>
    /// Specific item ID to spawn.
    /// </summary>
    public string ItemId { get; set; }

    /// <summary>
    /// Item pool ID to select from.
    /// </summary>
    public string ItemPool { get; set; }

    /// <summary>
    /// Placement strategy name (e.g., "center", "random", "surrounding").
    /// </summary>
    public string Placement { get; set; }

    /// <summary>
    /// Specific position if pre-defined (from prefab spawn points).
    /// </summary>
    public GridPosition? Position { get; set; }

    /// <summary>
    /// Count of entities to spawn (dice notation or fixed).
    /// </summary>
    public string Count { get; set; } = "1";

    /// <summary>
    /// Override faction theme ID for this region.
    /// If set, the region will use this theme instead of random selection.
    /// </summary>
    public string ThemeId { get; set; }

    /// <summary>
    /// Override encounter template ID.
    /// If set, forces this specific encounter type.
    /// </summary>
    public string EncounterTemplateId { get; set; }
}
