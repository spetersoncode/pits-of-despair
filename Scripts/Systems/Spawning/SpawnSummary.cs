using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Summary of spawning results for debugging and analytics.
/// Tracks creature counts, threat totals, and regional distribution.
/// </summary>
public class SpawnSummary
{
    /// <summary>
    /// Floor depth this summary is for.
    /// </summary>
    public int FloorDepth { get; set; }

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
    /// Total decorations spawned.
    /// </summary>
    public int DecorationsPlaced { get; set; }

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
    /// Generates debug log lines. Each line should be printed separately.
    /// </summary>
    public IEnumerable<string> GetDebugLines()
    {
        yield return $"[SpawnSummary] Floor {FloorDepth} ({SpawnTimeMs}ms): Threat {TotalThreatSpawned}, Items {ItemsPlaced}/{TotalItemBudget}, Gold {GoldPlaced}/{TotalGoldBudget}";
        yield return $"[SpawnSummary] {RegionsProcessed} regions, {EncountersPlaced} encounters, {CreaturesSpawned} creatures, {DecorationsPlaced} decorations, stairs {StairsPosition ?? "none"}";

        if (UniqueSpawns.Count > 0)
            yield return $"[SpawnSummary] Uniques: {string.Join(", ", UniqueSpawns)}";

        if (!string.IsNullOrEmpty(OutOfDepthSpawn))
            yield return $"[SpawnSummary] Out-of-depth: {OutOfDepthSpawn}";

        if (ThemeDistribution.Count > 0)
            yield return $"[SpawnSummary] Themes: {string.Join(", ", ThemeDistribution.Select(t => $"{t.Key}:{t.Value}"))}";

        if (EncounterDistribution.Count > 0)
            yield return $"[SpawnSummary] Encounters: {string.Join(", ", EncounterDistribution.Select(e => $"{e.Key}:{e.Value}"))}";

        foreach (var warning in Warnings)
            yield return $"[SpawnSummary] Warning: {warning}";
    }
}
