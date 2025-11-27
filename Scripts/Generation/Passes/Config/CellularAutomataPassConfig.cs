using System.Collections.Generic;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes.Config;

/// <summary>
/// Configuration for Cellular Automata generation pass.
/// Supports both base generator mode and modifier mode.
/// </summary>
public class CellularAutomataPassConfig
{
    /// <summary>
    /// Role override. If "modifier", operates on existing topology.
    /// If "base" or unset, acts as primary generator.
    /// </summary>
    public PassRole Role { get; set; } = PassRole.Base;

    /// <summary>
    /// Initial fill percentage of floor tiles (base mode only).
    /// Higher values = more initial floor tiles.
    /// </summary>
    public int FillPercent { get; set; } = 45;

    /// <summary>
    /// Number of CA smoothing iterations.
    /// </summary>
    public int Iterations { get; set; } = 5;

    /// <summary>
    /// Neighbor count threshold for a wall to become floor.
    /// If wall has >= birthLimit floor neighbors, it becomes floor.
    /// </summary>
    public int BirthLimit { get; set; } = 4;

    /// <summary>
    /// Neighbor count threshold for floor to become wall.
    /// If floor has < deathLimit floor neighbors, it becomes wall.
    /// </summary>
    public int DeathLimit { get; set; } = 3;

    /// <summary>
    /// Additional smoothing iterations after main CA (removes isolated tiles).
    /// </summary>
    public int SmoothingIterations { get; set; } = 2;

    // Modifier mode settings

    /// <summary>
    /// Target regions configuration (modifier mode only).
    /// </summary>
    public TargetRegionsConfig TargetRegions { get; set; }

    /// <summary>
    /// Create config from PassConfig dictionary.
    /// </summary>
    public static CellularAutomataPassConfig FromPassConfig(PassConfig passConfig)
    {
        var config = new CellularAutomataPassConfig();

        if (passConfig?.Config != null)
        {
            // Role
            if (passConfig.Config.TryGetValue("role", out var roleVal) && roleVal != null)
            {
                if (System.Enum.TryParse<PassRole>(roleVal.ToString(), true, out var role))
                    config.Role = role;
            }

            config.FillPercent = passConfig.GetConfigValue("fillPercent", config.FillPercent);
            config.Iterations = passConfig.GetConfigValue("iterations", config.Iterations);
            config.BirthLimit = passConfig.GetConfigValue("birthLimit", config.BirthLimit);
            config.DeathLimit = passConfig.GetConfigValue("deathLimit", config.DeathLimit);
            config.SmoothingIterations = passConfig.GetConfigValue("smoothingIterations", config.SmoothingIterations);

            // Parse target regions if present
            if (passConfig.Config.TryGetValue("targetRegions", out var targetVal) && targetVal is Dictionary<object, object> targetDict)
            {
                config.TargetRegions = ParseTargetRegions(targetDict);
            }
        }

        return config;
    }

    private static TargetRegionsConfig ParseTargetRegions(Dictionary<object, object> dict)
    {
        var target = new TargetRegionsConfig();

        if (dict.TryGetValue("type", out var typeVal))
            target.Type = typeVal?.ToString() ?? "random";

        if (dict.TryGetValue("count", out var countVal) && countVal != null)
        {
            if (int.TryParse(countVal.ToString(), out var count))
                target.Count = count;
        }

        if (dict.TryGetValue("filter", out var filterVal) && filterVal is Dictionary<object, object> filterDict)
        {
            if (filterDict.TryGetValue("minArea", out var minAreaVal) && minAreaVal != null)
            {
                if (int.TryParse(minAreaVal.ToString(), out var minArea))
                    target.MinArea = minArea;
            }
        }

        return target;
    }
}

/// <summary>
/// Configuration for targeting specific regions in modifier mode.
/// </summary>
public class TargetRegionsConfig
{
    /// <summary>
    /// Selection type: "random", "all", "tagged"
    /// </summary>
    public string Type { get; set; } = "random";

    /// <summary>
    /// Number of regions to target (for "random" type).
    /// </summary>
    public int Count { get; set; } = 1;

    /// <summary>
    /// Minimum area filter for target regions.
    /// </summary>
    public int MinArea { get; set; } = 0;

    /// <summary>
    /// Tag filter (for "tagged" type).
    /// </summary>
    public string Tag { get; set; }
}
