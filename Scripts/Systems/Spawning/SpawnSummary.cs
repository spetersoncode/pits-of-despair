using System.Collections.Generic;
using System.Text;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Summary of spawning results for debugging and analytics.
/// Tracks budgets, creature counts, and regional distribution.
/// </summary>
public class SpawnSummary
{
    /// <summary>
    /// Floor depth this summary is for.
    /// </summary>
    public int FloorDepth { get; set; }

    /// <summary>
    /// Total power budget allocated for this floor.
    /// </summary>
    public int TotalPowerBudget { get; set; }

    /// <summary>
    /// Total threat actually spawned.
    /// </summary>
    public int TotalThreatSpawned { get; set; }

    /// <summary>
    /// Total item budget allocated.
    /// </summary>
    public int TotalItemBudget { get; set; }

    /// <summary>
    /// Items actually placed.
    /// </summary>
    public int ItemsPlaced { get; set; }

    /// <summary>
    /// Total gold budget allocated.
    /// </summary>
    public int TotalGoldBudget { get; set; }

    /// <summary>
    /// Gold actually placed.
    /// </summary>
    public int GoldPlaced { get; set; }

    /// <summary>
    /// Number of regions processed.
    /// </summary>
    public int RegionsProcessed { get; set; }

    /// <summary>
    /// Number of encounters placed.
    /// </summary>
    public int EncountersPlaced { get; set; }

    /// <summary>
    /// Total creatures spawned.
    /// </summary>
    public int CreaturesSpawned { get; set; }

    /// <summary>
    /// Unique monsters spawned.
    /// </summary>
    public List<string> UniqueSpawns { get; set; } = new();

    /// <summary>
    /// Out-of-depth spawn info (if triggered).
    /// </summary>
    public string OutOfDepthSpawn { get; set; }

    /// <summary>
    /// Theme distribution across regions.
    /// </summary>
    public Dictionary<string, int> ThemeDistribution { get; set; } = new();

    /// <summary>
    /// Encounter template distribution.
    /// </summary>
    public Dictionary<string, int> EncounterDistribution { get; set; } = new();

    /// <summary>
    /// Spawn warnings/errors encountered.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Position where stairs/throne was placed.
    /// </summary>
    public string StairsPosition { get; set; }

    /// <summary>
    /// Time taken for spawning (milliseconds).
    /// </summary>
    public long SpawnTimeMs { get; set; }

    /// <summary>
    /// Adds a warning message.
    /// </summary>
    public void AddWarning(string message)
    {
        Warnings.Add(message);
    }

    /// <summary>
    /// Records a theme used in a region.
    /// </summary>
    public void RecordTheme(string themeId)
    {
        if (string.IsNullOrEmpty(themeId))
            return;

        if (!ThemeDistribution.ContainsKey(themeId))
            ThemeDistribution[themeId] = 0;

        ThemeDistribution[themeId]++;
    }

    /// <summary>
    /// Records an encounter template used.
    /// </summary>
    public void RecordEncounter(string templateId)
    {
        if (string.IsNullOrEmpty(templateId))
            return;

        if (!EncounterDistribution.ContainsKey(templateId))
            EncounterDistribution[templateId] = 0;

        EncounterDistribution[templateId]++;
    }

    /// <summary>
    /// Calculates budget utilization percentage for power.
    /// </summary>
    public float PowerBudgetUtilization => TotalPowerBudget > 0
        ? (float)TotalThreatSpawned / TotalPowerBudget * 100f
        : 0f;

    /// <summary>
    /// Generates a formatted debug string.
    /// </summary>
    public string ToDebugString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Spawn Summary: Floor {FloorDepth} ===");
        sb.AppendLine($"Time: {SpawnTimeMs}ms");
        sb.AppendLine();

        sb.AppendLine("--- Budgets ---");
        sb.AppendLine($"Power:  {TotalThreatSpawned}/{TotalPowerBudget} ({PowerBudgetUtilization:F1}%)");
        sb.AppendLine($"Items:  {ItemsPlaced}/{TotalItemBudget}");
        sb.AppendLine($"Gold:   {GoldPlaced}/{TotalGoldBudget}");
        sb.AppendLine();

        sb.AppendLine("--- Spawns ---");
        sb.AppendLine($"Regions:    {RegionsProcessed}");
        sb.AppendLine($"Encounters: {EncountersPlaced}");
        sb.AppendLine($"Creatures:  {CreaturesSpawned}");
        sb.AppendLine($"Stairs:     {StairsPosition ?? "Not placed"}");
        sb.AppendLine();

        if (UniqueSpawns.Count > 0)
        {
            sb.AppendLine("--- Uniques ---");
            foreach (var unique in UniqueSpawns)
            {
                sb.AppendLine($"  - {unique}");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(OutOfDepthSpawn))
        {
            sb.AppendLine($"--- Out of Depth ---");
            sb.AppendLine($"  {OutOfDepthSpawn}");
            sb.AppendLine();
        }

        if (ThemeDistribution.Count > 0)
        {
            sb.AppendLine("--- Theme Distribution ---");
            foreach (var (theme, count) in ThemeDistribution)
            {
                sb.AppendLine($"  {theme}: {count} region(s)");
            }
            sb.AppendLine();
        }

        if (EncounterDistribution.Count > 0)
        {
            sb.AppendLine("--- Encounter Types ---");
            foreach (var (encounter, count) in EncounterDistribution)
            {
                sb.AppendLine($"  {encounter}: {count}");
            }
            sb.AppendLine();
        }

        if (Warnings.Count > 0)
        {
            sb.AppendLine("--- Warnings ---");
            foreach (var warning in Warnings)
            {
                sb.AppendLine($"  ! {warning}");
            }
        }

        return sb.ToString();
    }
}
