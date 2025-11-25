using System.Collections.Generic;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Weighted entry for theme or encounter selection.
/// </summary>
public class WeightedEntry
{
    /// <summary>
    /// ID of the theme or encounter template.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Selection weight (higher = more likely).
    /// </summary>
    public int Weight { get; set; } = 1;
}

/// <summary>
/// Item rarity levels for loot distribution.
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}

/// <summary>
/// Configuration for item spawning on a floor.
/// Defines item pools by rarity and distribution rules.
/// </summary>
public class ItemSpawnConfig
{
    /// <summary>
    /// Item IDs available at common rarity on this floor.
    /// </summary>
    public List<string> CommonItems { get; set; } = new();

    /// <summary>
    /// Item IDs available at uncommon rarity on this floor.
    /// </summary>
    public List<string> UncommonItems { get; set; } = new();

    /// <summary>
    /// Item IDs available at rare rarity on this floor.
    /// </summary>
    public List<string> RareItems { get; set; } = new();

    /// <summary>
    /// Item IDs available at epic rarity on this floor.
    /// </summary>
    public List<string> EpicItems { get; set; } = new();

    /// <summary>
    /// Chance for unguarded items to spawn (0.0-1.0).
    /// </summary>
    public float UnguardedItemChance { get; set; } = 0.3f;

    /// <summary>
    /// Minimum item rarity for treasure guard encounters.
    /// </summary>
    public ItemRarity MinGuardedRarity { get; set; } = ItemRarity.Uncommon;

    /// <summary>
    /// Weight for common item selection (default 60).
    /// </summary>
    public int CommonWeight { get; set; } = 60;

    /// <summary>
    /// Weight for uncommon item selection (default 30).
    /// </summary>
    public int UncommonWeight { get; set; } = 30;

    /// <summary>
    /// Weight for rare item selection (default 9).
    /// </summary>
    public int RareWeight { get; set; } = 9;

    /// <summary>
    /// Weight for epic item selection (default 1).
    /// </summary>
    public int EpicWeight { get; set; } = 1;
}

/// <summary>
/// Configuration for spawning on a specific floor or floor range.
/// Replaces SpawnTableData with power-budget-based system.
/// </summary>
public class FloorSpawnConfig
{
    /// <summary>
    /// Unique identifier for this config.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this config.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Minimum floor depth this config applies to.
    /// </summary>
    public int MinFloor { get; set; } = 1;

    /// <summary>
    /// Maximum floor depth this config applies to.
    /// </summary>
    public int MaxFloor { get; set; } = 99;

    /// <summary>
    /// Total creature threat budget for the floor (dice notation).
    /// Example: "3d6+10" for 13-28 total threat points.
    /// </summary>
    public string PowerBudget { get; set; } = "2d6+5";

    /// <summary>
    /// Item spawn budget (dice notation).
    /// </summary>
    public string ItemBudget { get; set; } = "1d4+2";

    /// <summary>
    /// Gold spawn budget (dice notation).
    /// </summary>
    public string GoldBudget { get; set; } = "3d10+10";

    /// <summary>
    /// Weighted list of faction themes available on this floor.
    /// </summary>
    public List<WeightedEntry> ThemeWeights { get; set; } = new();

    /// <summary>
    /// Weighted list of encounter templates available on this floor.
    /// </summary>
    public List<WeightedEntry> EncounterWeights { get; set; } = new();

    /// <summary>
    /// Chance (0.0-1.0) to spawn an out-of-depth creature.
    /// </summary>
    public float OutOfDepthChance { get; set; } = 0.0f;

    /// <summary>
    /// How many floors ahead to pull out-of-depth creatures from.
    /// </summary>
    public int OutOfDepthFloors { get; set; } = 2;

    /// <summary>
    /// Minimum threat rating for creatures on this floor.
    /// Creatures below this are filtered out.
    /// </summary>
    public int MinThreat { get; set; } = 0;

    /// <summary>
    /// Maximum threat rating for regular spawns on this floor.
    /// Higher threat creatures only appear via out-of-depth mechanic.
    /// </summary>
    public int MaxThreat { get; set; } = 999;

    /// <summary>
    /// Item spawning configuration for this floor.
    /// Defines item pools by rarity and distribution rules.
    /// </summary>
    public ItemSpawnConfig Items { get; set; } = new();

    /// <summary>
    /// Rolls the power budget using dice notation.
    /// </summary>
    public int RollPowerBudget()
    {
        return DiceRoller.Roll(PowerBudget);
    }

    /// <summary>
    /// Rolls the item budget using dice notation.
    /// </summary>
    public int RollItemBudget()
    {
        return DiceRoller.Roll(ItemBudget);
    }

    /// <summary>
    /// Rolls the gold budget using dice notation.
    /// </summary>
    public int RollGoldBudget()
    {
        return DiceRoller.Roll(GoldBudget);
    }
}
