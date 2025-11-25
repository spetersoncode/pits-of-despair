using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Data;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Selects creatures from faction themes based on archetype requirements and budget constraints.
/// Provides weighted selection favoring creatures that best match the requested archetype and threat level.
/// </summary>
public class ArchetypeSelector
{
    private readonly DataLoader _dataLoader;
    private readonly RandomNumberGenerator _rng;

    /// <summary>
    /// Cache of inferred archetypes per creature ID to avoid repeated inference.
    /// </summary>
    private readonly Dictionary<string, List<CreatureArchetype>> _archetypeCache = new();

    public ArchetypeSelector(DataLoader dataLoader)
    {
        _dataLoader = dataLoader;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Selects a creature from the theme that matches the archetype requirements and fits within budget.
    /// </summary>
    /// <param name="theme">The faction theme to select from</param>
    /// <param name="preferredArchetypes">Preferred archetypes (null or empty = any)</param>
    /// <param name="budget">Maximum threat allowed</param>
    /// <param name="targetThreat">Ideal threat level (used for weighting)</param>
    /// <param name="threatMultiplier">Multiplier applied to creature threat</param>
    /// <returns>Selected creature ID and data, or null if none found</returns>
    public (string id, CreatureData data)? SelectCreature(
        FactionTheme theme,
        List<string> preferredArchetypes,
        int budget,
        int targetThreat = 0,
        float threatMultiplier = 1.0f)
    {
        if (theme == null || theme.Creatures == null || theme.Creatures.Count == 0)
            return null;

        var candidates = GetCandidates(theme, preferredArchetypes, budget, threatMultiplier);

        if (candidates.Count == 0)
            return null;

        // If no target threat specified, use weighted random by match score
        if (targetThreat <= 0)
        {
            return SelectByMatchScore(candidates);
        }

        // Weight by both match score and threat proximity
        return SelectByThreatProximity(candidates, targetThreat, threatMultiplier);
    }

    /// <summary>
    /// Gets all creatures from the theme that match requirements.
    /// </summary>
    public List<CreatureCandidate> GetCandidates(
        FactionTheme theme,
        List<string> preferredArchetypes,
        int budget,
        float threatMultiplier = 1.0f)
    {
        var candidates = new List<CreatureCandidate>();

        foreach (var creatureId in theme.Creatures)
        {
            var data = _dataLoader.GetCreature(creatureId);
            if (data == null)
                continue;

            // Check if effective threat fits budget
            int effectiveThreat = Mathf.RoundToInt(data.Threat * threatMultiplier);
            if (effectiveThreat > budget)
                continue;

            // Get archetypes (use cache)
            var archetypes = GetCachedArchetypes(creatureId, data);

            // Calculate match score
            int matchScore = CalculateMatchScore(archetypes, preferredArchetypes);

            candidates.Add(new CreatureCandidate
            {
                Id = creatureId,
                Data = data,
                Archetypes = archetypes,
                EffectiveThreat = effectiveThreat,
                MatchScore = matchScore
            });
        }

        return candidates;
    }

    /// <summary>
    /// Selects the best creature to fill a specific threat amount.
    /// Prefers creatures closest to the target threat that also match archetypes.
    /// </summary>
    public (string id, CreatureData data)? SelectForThreatTarget(
        FactionTheme theme,
        int targetThreat,
        List<string> preferredArchetypes = null,
        float threatMultiplier = 1.0f)
    {
        var candidates = GetCandidates(theme, preferredArchetypes, targetThreat + 5, threatMultiplier);

        if (candidates.Count == 0)
            return null;

        // Score by threat proximity (closer = better)
        return SelectByThreatProximity(candidates, targetThreat, threatMultiplier);
    }

    /// <summary>
    /// Gets all creatures matching a specific archetype from a theme.
    /// </summary>
    public List<(string id, CreatureData data)> GetCreaturesByArchetype(
        FactionTheme theme,
        CreatureArchetype archetype)
    {
        var results = new List<(string id, CreatureData data)>();

        foreach (var creatureId in theme.Creatures)
        {
            var data = _dataLoader.GetCreature(creatureId);
            if (data == null)
                continue;

            var archetypes = GetCachedArchetypes(creatureId, data);
            if (archetypes.Contains(archetype))
            {
                results.Add((creatureId, data));
            }
        }

        return results;
    }

    /// <summary>
    /// Checks if a theme has at least one creature matching the archetype.
    /// </summary>
    public bool ThemeHasArchetype(FactionTheme theme, CreatureArchetype archetype)
    {
        foreach (var creatureId in theme.Creatures)
        {
            var data = _dataLoader.GetCreature(creatureId);
            if (data == null)
                continue;

            var archetypes = GetCachedArchetypes(creatureId, data);
            if (archetypes.Contains(archetype))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the lowest threat creature in the theme (for minion/fodder selection).
    /// </summary>
    public (string id, CreatureData data)? GetLowestThreatCreature(FactionTheme theme)
    {
        string lowestId = null;
        CreatureData lowestData = null;
        int lowestThreat = int.MaxValue;

        foreach (var creatureId in theme.Creatures)
        {
            var data = _dataLoader.GetCreature(creatureId);
            if (data != null && data.Threat < lowestThreat)
            {
                lowestThreat = data.Threat;
                lowestId = creatureId;
                lowestData = data;
            }
        }

        return lowestData != null ? (lowestId, lowestData) : null;
    }

    /// <summary>
    /// Gets the highest threat creature in the theme (for leader/boss selection).
    /// </summary>
    public (string id, CreatureData data)? GetHighestThreatCreature(FactionTheme theme, int maxThreat = int.MaxValue)
    {
        string highestId = null;
        CreatureData highestData = null;
        int highestThreat = 0;

        foreach (var creatureId in theme.Creatures)
        {
            var data = _dataLoader.GetCreature(creatureId);
            if (data != null && data.Threat > highestThreat && data.Threat <= maxThreat)
            {
                highestThreat = data.Threat;
                highestId = creatureId;
                highestData = data;
            }
        }

        return highestData != null ? (highestId, highestData) : null;
    }

    /// <summary>
    /// Clears the archetype cache. Call when creature data changes.
    /// </summary>
    public void ClearCache()
    {
        _archetypeCache.Clear();
    }

    private List<CreatureArchetype> GetCachedArchetypes(string creatureId, CreatureData data)
    {
        if (!_archetypeCache.TryGetValue(creatureId, out var archetypes))
        {
            archetypes = ArchetypeInferrer.InferArchetypes(data);
            _archetypeCache[creatureId] = archetypes;
        }
        return archetypes;
    }

    private int CalculateMatchScore(List<CreatureArchetype> creatureArchetypes, List<string> preferredArchetypes)
    {
        if (preferredArchetypes == null || preferredArchetypes.Count == 0)
            return 50; // Neutral score for no preference

        int matchCount = 0;
        foreach (var preferred in preferredArchetypes)
        {
            if (TryParseArchetype(preferred, out var archetype) && creatureArchetypes.Contains(archetype))
            {
                matchCount++;
            }
        }

        if (matchCount == 0)
            return 10; // Low but non-zero score for no matches

        // Score 50-100 based on match ratio
        return 50 + (matchCount * 50 / preferredArchetypes.Count);
    }

    private (string id, CreatureData data)? SelectByMatchScore(List<CreatureCandidate> candidates)
    {
        int totalScore = candidates.Sum(c => c.MatchScore);
        if (totalScore == 0)
        {
            var random = candidates[_rng.RandiRange(0, candidates.Count - 1)];
            return (random.Id, random.Data);
        }

        int roll = _rng.RandiRange(0, totalScore - 1);
        int cumulative = 0;

        foreach (var candidate in candidates)
        {
            cumulative += candidate.MatchScore;
            if (roll < cumulative)
                return (candidate.Id, candidate.Data);
        }

        return (candidates[0].Id, candidates[0].Data);
    }

    private (string id, CreatureData data)? SelectByThreatProximity(
        List<CreatureCandidate> candidates,
        int targetThreat,
        float threatMultiplier)
    {
        // Calculate combined score: match score + proximity bonus
        var scored = candidates.Select(c =>
        {
            int threatDiff = Mathf.Abs(c.EffectiveThreat - targetThreat);
            // Proximity score: max 50 points, decreasing as threat differs
            int proximityScore = Mathf.Max(0, 50 - threatDiff * 10);
            int totalScore = c.MatchScore + proximityScore;
            return (candidate: c, score: totalScore);
        }).ToList();

        int totalScore = scored.Sum(s => s.score);
        if (totalScore == 0)
        {
            var random = candidates[_rng.RandiRange(0, candidates.Count - 1)];
            return (random.Id, random.Data);
        }

        int roll = _rng.RandiRange(0, totalScore - 1);
        int cumulative = 0;

        foreach (var (candidate, score) in scored)
        {
            cumulative += score;
            if (roll < cumulative)
                return (candidate.Id, candidate.Data);
        }

        return (candidates[0].Id, candidates[0].Data);
    }

    private static bool TryParseArchetype(string value, out CreatureArchetype archetype)
    {
        archetype = value?.ToLowerInvariant() switch
        {
            "tank" => CreatureArchetype.Tank,
            "warrior" => CreatureArchetype.Warrior,
            "assassin" => CreatureArchetype.Assassin,
            "ranged" => CreatureArchetype.Ranged,
            "support" => CreatureArchetype.Support,
            "brute" => CreatureArchetype.Brute,
            "scout" => CreatureArchetype.Scout,
            _ => CreatureArchetype.Warrior
        };
        return value != null;
    }
}

/// <summary>
/// Represents a creature candidate during selection.
/// </summary>
public class CreatureCandidate
{
    public string Id { get; set; }
    public CreatureData Data { get; set; }
    public List<CreatureArchetype> Archetypes { get; set; }
    public int EffectiveThreat { get; set; }
    public int MatchScore { get; set; }
}
