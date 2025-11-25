using System.Collections.Generic;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Configures AI behavior for spawned encounters.
/// Sets up protection targets, territory behavior, patrol routes, and initial states.
/// </summary>
public class SpawnAIConfigurator
{
    /// <summary>
    /// Configures all creatures in an encounter with appropriate AI settings.
    /// </summary>
    public void ConfigureEncounter(SpawnedEncounter encounter)
    {
        if (encounter?.Creatures == null || encounter.Creatures.Count == 0)
            return;

        var aiConfig = encounter.Template?.AIConfig;

        // Configure each creature
        foreach (var creature in encounter.Creatures)
        {
            ConfigureCreatureAI(creature, encounter, aiConfig);
        }

        // Configure group relationships
        ConfigureGroupRelationships(encounter, aiConfig);
    }

    /// <summary>
    /// Configures a single creature's AI settings.
    /// </summary>
    private void ConfigureCreatureAI(
        SpawnedCreature creature,
        SpawnedEncounter encounter,
        EncounterAIConfig aiConfig)
    {
        var aiComponent = creature.Entity?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent == null)
            return;

        // Set home region/territory
        if (encounter.Region != null)
        {
            aiComponent.HomeRegionId = encounter.Region.Id;
            aiComponent.HomeCenter = encounter.CenterPosition;
            aiComponent.HomeTerritoryRadius = CalculateTerritoryRadius(encounter.Region);
        }

        // Configure initial state based on template
        ConfigureInitialState(aiComponent, creature, aiConfig);
    }

    /// <summary>
    /// Configures the initial AI state (sleeping, guarding, etc.).
    /// </summary>
    private void ConfigureInitialState(
        AIComponent aiComponent,
        SpawnedCreature creature,
        EncounterAIConfig aiConfig)
    {
        if (aiConfig == null)
            return;

        // Set sleeping state for ambush encounters
        if (aiConfig.InitialState?.ToLowerInvariant() == "sleeping")
        {
            aiComponent.Sleep();
            aiComponent.WakeDistance = aiConfig.WakeRadius > 0 ? aiConfig.WakeRadius : 5;
        }

        // Note: Guarding and other states are handled via the existing goal system.
        // The BoredGoal checks for threats and handles guarding behavior naturally.
    }

    /// <summary>
    /// Configures group relationships (followers protect leader, etc.).
    /// </summary>
    private void ConfigureGroupRelationships(SpawnedEncounter encounter, EncounterAIConfig aiConfig)
    {
        if (aiConfig == null || encounter.Leader?.Entity == null)
            return;

        // Configure followers to protect leader
        if (aiConfig.FollowersProtectLeader)
        {
            foreach (var creature in encounter.Creatures)
            {
                // Skip the leader
                if (creature == encounter.Leader)
                    continue;

                var aiComponent = creature.Entity?.GetNodeOrNull<AIComponent>("AIComponent");
                if (aiComponent != null)
                {
                    aiComponent.ProtectionTarget = encounter.Leader.Entity;
                    aiComponent.FollowDistance = 3; // Default follow distance
                }
            }
        }
    }

    /// <summary>
    /// Calculates territory radius based on region size.
    /// </summary>
    private int CalculateTerritoryRadius(Region region)
    {
        if (region?.Tiles == null || region.Tiles.Count == 0)
            return 10;

        // Use half the bounding box diagonal as radius
        int width = region.BoundingBox.Size.X;
        int height = region.BoundingBox.Size.Y;
        int diagonal = Mathf.RoundToInt(Mathf.Sqrt(width * width + height * height));
        return Mathf.Max(5, diagonal / 2);
    }

    /// <summary>
    /// Configures a creature to start in sleeping state.
    /// Used for ambush encounters.
    /// </summary>
    public void SetSleeping(BaseEntity entity, int wakeDistance = 5)
    {
        var aiComponent = entity?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.Sleep();
            aiComponent.WakeDistance = wakeDistance;
        }
    }

    /// <summary>
    /// Configures a creature to protect another entity.
    /// Used for bodyguard/follower behavior.
    /// </summary>
    public void SetProtectionTarget(BaseEntity follower, BaseEntity target, int followDistance = 3)
    {
        var aiComponent = follower?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.ProtectionTarget = target;
            aiComponent.FollowDistance = followDistance;
        }
    }

    /// <summary>
    /// Configures a creature's home territory.
    /// </summary>
    public void SetHomeTerritory(BaseEntity entity, GridPosition center, int radius, int regionId = -1)
    {
        var aiComponent = entity?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.HomeCenter = center;
            aiComponent.HomeTerritoryRadius = radius;
            aiComponent.HomeRegionId = regionId;
        }
    }

    /// <summary>
    /// Batch configures multiple creatures for a pack/group encounter.
    /// Sets up the alpha as leader with all others as followers.
    /// </summary>
    public void ConfigurePackBehavior(
        BaseEntity alpha,
        List<BaseEntity> packMembers,
        GridPosition homeCenter,
        int territoryRadius = 10)
    {
        // Configure alpha
        SetHomeTerritory(alpha, homeCenter, territoryRadius);

        // Configure pack members to follow alpha
        foreach (var member in packMembers)
        {
            if (member != alpha)
            {
                SetProtectionTarget(member, alpha, followDistance: 2);
                SetHomeTerritory(member, homeCenter, territoryRadius);
            }
        }
    }
}
