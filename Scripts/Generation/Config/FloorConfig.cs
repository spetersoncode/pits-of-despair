using System;
using System.Collections.Generic;
using PitsOfDespair.Generation.Spawning.Data;

namespace PitsOfDespair.Generation.Config;

/// <summary>
/// Configuration for creature selection scoring.
/// </summary>
public class CreatureSelectionConfig
{
    /// <summary>
    /// Base score for archetype matching when no preference specified.
    /// Default: 50 (neutral score)
    /// </summary>
    public int BaseScore { get; set; } = 50;

    /// <summary>
    /// Score when creature has no matching archetypes.
    /// Default: 10 (low but non-zero)
    /// </summary>
    public int NoMatchScore { get; set; } = 10;

    /// <summary>
    /// Bonus score range for archetype matches.
    /// Default: 50
    /// </summary>
    public int MatchScoreBonus { get; set; } = 50;

    /// <summary>
    /// Bonus score for role keyword matches (e.g., "leader", "scout").
    /// Default: 20
    /// </summary>
    public int RoleKeywordBonus { get; set; } = 20;
}

/// <summary>
/// Floor configuration defining difficulty curve and content composition.
/// Loaded from Data/Floors/*.yaml
/// </summary>
public class FloorConfig
{
    /// <summary>
    /// Display name for this floor or floor range.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Floor depth this config applies to. Null means this is a fallback config.
    /// </summary>
    public int? Floor { get; set; }

    /// <summary>
    /// Single pipeline ID to use. If null, check Pipelines for weighted selection.
    /// </summary>
    public string Pipeline { get; set; }

    /// <summary>
    /// Weighted list of pipelines for random selection.
    /// Used when Pipeline is null.
    /// </summary>
    public List<WeightedEntry> Pipelines { get; set; }

    /// <summary>
    /// Minimum threat rating for creatures on this floor.
    /// </summary>
    public int MinThreat { get; set; } = 0;

    /// <summary>
    /// Maximum threat rating for regular spawns on this floor.
    /// </summary>
    public int MaxThreat { get; set; } = 999;

    /// <summary>
    /// Minimum item value for this floor. If null, defaults to MinThreat.
    /// </summary>
    public int? MinItemValue { get; set; } = null;

    /// <summary>
    /// Maximum item value for this floor. If null, defaults to MaxThreat.
    /// </summary>
    public int? MaxItemValue { get; set; } = null;

    /// <summary>
    /// Chance (0.0-1.0) to spawn an out-of-depth creature.
    /// </summary>
    public float CreatureOutOfDepthChance { get; set; } = 0.0f;

    /// <summary>
    /// How many floors ahead to pull out-of-depth creatures from.
    /// </summary>
    public int OutOfDepthFloors { get; set; } = 2;

    /// <summary>
    /// Weighted list of faction themes available on this floor.
    /// </summary>
    public List<WeightedEntry> ThemeWeights { get; set; } = new();

    /// <summary>
    /// Unique creatures with spawn chances for this floor.
    /// </summary>
    public List<UniqueSpawnEntry> UniqueCreatures { get; set; } = new();

    /// <summary>
    /// Optional creature selection scoring configuration.
    /// If null, uses defaults.
    /// </summary>
    public CreatureSelectionConfig CreatureSelection { get; set; }

    /// <summary>
    /// Gets the effective minimum item value (defaults to MinThreat if not specified).
    /// </summary>
    public int GetMinItemValue() => MinItemValue ?? MinThreat;

    /// <summary>
    /// Gets the effective maximum item value (defaults to MaxThreat if not specified).
    /// </summary>
    public int GetMaxItemValue() => MaxItemValue ?? MaxThreat;

    /// <summary>
    /// Selects a pipeline ID, either from the single Pipeline property or weighted random from Pipelines.
    /// </summary>
    /// <param name="rng">Random number generator for weighted selection.</param>
    /// <returns>Pipeline ID to use, or "bsp_standard" as fallback.</returns>
    public string SelectPipeline(Random rng)
    {
        // Single pipeline reference takes priority
        if (!string.IsNullOrEmpty(Pipeline))
            return Pipeline;

        // Weighted random selection
        if (Pipelines != null && Pipelines.Count > 0)
        {
            int totalWeight = 0;
            foreach (var entry in Pipelines)
                totalWeight += entry.Weight;

            if (totalWeight > 0)
            {
                int roll = rng.Next(totalWeight);
                int cumulative = 0;
                foreach (var entry in Pipelines)
                {
                    cumulative += entry.Weight;
                    if (roll < cumulative)
                        return entry.Id;
                }
            }
        }

        // Fallback
        return "bsp_standard";
    }
}
