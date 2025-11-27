using System.Collections.Generic;
using PitsOfDespair.Generation.Config;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Runtime context for spawning, combining layout-dependent settings from Pipeline
/// with difficulty/content settings from Floor.
/// Created at spawn time and passed through spawning phases.
/// </summary>
public class SpawnContext
{
    #region From Pipeline (Layout-Dependent)

    /// <summary>
    /// Target item density as percentage of walkable tiles.
    /// </summary>
    public float ItemDensity { get; init; }

    /// <summary>
    /// Target gold pile density as percentage of walkable tiles.
    /// </summary>
    public float GoldDensity { get; init; }

    /// <summary>
    /// Chance for each region to receive an encounter.
    /// </summary>
    public float EncounterChance { get; init; }

    /// <summary>
    /// Maximum encounters as ratio of total regions.
    /// </summary>
    public float MaxEncounterRatio { get; init; }

    /// <summary>
    /// Minimum distance between encounter centers (in tiles).
    /// </summary>
    public int MinEncounterSpacing { get; init; }

    /// <summary>
    /// Maximum encounters per region.
    /// </summary>
    public int MaxEncountersPerRegion { get; init; }

    /// <summary>
    /// Radius around player start where no encounters spawn.
    /// </summary>
    public int PlayerExclusionRadius { get; init; }

    /// <summary>
    /// Multiplier for template weight when region type matches.
    /// </summary>
    public float RegionMatchMultiplier { get; init; }

    /// <summary>
    /// Multiplier for ambush template weight in dangerous regions.
    /// </summary>
    public float DangerBonusMultiplier { get; init; }

    /// <summary>
    /// Minimum creature count for floor validation.
    /// </summary>
    public int MinCreatureCount { get; init; }

    /// <summary>
    /// Encounter types suited to this layout with weights.
    /// </summary>
    public List<WeightedEntry> EncounterWeights { get; init; }

    #endregion

    #region From Floor (Difficulty/Content)

    /// <summary>
    /// Minimum threat rating for creatures.
    /// </summary>
    public int MinThreat { get; init; }

    /// <summary>
    /// Maximum threat rating for regular spawns.
    /// </summary>
    public int MaxThreat { get; init; }

    /// <summary>
    /// Minimum item value for this floor.
    /// </summary>
    public int MinItemValue { get; init; }

    /// <summary>
    /// Maximum item value for this floor.
    /// </summary>
    public int MaxItemValue { get; init; }

    /// <summary>
    /// Base gold amount per pile.
    /// </summary>
    public int BaseGoldPerPile { get; init; }

    /// <summary>
    /// Gold scaling per floor depth.
    /// </summary>
    public float GoldFloorScale { get; init; }

    /// <summary>
    /// Chance to spawn out-of-depth creatures.
    /// </summary>
    public float CreatureOutOfDepthChance { get; init; }

    /// <summary>
    /// Floors ahead for out-of-depth pulls.
    /// </summary>
    public int OutOfDepthFloors { get; init; }

    /// <summary>
    /// Faction themes available on this floor.
    /// </summary>
    public List<WeightedEntry> ThemeWeights { get; init; }

    /// <summary>
    /// Unique creatures for this floor.
    /// </summary>
    public List<UniqueSpawnEntry> UniqueCreatures { get; init; }

    #endregion

    #region Creature Selection Scoring

    /// <summary>
    /// Base score for archetype matching.
    /// </summary>
    public int BaseScore { get; init; }

    /// <summary>
    /// Score when creature has no matching archetypes.
    /// </summary>
    public int NoMatchScore { get; init; }

    /// <summary>
    /// Bonus for archetype matches.
    /// </summary>
    public int MatchScoreBonus { get; init; }

    /// <summary>
    /// Bonus for role keyword matches.
    /// </summary>
    public int RoleKeywordBonus { get; init; }

    #endregion

    /// <summary>
    /// Creates a SpawnContext by merging Pipeline and Floor configurations.
    /// </summary>
    public static SpawnContext Create(PipelineConfig pipeline, FloorConfig floor)
    {
        var spawnSettings = pipeline.SpawnSettings ?? new PipelineSpawnSettings();
        var creatureSelection = floor.CreatureSelection ?? new CreatureSelectionConfig();

        return new SpawnContext
        {
            // From Pipeline
            ItemDensity = spawnSettings.ItemDensity,
            GoldDensity = spawnSettings.GoldDensity,
            EncounterChance = spawnSettings.EncounterChance,
            MaxEncounterRatio = spawnSettings.MaxEncounterRatio,
            MinEncounterSpacing = spawnSettings.MinEncounterSpacing,
            MaxEncountersPerRegion = spawnSettings.MaxEncountersPerRegion,
            PlayerExclusionRadius = spawnSettings.PlayerExclusionRadius,
            RegionMatchMultiplier = spawnSettings.RegionMatchMultiplier,
            DangerBonusMultiplier = spawnSettings.DangerBonusMultiplier,
            MinCreatureCount = spawnSettings.MinCreatureCount,
            EncounterWeights = spawnSettings.EncounterWeights ?? new List<WeightedEntry>(),

            // From Floor
            MinThreat = floor.MinThreat,
            MaxThreat = floor.MaxThreat,
            MinItemValue = floor.GetMinItemValue(),
            MaxItemValue = floor.GetMaxItemValue(),
            BaseGoldPerPile = floor.BaseGoldPerPile,
            GoldFloorScale = floor.GoldFloorScale,
            CreatureOutOfDepthChance = floor.CreatureOutOfDepthChance,
            OutOfDepthFloors = floor.OutOfDepthFloors,
            ThemeWeights = floor.ThemeWeights ?? new List<WeightedEntry>(),
            UniqueCreatures = floor.UniqueCreatures ?? new List<UniqueSpawnEntry>(),

            // Creature Selection
            BaseScore = creatureSelection.BaseScore,
            NoMatchScore = creatureSelection.NoMatchScore,
            MatchScoreBonus = creatureSelection.MatchScoreBonus,
            RoleKeywordBonus = creatureSelection.RoleKeywordBonus
        };
    }

}
