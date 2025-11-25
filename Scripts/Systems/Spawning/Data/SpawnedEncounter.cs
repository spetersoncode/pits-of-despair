using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Information about a spawned creature within an encounter.
/// </summary>
public class SpawnedCreature
{
    /// <summary>
    /// The spawned entity.
    /// </summary>
    public BaseEntity Entity { get; set; }

    /// <summary>
    /// Creature ID from data.
    /// </summary>
    public string CreatureId { get; set; } = string.Empty;

    /// <summary>
    /// Grid position where the creature was spawned.
    /// </summary>
    public GridPosition Position { get; set; }

    /// <summary>
    /// Inferred archetype(s) for this creature.
    /// </summary>
    public List<CreatureArchetype> Archetypes { get; set; } = new();

    /// <summary>
    /// Role within the encounter (e.g., "leader", "follower", "minion").
    /// </summary>
    public string Role { get; set; } = "follower";

    /// <summary>
    /// Threat rating of this creature.
    /// </summary>
    public int Threat { get; set; } = 1;
}

/// <summary>
/// Result structure for a spawned encounter.
/// Contains all spawned entities and metadata for AI configuration.
/// </summary>
public class SpawnedEncounter
{
    /// <summary>
    /// The template that was used to spawn this encounter.
    /// </summary>
    public EncounterTemplate Template { get; set; }

    /// <summary>
    /// The faction theme used for this encounter.
    /// </summary>
    public FactionTheme Theme { get; set; }

    /// <summary>
    /// The region this encounter was spawned in.
    /// </summary>
    public Region Region { get; set; }

    /// <summary>
    /// All creatures spawned in this encounter.
    /// </summary>
    public List<SpawnedCreature> Creatures { get; set; } = new();

    /// <summary>
    /// The leader creature, if this encounter has one.
    /// </summary>
    public SpawnedCreature Leader { get; set; }

    /// <summary>
    /// Total threat consumed by this encounter.
    /// </summary>
    public int TotalThreat { get; set; } = 0;

    /// <summary>
    /// Center position of the encounter (for patrol routes, territory).
    /// </summary>
    public GridPosition CenterPosition { get; set; }

    /// <summary>
    /// Whether spawning was successful.
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// Error message if spawning failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets all spawned entities.
    /// </summary>
    public IEnumerable<BaseEntity> GetAllEntities()
    {
        foreach (var creature in Creatures)
        {
            if (creature.Entity != null)
                yield return creature.Entity;
        }
    }

    /// <summary>
    /// Gets the count of spawned creatures.
    /// </summary>
    public int CreatureCount => Creatures.Count;
}

