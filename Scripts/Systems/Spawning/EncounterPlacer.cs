using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Selects and places encounters within regions based on budget and templates.
/// </summary>
public class EncounterPlacer
{
    private readonly DataLoader _dataLoader;
    private readonly RandomNumberGenerator _rng;

    /// <summary>
    /// Minimum distance (squared) between encounter centers.
    /// </summary>
    private const int MinEncounterSpacingSquared = 36; // 6 tiles

    public EncounterPlacer(DataLoader dataLoader)
    {
        _dataLoader = dataLoader;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Places encounters in a region until budget is exhausted or no valid encounters fit.
    /// </summary>
    /// <param name="region">The region to place encounters in</param>
    /// <param name="spawnData">Spawn data for this region</param>
    /// <param name="config">Floor spawn configuration</param>
    /// <param name="occupiedPositions">Already occupied positions (updated as encounters are placed)</param>
    /// <returns>List of encounters placed in this region</returns>
    public List<SpawnedEncounter> PlaceEncountersInRegion(
        Region region,
        RegionSpawnData spawnData,
        FloorSpawnConfig config,
        HashSet<Vector2I> occupiedPositions)
    {
        var placedEncounters = new List<SpawnedEncounter>();

        if (spawnData.Theme == null || spawnData.RemainingBudget <= 0)
        {
            spawnData.IsProcessed = true;
            return placedEncounters;
        }

        // Get available encounter templates for this floor
        var availableTemplates = GetAvailableTemplates(config, region);
        if (availableTemplates.Count == 0)
        {
            GD.PushWarning($"EncounterPlacer: No encounter templates available for region {region.Id}");
            spawnData.IsProcessed = true;
            return placedEncounters;
        }

        // Track encounter centers for spacing
        var encounterCenters = new List<GridPosition>();
        int maxAttempts = 10;
        int attempts = 0;

        while (spawnData.RemainingBudget > 0 && attempts < maxAttempts)
        {
            attempts++;

            // Select an encounter template that fits the remaining budget
            var template = SelectTemplate(availableTemplates, spawnData, region);
            if (template == null)
            {
                break; // No templates fit remaining budget
            }

            // Find a valid position for this encounter
            var position = FindEncounterPosition(region, template, encounterCenters, occupiedPositions);
            if (position == null)
            {
                continue; // No valid position, try different template
            }

            // Create the encounter result (actual creature spawning happens later)
            var encounter = new SpawnedEncounter
            {
                Template = template,
                Theme = spawnData.Theme,
                Region = region,
                CenterPosition = position.Value,
                Success = true
            };

            // Calculate threat cost for this encounter
            int threatCost = CalculateEncounterThreatCost(template, spawnData);

            // Consume budget
            if (!spawnData.ConsumeBudget(threatCost))
            {
                break;
            }

            encounter.TotalThreat = threatCost;
            encounterCenters.Add(position.Value);
            placedEncounters.Add(encounter);
            spawnData.SpawnedEncounters.Add(encounter);

            // Reset attempts on successful placement
            attempts = 0;
        }

        spawnData.IsProcessed = true;
        return placedEncounters;
    }

    /// <summary>
    /// Gets encounter templates valid for this floor and region.
    /// </summary>
    private List<EncounterTemplate> GetAvailableTemplates(FloorSpawnConfig config, Region region)
    {
        var templates = new List<EncounterTemplate>();

        // First try weighted templates from config
        foreach (var entry in config.EncounterWeights)
        {
            var template = _dataLoader.GetEncounterTemplate(entry.Id);
            if (template != null && IsTemplateValidForRegion(template, region))
            {
                templates.Add(template);
            }
        }

        // If none configured, get all templates that fit the region
        if (templates.Count == 0)
        {
            foreach (var template in _dataLoader.GetAllEncounterTemplates())
            {
                if (IsTemplateValidForRegion(template, region))
                {
                    templates.Add(template);
                }
            }
        }

        return templates;
    }

    /// <summary>
    /// Checks if a template can be used in this region.
    /// </summary>
    private bool IsTemplateValidForRegion(EncounterTemplate template, Region region)
    {
        // Check minimum region size
        if (region.Area < template.MinRegionSize)
            return false;

        // Placement preferences are soft constraints - handled in selection weighting
        // All templates that meet minimum size are valid candidates

        return true;
    }

    /// <summary>
    /// Checks if a region matches a type descriptor.
    /// </summary>
    private bool MatchesRegionType(Region region, string typeDescriptor)
    {
        return typeDescriptor.ToLowerInvariant() switch
        {
            "large" => region.Area >= 64,
            "medium" => region.Area >= 25 && region.Area < 64,
            "small" => region.Area < 25,
            "dead_end" => region.Tag?.ToLowerInvariant() == "dead_end",
            "passage" => region.Tag?.ToLowerInvariant() == "passage" || region.Area < 16,
            _ => region.Tag?.ToLowerInvariant() == typeDescriptor.ToLowerInvariant()
        };
    }

    /// <summary>
    /// Selects a template based on weighted random, budget, and region fit.
    /// </summary>
    private EncounterTemplate SelectTemplate(
        List<EncounterTemplate> templates,
        RegionSpawnData spawnData,
        Region region)
    {
        // Filter to templates that fit remaining budget
        var validTemplates = templates
            .Where(t => t.MinBudget <= spawnData.RemainingBudget)
            .ToList();

        if (validTemplates.Count == 0)
            return null;

        // Weight templates by fit
        var weightedTemplates = new List<(EncounterTemplate template, int weight)>();
        int totalWeight = 0;

        foreach (var template in validTemplates)
        {
            int weight = 10; // Base weight

            // Bonus for matching region preferences
            if (template.Placement.PreferredRegions.Count > 0)
            {
                foreach (var preferred in template.Placement.PreferredRegions)
                {
                    if (MatchesRegionType(region, preferred))
                    {
                        weight += 20;
                        break;
                    }
                }
            }

            // Bonus for budget efficiency (use more of remaining budget)
            float budgetRatio = (float)template.MinBudget / spawnData.RemainingBudget;
            if (budgetRatio > 0.5f)
            {
                weight += 10;
            }

            // Adjust for danger level
            if (spawnData.DangerLevel > 1.2f && template.Type == EncounterType.Ambush)
            {
                weight += 15; // Dangerous regions favor ambushes
            }

            weightedTemplates.Add((template, weight));
            totalWeight += weight;
        }

        if (totalWeight == 0)
            return validTemplates[0];

        // Weighted random selection
        int roll = _rng.RandiRange(0, totalWeight - 1);
        int cumulative = 0;

        foreach (var (template, weight) in weightedTemplates)
        {
            cumulative += weight;
            if (roll < cumulative)
                return template;
        }

        return weightedTemplates[0].template;
    }

    /// <summary>
    /// Finds a valid position for an encounter within the region.
    /// </summary>
    private GridPosition? FindEncounterPosition(
        Region region,
        EncounterTemplate template,
        List<GridPosition> existingCenters,
        HashSet<Vector2I> occupiedPositions)
    {
        List<GridPosition> candidates;

        // Select candidates based on template preferences
        if (template.Placement.PreferEdges)
        {
            candidates = region.EdgeTiles.ToList();
        }
        else if (template.Type == EncounterType.Lair)
        {
            // Lairs prefer center
            candidates = new List<GridPosition> { region.Centroid };

            // Add nearby tiles as fallback
            foreach (var tile in region.Tiles)
            {
                int dist = (tile.X - region.Centroid.X) * (tile.X - region.Centroid.X) +
                          (tile.Y - region.Centroid.Y) * (tile.Y - region.Centroid.Y);
                if (dist <= 9) // Within 3 tiles of center
                {
                    candidates.Add(tile);
                }
            }
        }
        else
        {
            // Default: use all tiles
            candidates = region.Tiles.ToList();
        }

        // Shuffle for randomness
        candidates = candidates.OrderBy(_ => _rng.Randi()).ToList();

        foreach (var candidate in candidates)
        {
            var vec = new Vector2I(candidate.X, candidate.Y);

            // Skip if occupied
            if (occupiedPositions.Contains(vec))
                continue;

            // Check spacing from existing encounters
            bool tooClose = false;
            foreach (var existing in existingCenters)
            {
                int distSquared = (candidate.X - existing.X) * (candidate.X - existing.X) +
                                 (candidate.Y - existing.Y) * (candidate.Y - existing.Y);
                if (distSquared < MinEncounterSpacingSquared)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Calculates the threat cost for an encounter based on template and danger level.
    /// </summary>
    private int CalculateEncounterThreatCost(EncounterTemplate template, RegionSpawnData spawnData)
    {
        // Base cost from template
        int baseCost = template.MinBudget;

        // Scale by danger level
        float scaledCost = baseCost * spawnData.DangerLevel;

        // Add some variance
        float variance = _rng.RandfRange(0.8f, 1.2f);
        scaledCost *= variance;

        // Clamp to template bounds
        int finalCost = Mathf.RoundToInt(scaledCost);
        finalCost = Mathf.Clamp(finalCost, template.MinBudget, template.MaxBudget);

        // Don't exceed remaining budget
        return Mathf.Min(finalCost, spawnData.RemainingBudget);
    }
}
