using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Helpers;
using PitsOfDespair.Generation.Spawning.Data;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Spawns creatures for encounters based on template slots and theme creatures.
/// Handles creature selection, placement, and AI configuration.
/// </summary>
public class EncounterSpawner
{
    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly DataLoader _dataLoader;
    private readonly RandomNumberGenerator _rng;

    public EncounterSpawner(
        EntityFactory entityFactory,
        EntityManager entityManager,
        DataLoader dataLoader)
    {
        _entityFactory = entityFactory;
        _entityManager = entityManager;
        _dataLoader = dataLoader;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Spawns all creatures for an encounter.
    /// </summary>
    /// <param name="encounter">The encounter to spawn (contains template, theme, region, position)</param>
    /// <param name="occupiedPositions">Already occupied positions (updated as creatures spawn)</param>
    /// <returns>True if at least one creature was spawned</returns>
    public bool SpawnEncounter(SpawnedEncounter encounter, HashSet<Vector2I> occupiedPositions)
    {
        if (encounter.Template == null || encounter.Theme == null || encounter.Region == null)
        {
            encounter.Success = false;
            encounter.ErrorMessage = "Missing template, theme, or region";
            return false;
        }

        // Get available tiles in the region
        var availableTiles = encounter.Region.Tiles
            .Select(t => new Vector2I(t.X, t.Y))
            .Where(t => !occupiedPositions.Contains(t))
            .ToList();

        if (availableTiles.Count == 0)
        {
            encounter.Success = false;
            encounter.ErrorMessage = "No available tiles in region";
            return false;
        }

        // Get creatures available in this theme
        var themeCreatures = GetThemeCreatures(encounter.Theme);
        if (themeCreatures.Count == 0)
        {
            encounter.Success = false;
            encounter.ErrorMessage = $"No creatures found in theme '{encounter.Theme.Id}'";
            return false;
        }

        // Process each slot in the template
        SpawnedCreature leaderCreature = null;
        int actualThreatSpawned = 0;

        foreach (var slot in encounter.Template.Slots)
        {
            // Determine how many creatures to spawn for this slot
            int count = RollSlotCount(slot);

            for (int i = 0; i < count; i++)
            {
                // Select a creature that matches the slot requirements
                var creatureSelection = SelectCreatureForSlot(slot, themeCreatures, encounter);
                if (creatureSelection == null)
                {
                    GD.PushWarning($"[EncounterSpawner] No creature fits slot '{slot.Role}'");
                    continue;
                }

                var (creatureId, creatureData) = creatureSelection.Value;

                // Find placement position
                var position = FindSlotPosition(slot, encounter, availableTiles, occupiedPositions);
                if (position == null)
                {
                    GD.PushWarning($"[EncounterSpawner] No position for slot '{slot.Role}' in region {encounter.Region?.Id}");
                    continue;
                }

                // Spawn the creature
                var spawnedCreature = SpawnCreature(creatureId, creatureData, position.Value, slot, encounter);
                if (spawnedCreature != null)
                {
                    encounter.Creatures.Add(spawnedCreature);
                    occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
                    availableTiles.Remove(new Vector2I(position.Value.X, position.Value.Y));

                    actualThreatSpawned += spawnedCreature.Threat;

                    // Track leader for AI configuration
                    if (slot.Role == "leader" || slot.Role == "alpha" || slot.Role == "guardian")
                    {
                        leaderCreature = spawnedCreature;
                        encounter.Leader = spawnedCreature;
                    }
                }
            }
        }

        // Update encounter with actual threat spawned
        encounter.TotalThreat = actualThreatSpawned;

        // Configure AI relationships (followers protect leader, etc.)
        ConfigureEncounterAI(encounter, leaderCreature);

        encounter.Success = encounter.Creatures.Count > 0;
        return encounter.Success;
    }

    /// <summary>
    /// Gets all creature data for creatures in a theme (with their IDs).
    /// </summary>
    private List<(string id, CreatureData data)> GetThemeCreatures(FactionTheme theme)
    {
        var creatures = new List<(string id, CreatureData data)>();

        foreach (var creatureId in theme.Creatures)
        {
            var data = _dataLoader.Creatures.Get(creatureId);
            if (data != null)
            {
                creatures.Add((creatureId, data));
            }
            else
            {
                GD.PushWarning($"[EncounterSpawner] Theme '{theme.Id}' references unknown creature '{creatureId}'");
            }
        }

        return creatures;
    }

    /// <summary>
    /// Rolls the count for a slot using dice notation.
    /// </summary>
    private int RollSlotCount(EncounterSlot slot)
    {
        int minCount = DiceRoller.Roll(slot.MinCount);
        int maxCount = DiceRoller.Roll(slot.MaxCount);

        // Ensure min <= max
        if (minCount > maxCount)
        {
            (minCount, maxCount) = (maxCount, minCount);
        }

        return _rng.RandiRange(minCount, maxCount);
    }

    /// <summary>
    /// Selects a creature that matches the slot requirements.
    /// </summary>
    private (string id, CreatureData data)? SelectCreatureForSlot(
        EncounterSlot slot,
        List<(string id, CreatureData data)> themeCreatures,
        SpawnedEncounter encounter)
    {
        // Score and sort creatures by fit
        var candidates = new List<((string id, CreatureData data) creature, int score)>();

        foreach (var creature in themeCreatures)
        {
            // Check threat fits floor's threat band
            if (encounter.FloorConfig != null)
            {
                if (creature.data.Threat < encounter.FloorConfig.MinThreat)
                    continue;
                if (creature.data.Threat > encounter.FloorConfig.MaxThreat)
                    continue;
            }

            // Check required archetypes (strict filter)
            if (slot.RequiredArchetypes != null && slot.RequiredArchetypes.Count > 0)
            {
                if (!ArchetypeInferrer.MatchesArchetypes(creature.data, slot.RequiredArchetypes))
                    continue;
            }

            // Get config values for scoring
            int baseScore = encounter.FloorConfig?.BaseScore ?? 50;
            int noMatchScore = encounter.FloorConfig?.NoMatchScore ?? 10;
            int matchScoreBonus = encounter.FloorConfig?.MatchScoreBonus ?? 50;
            int roleKeywordBonus = encounter.FloorConfig?.RoleKeywordBonus ?? 20;
            int maxThreat = encounter.FloorConfig?.MaxThreat ?? 999;

            // Calculate match score (using preferred archetypes for scoring)
            int score = ArchetypeInferrer.CalculateArchetypeMatchScore(
                creature.data,
                slot.PreferredArchetypes,
                baseScore,
                noMatchScore,
                matchScoreBonus);

            // Bonus for role-specific keywords in creature name/type
            if (MatchesRoleKeywords(creature.data, slot.Role))
            {
                score += roleKeywordBonus;
            }

            // Apply inverse threat weighting (lower threat = more common)
            // Formula: weight = max(1, maxThreat - threat + 1)
            int threatWeight = Mathf.Max(1, maxThreat - creature.data.Threat + 1);
            int weightedScore = score * threatWeight;

            candidates.Add((creature, weightedScore));
        }

        if (candidates.Count == 0)
        {
            // Fallback: take any creature that fits threat band
            var fallback = themeCreatures
                .Where(c => encounter.FloorConfig == null ||
                    (c.data.Threat >= encounter.FloorConfig.MinThreat &&
                     c.data.Threat <= encounter.FloorConfig.MaxThreat))
                .OrderBy(_ => _rng.Randi())
                .FirstOrDefault();
            return fallback.data != null ? fallback : null;
        }

        // Weighted random selection by score
        int totalScore = candidates.Sum(c => c.score);
        if (totalScore == 0)
        {
            return candidates[_rng.RandiRange(0, candidates.Count - 1)].creature;
        }

        int roll = _rng.RandiRange(0, totalScore - 1);
        int cumulative = 0;

        foreach (var (creature, score) in candidates)
        {
            cumulative += score;
            if (roll < cumulative)
                return creature;
        }

        return candidates[0].creature;
    }

    /// <summary>
    /// Checks if a creature matches role keywords.
    /// </summary>
    private bool MatchesRoleKeywords(CreatureData creature, string role)
    {
        string name = creature.Name?.ToLowerInvariant() ?? "";
        string type = creature.Type?.ToLowerInvariant() ?? "";

        return role?.ToLowerInvariant() switch
        {
            "leader" => name.Contains("chief") || name.Contains("boss") || name.Contains("alpha") || name.Contains("elder"),
            "alpha" => name.Contains("alpha") || name.Contains("elder") || name.Contains("pack"),
            "scout" => name.Contains("scout") || name.Contains("archer") || name.Contains("tracker"),
            "guard" => name.Contains("guard") || name.Contains("sentinel") || name.Contains("warden"),
            "guardian" => name.Contains("guardian") || name.Contains("protector") || type.Contains("elite"),
            "vermin" => type.Contains("rodent") || type.Contains("vermin") || name.Contains("rat"),
            _ => false
        };
    }

    /// <summary>
    /// Finds a position for a creature based on slot placement preference.
    /// </summary>
    private GridPosition? FindSlotPosition(
        EncounterSlot slot,
        SpawnedEncounter encounter,
        List<Vector2I> availableTiles,
        HashSet<Vector2I> occupiedPositions)
    {
        if (availableTiles.Count == 0)
            return null;

        var region = encounter.Region;
        var center = encounter.CenterPosition;

        List<Vector2I> candidates;

        switch (slot.Placement?.ToLowerInvariant())
        {
            case "center":
                // Prefer tiles near the center
                candidates = availableTiles
                    .OrderBy(t => (t.X - center.X) * (t.X - center.X) + (t.Y - center.Y) * (t.Y - center.Y))
                    .Take(5)
                    .ToList();
                break;

            case "surrounding":
                // Prefer tiles around the center (not too close, not too far)
                candidates = availableTiles
                    .Where(t =>
                    {
                        int dist = (t.X - center.X) * (t.X - center.X) + (t.Y - center.Y) * (t.Y - center.Y);
                        return dist >= 4 && dist <= 25; // 2-5 tiles from center
                    })
                    .OrderBy(_ => _rng.Randi())
                    .Take(10)
                    .ToList();

                // Fallback if no tiles in range
                if (candidates.Count == 0)
                    candidates = availableTiles.OrderBy(_ => _rng.Randi()).Take(5).ToList();
                break;

            case "edge":
            case "near_walls":
                // Prefer edge tiles
                var edgeTiles = region.EdgeTiles
                    .Select(t => new Vector2I(t.X, t.Y))
                    .Where(t => !occupiedPositions.Contains(t))
                    .ToList();

                candidates = edgeTiles.Count > 0
                    ? edgeTiles.OrderBy(_ => _rng.Randi()).Take(5).ToList()
                    : availableTiles.OrderBy(_ => _rng.Randi()).Take(5).ToList();
                break;

            case "formation":
                // Place in a line from center
                candidates = availableTiles
                    .OrderBy(t => Mathf.Abs(t.Y - center.Y)) // Prefer same row
                    .ThenBy(t => Mathf.Abs(t.X - center.X))
                    .Take(5)
                    .ToList();
                break;

            default: // "random"
                candidates = availableTiles.OrderBy(_ => _rng.Randi()).Take(5).ToList();
                break;
        }

        return candidates.Count > 0
            ? new GridPosition(candidates[0].X, candidates[0].Y)
            : null;
    }

    /// <summary>
    /// Spawns a creature entity at the given position.
    /// </summary>
    private SpawnedCreature SpawnCreature(
        string creatureId,
        CreatureData data,
        GridPosition position,
        EncounterSlot slot,
        SpawnedEncounter encounter)
    {
        // Create entity via factory
        var entity = _entityFactory.CreateCreature(creatureId, position);
        if (entity == null)
        {
            GD.PushWarning($"EncounterSpawner: Failed to create creature '{creatureId}'");
            return null;
        }

        // Add to entity manager
        _entityManager.AddEntity(entity);

        // Create spawn record
        var spawnedCreature = new SpawnedCreature
        {
            Entity = entity,
            CreatureId = creatureId,
            Position = position,
            Archetypes = ArchetypeInferrer.InferArchetypes(data),
            Role = slot.Role,
            Threat = Mathf.RoundToInt(data.Threat * slot.ThreatMultiplier)
        };

        return spawnedCreature;
    }

    /// <summary>
    /// Configures AI relationships for the encounter.
    /// </summary>
    private void ConfigureEncounterAI(SpawnedEncounter encounter, SpawnedCreature leader)
    {
        var aiConfig = encounter.Template.AiConfig;

        // Configure followers to protect leader
        if (aiConfig.FollowersProtectLeader && leader?.Entity != null)
        {
            foreach (var creature in encounter.Creatures)
            {
                if (creature == leader)
                    continue;

                var aiComponent = creature.Entity?.GetNodeOrNull<Components.AIComponent>("AIComponent");
                if (aiComponent != null)
                {
                    aiComponent.ProtectionTarget = leader.Entity;
                }
            }
        }

        // Configure initial AI state (sleeping, patrolling, etc.)
        // This would set flags on AIComponent that goals can check
        // For now, the goals use default behavior
        // Future: Add SleepingComponent, PatrolComponent, etc.
    }
}
